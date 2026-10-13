using System;
using System.Collections.Generic;
using UIFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace ConvexHullDemo {
    [DefaultExecutionOrder(-50)]
    public sealed class ConvexHullDemoController : MonoBehaviour {
        public const string ToolbarId = "HullToolbar";
        public const string MenuId = "HullContextMenu";
        public const string HelpId = "HullHelp";
        [Header("Scene references")]
        public Camera worldCamera;
        public UISetting uiSetting;
        public HullPointView pointPrefab;
        public Transform pointsRoot;
        public LineRenderer outline;
        public MeshFilter fill;
        public MeshRenderer fillRenderer;
        public List<HullPointView> previewPoints = new List<HullPointView>();
        [Header("Appearance")]
        public Color innerColor = new Color(.36f, .44f, .54f, 1);
        public Color hullColor = new Color(.10f, .38f, .80f, 1);
        public Color selectionColor = new Color(.95f, .53f, .12f, 1);
        public Color fillColor = new Color(.18f, .49f, .93f, .12f);
        [Min(.01f), Tooltip("Point diameter in world units; independent of screen resolution.")]
        public float pointDiameter = .3f;
        [Min(.001f), Tooltip("Hull outline width in world units.")]
        public float outlineWidth = .04f;
        [Header("Interaction")]
        [Min(.01f), Tooltip("Hit radius in world units; never smaller than the visible point radius.")]
        public float hitRadius = .24f;
        public float dragThresholdPixels = 4;
        [Header("Zoom (orthographic size in world units)")]
        [Min(.01f), Tooltip("Minimum orthographic size: smallest visible range / maximum magnification.")]
        public float minZoomSize = 1;
        [Min(.01f), Tooltip("Maximum orthographic size: largest visible range / minimum magnification.")]
        public float maxZoomSize = 30;
        [Min(.001f), Tooltip("Exponential zoom speed per 120 scroll units (one mouse wheel step).")]
        public float zoomSpeed = .15f;
        public Vector2[] initialPoints = {
            new Vector2(-5,-2), new Vector2(-4,2), new Vector2(0,3), new Vector2(4,2),
            new Vector2(5,-2), new Vector2(1,-3), new Vector2(-2,-1), new Vector2(0,1),
            new Vector2(2,0), new Vector2(-3,1), new Vector2(1,-1), new Vector2(3,-1)
        };

        public sealed class Point {
            public int Id { get; internal set; }
            public Vector2 Position { get; internal set; }
            internal HullPointView View;
        }
        private readonly List<Point> points = new List<Point>();
        private readonly Stack<HullPointView> pool = new Stack<HullPointView>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private readonly HashSet<Vector2> hullPositions = new HashSet<Vector2>();
        private List<Vector2> hull = new List<Vector2>();
        private Mesh fillMesh;
        private UIFrame frame;
        private HullToolbarPanel toolbar;
        private HullContextMenuPanel menu;
        private HullHelpPanel help;
        private int nextId = 1;
        private int? selection;
        private bool dirty = true;
        private bool leftActive, dragging;
        private int? dragPoint;
        private Vector2 leftStart, pointStart;
        private Vector3 cameraStart;
        private float unitsPerPixelAtPress;
        private Vector2 contextWorld;
        private int? contextId;
        public event Action Changed;
        public IReadOnlyList<Point> Points => points;
        public IReadOnlyList<Vector2> Hull => hull;
        public int? SelectedId => selection;
        public double HullArea { get; private set; }
        public int HullRevision { get; private set; }
        public HullToolbarPanel Toolbar => toolbar;
        public HullContextMenuPanel Menu => menu;
        public HullHelpPanel Help => help;
        public bool IsHelpOpen => help != null && help.gameObject.activeSelf;
        public bool IsMenuOpen => menu != null && menu.gameObject.activeSelf;

        private void Awake() {
            worldCamera.orthographicSize = Mathf.Clamp(worldCamera.orthographicSize, minZoomSize, maxZoomSize);
            fillMesh = new Mesh { name = "Convex hull fill (runtime)" };
            fillMesh.MarkDynamic(); fill.sharedMesh = fillMesh;
            foreach (var view in previewPoints) {
                view.gameObject.SetActive(false); pool.Push(view);
            }
            foreach (var p in initialPoints) AddPoint(p);
            FlushChanges();
        }
        private void Start() {
            frame = uiSetting.CreateUIInstance();
            toolbar = frame.GetComponentInChildren<HullToolbarPanel>(true);
            menu = frame.GetComponentInChildren<HullContextMenuPanel>(true);
            help = frame.GetComponentInChildren<HullHelpPanel>(true);
            toolbar.Bind(this); menu.Bind(this, frame.UICamera); help.Bind(this);
            frame.ShowPanel(ToolbarId); toolbar.Refresh();
            if (HullHelpPanel.ShouldShowOnEntry) ShowHelp();
        }

        private void Update() {
            var mouse = Mouse.current;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { if (IsHelpOpen) HideHelp(); else CloseMenu(); }
            if (mouse == null) return;
            Vector2 screen = mouse.position.ReadValue();
            if (mouse.rightButton.wasPressedThisFrame) {
                if (!PointerOverUI(screen) || (IsMenuOpen && menu.Contains(screen))) OpenContextMenu(screen);
            }
            if (mouse.leftButton.wasPressedThisFrame) BeginLeft(screen);
            if (mouse.leftButton.isPressed) MoveLeft(screen);
            if (mouse.leftButton.wasReleasedThisFrame) EndLeft(screen);
            Zoom(screen, mouse.scroll.ReadValue().y);
        }
        private void LateUpdate() {
            FlushChanges();
            ApplyVisualSizes();
        }

        /// <summary>Positive wheel input zooms in around the cursor; UI and active drags consume the gesture.</summary>
        public void Zoom(Vector2 screen, float scrollDelta) {
            if (scrollDelta == 0 || float.IsNaN(scrollDelta) || float.IsInfinity(scrollDelta) ||
                leftActive || IsHelpOpen || !worldCamera.pixelRect.Contains(screen) || PointerOverUI(screen)) return;
            float minimum = Mathf.Max(.01f, minZoomSize), maximum = Mathf.Max(minimum, maxZoomSize);
            float factor = Mathf.Exp(-Mathf.Clamp(scrollDelta / 120f, -100, 100) * Mathf.Clamp(zoomSpeed, .001f, 1));
            float size = Mathf.Clamp(worldCamera.orthographicSize * factor, minimum, maximum);
            if (Mathf.Approximately(size, worldCamera.orthographicSize)) return;
            Vector2 before = ScreenToWorld(screen);
            worldCamera.orthographicSize = size;
            Vector2 offset = before - ScreenToWorld(screen);
            worldCamera.transform.position += new Vector3(offset.x, offset.y, 0);
            CloseMenu();
        }

        public Vector2 ScreenToWorld(Vector2 screen) {
            var p = worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -worldCamera.transform.position.z));
            return new Vector2(p.x, p.y);
        }
        public bool PointerOverUI(Vector2 screen) {
            if (EventSystem.current == null) return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screen }, uiHits);
            return uiHits.Count > 0;
        }
        public int? HitTest(Vector2 screen) {
            int? found = null;
            Vector2 world = ScreenToWorld(screen);
            float radius = Mathf.Max(hitRadius, pointDiameter * .5f);
            float best = radius * radius;
            foreach (var p in points) {
                float distance = (p.Position - world).sqrMagnitude;
                if (distance > radius * radius) continue;
                if (p.Id == selection) return p.Id;
                if (distance <= best) { best = distance; found = p.Id; }
            }
            return found;
        }
        public void Select(int? id) {
            selection = Find(id) != null ? id : null;
            UpdatePointAppearance(); Changed?.Invoke();
        }
        public void BeginLeft(Vector2 screen) {
            leftActive = false;
            if (IsMenuOpen && !menu.Contains(screen)) CloseMenu();
            if (PointerOverUI(screen) || !worldCamera.pixelRect.Contains(screen)) return;
            leftActive = true; dragging = false; leftStart = screen;
            cameraStart = worldCamera.transform.position;
            unitsPerPixelAtPress = 2 * worldCamera.orthographicSize / Mathf.Max(1, worldCamera.pixelHeight);
            var hit = HitTest(screen);
            dragPoint = hit.HasValue && hit == selection ? hit : null;
            if (dragPoint.HasValue) pointStart = Find(dragPoint).Position;
        }
        public void MoveLeft(Vector2 screen) {
            if (!leftActive) return;
            Vector2 delta = screen - leftStart;
            if (!dragging && delta.sqrMagnitude > dragThresholdPixels * dragThresholdPixels) dragging = true;
            if (!dragging) return;
            if (dragPoint.HasValue) MovePoint(dragPoint.Value, pointStart + delta * unitsPerPixelAtPress);
            else worldCamera.transform.position = cameraStart - new Vector3(delta.x, delta.y, 0) * unitsPerPixelAtPress;
        }
        public void EndLeft(Vector2 screen) {
            if (!leftActive) return;
            MoveLeft(screen);
            if (!dragging && !PointerOverUI(screen)) Select(HitTest(screen));
            leftActive = false; dragPoint = null;
        }
        private void OnApplicationFocus(bool focus) { if (!focus) CancelGesture(); }
        public void CancelGesture() { leftActive = false; dragging = false; dragPoint = null; }

        public void OpenContextMenu(Vector2 screen) {
            if (!worldCamera.pixelRect.Contains(screen)) return;
            CancelGesture();
            contextWorld = ScreenToWorld(screen); contextId = HitTest(screen);
            Select(contextId);
            menu.Configure(screen, contextId.HasValue);
            frame.ShowPanel(MenuId);
            menu.PositionAt(screen);
        }
        public void ShowHelp() {
            CloseMenu(); CancelGesture();
            help.SyncPreference(); frame.ShowPanel(HelpId);
        }
        public void HideHelp() { if (frame != null && IsHelpOpen) frame.HidePanel(HelpId); }
        public void CloseMenu() { if (frame != null && IsMenuOpen) frame.HidePanel(MenuId); }
        public void CreateContextPoint() {
            var point = AddPoint(contextWorld); Select(point.Id); CloseMenu();
        }
        public void DeleteContextPoint() {
            if (contextId.HasValue) RemovePoint(contextId.Value);
            CloseMenu();
        }

        public Point AddPoint(Vector2 position) {
            if (!Finite(position)) throw new ArgumentException("Point coordinates must be finite.");
            var view = pool.Count > 0 ? pool.Pop() : Instantiate(pointPrefab, pointsRoot);
            view.gameObject.SetActive(true);
            var point = new Point { Id = nextId++, Position = position, View = view };
            view.name = "Point " + point.Id;
            view.transform.position = new Vector3(position.x, position.y, -.05f);
            view.transform.localScale = Vector3.one * pointDiameter * .5f;
            points.Add(point); dirty = true;
            return point;
        }
        public void MovePoint(int id, Vector2 position) {
            var point = Find(id);
            if (point == null || !Finite(position) || point.Position.Equals(position)) return;
            point.Position = position;
            point.View.transform.position = new Vector3(position.x, position.y, -.05f);
            dirty = true;
        }
        public void RemovePoint(int id) {
            var point = Find(id);
            if (point == null) return;
            point.View.gameObject.SetActive(false); pool.Push(point.View); points.Remove(point);
            if (selection == id) selection = null;
            CancelGesture(); CloseMenu(); dirty = true;
        }
        public void ClearPoints() {
            foreach (var p in points) { p.View.gameObject.SetActive(false); pool.Push(p.View); }
            points.Clear(); selection = null; CancelGesture(); CloseMenu(); dirty = true;
        }
        public bool Randomize(string input, out string error) {
            int count;
            if (!int.TryParse(input, out count) || count < 1 || count > 1000) {
                error = "请输入 1–1000 的整数，现有点集已保留。"; return false;
            }
            Rect rect = GetRandomScreenRect();
            if (rect.width < 1 || rect.height < 1) {
                error = "当前窗口太小，请扩大窗口后重试。"; return false;
            }
            ClearPoints();
            for (int i = 0; i < count; i++)
                AddPoint(ScreenToWorld(new Vector2(UnityEngine.Random.Range(rect.xMin, rect.xMax), UnityEngine.Random.Range(rect.yMin, rect.yMax))));
            error = ""; return true;
        }
        public Rect GetRandomScreenRect() {
            Rect visible = worldCamera.pixelRect;
            float bottom = visible.yMin + 24, top = visible.yMax - 24;
            if (toolbar != null) {
                bottom = Mathf.Max(bottom, toolbar.HelpButtonScreenTop + 24);
                top = Mathf.Min(top, toolbar.ToolbarScreenBottom - 24);
            }
            return Rect.MinMaxRect(visible.xMin + 24, bottom, visible.xMax - 24, top);
        }
        private Point Find(int? id) => id.HasValue ? points.Find(p => p.Id == id.Value) : null;
        private static bool Finite(Vector2 p) => !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y);

        public void FlushChanges() {
            if (!dirty) return;
            dirty = false;
            var coords = new List<Vector2>(points.Count);
            foreach (var p in points) coords.Add(p.Position);
            hull = ConvexHull2D.Compute(coords); HullArea = ConvexHull2D.Area(hull); HullRevision++;
            hullPositions.Clear(); foreach (var p in hull) hullPositions.Add(p);
            outline.positionCount = hull.Count >= 2 ? hull.Count : 0;
            outline.loop = hull.Count >= 3;
            outline.startColor = outline.endColor = hullColor;
            for (int i = 0; i < outline.positionCount; i++) outline.SetPosition(i, new Vector3(hull[i].x, hull[i].y, .05f));
            fillMesh.Clear();
            if (hull.Count >= 3) {
                var vertices = new Vector3[hull.Count]; var colors = new Color[hull.Count];
                Vector2 origin = hull[0];
                for (int i = 0; i < hull.Count; i++) { vertices[i] = hull[i] - origin; colors[i] = Color.white; }
                var triangles = new int[(hull.Count - 2) * 3];
                for (int i = 0; i < hull.Count - 2; i++) { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2; }
                fill.transform.position = new Vector3(origin.x, origin.y, .15f);
                fillMesh.vertices = vertices; fillMesh.colors = colors; fillMesh.triangles = triangles; fillMesh.RecalculateBounds();
            }
            var block = new MaterialPropertyBlock(); block.SetColor("_Color", fillColor); fillRenderer.SetPropertyBlock(block);
            UpdatePointAppearance(); Changed?.Invoke();
        }
        private void UpdatePointAppearance() {
            foreach (var p in points) p.View.SetAppearance(hullPositions.Contains(p.Position) ? hullColor : innerColor, selectionColor, p.Id == selection);
        }
        public void ApplyVisualSizes() {
            if (outline != null) outline.widthMultiplier = outlineWidth;
            foreach (var p in points) p.View.transform.localScale = Vector3.one * pointDiameter * .5f;
            if (!Application.isPlaying)
                foreach (var view in previewPoints)
                    if (view != null) view.transform.localScale = Vector3.one * pointDiameter * .5f;
        }
        private void OnValidate() {
            pointDiameter = Mathf.Max(.01f, pointDiameter);
            outlineWidth = Mathf.Max(.001f, outlineWidth);
            hitRadius = Mathf.Max(.01f, hitRadius);
            minZoomSize = Mathf.Max(.01f, minZoomSize);
            maxZoomSize = Mathf.Max(minZoomSize, maxZoomSize);
            zoomSpeed = Mathf.Clamp(zoomSpeed, .001f, 1);
            ApplyVisualSizes();
        }
        private void OnDestroy() {
            if (frame != null) {
                if (worldCamera != null) worldCamera.GetUniversalAdditionalCameraData().cameraStack.Remove(frame.UICamera);
                Destroy(frame.gameObject);
            }
            if (fillMesh != null) Destroy(fillMesh);
        }
    }
}
