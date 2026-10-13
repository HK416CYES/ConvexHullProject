using System.Collections.Generic;
using UnityEngine;

namespace ConvexHullDemo {
    /// <summary>A finite mesh regenerated only for the visible world rectangle, with no canvas boundary.</summary>
    public sealed class InfiniteGrid : MonoBehaviour {
        public Camera worldCamera;
        public MeshFilter meshFilter;
        public Color minorColor = new Color(.86f, .89f, .92f, 1);
        public Color majorColor = new Color(.75f, .81f, .87f, 1);
        public Color axisColor = new Color(.49f, .59f, .69f, 1);
        [Min(.01f)] public float spacing = 1;
        [Min(.001f), Tooltip("Grid line width in world units.")] public float lineWidth = .01f;
        private float lastSpacing, lastWidth;
        private Color lastMinor, lastMajor, lastAxis;
        private Mesh mesh;
        private Vector3 lastPosition;
        private Rect lastRect;
        private float lastSize = -1;

        private void Awake() {
            mesh = new Mesh { name = "Visible grid (runtime)" };
            mesh.MarkDynamic();
            meshFilter.sharedMesh = mesh;
        }
        private void LateUpdate() {
            if (worldCamera.transform.position == lastPosition && lastRect == worldCamera.pixelRect &&
                lastSize == worldCamera.orthographicSize && lastSpacing == spacing && lastWidth == lineWidth &&
                lastMinor == minorColor && lastMajor == majorColor && lastAxis == axisColor) return;
            lastPosition = worldCamera.transform.position;
            lastRect = worldCamera.pixelRect;
            lastSize = worldCamera.orthographicSize;
            lastSpacing = spacing; lastWidth = lineWidth;
            lastMinor = minorColor; lastMajor = majorColor; lastAxis = axisColor;
            transform.position = new Vector3(lastPosition.x, lastPosition.y, .4f);
            Rebuild(mesh, worldCamera, spacing, lineWidth, minorColor, majorColor, axisColor);
        }
        public static void Rebuild(Mesh target, Camera camera, float spacing, float lineWidth,
            Color minor, Color major, Color axis) {
            float halfY = camera.orthographicSize, halfX = halfY * camera.aspect;
            var center = camera.transform.position;
            float minX = center.x - halfX - 1, maxX = center.x + halfX + 1;
            float minY = center.y - halfY - 1, maxY = center.y + halfY + 1;
            float width = Mathf.Max(.001f, lineWidth);
            float step = Mathf.Max(.01f, spacing);
            // Prevent excessive mesh sizes when an inspector-configured camera spans a very large range.
            while ((maxX - minX + maxY - minY) / step > 400) step *= 10;
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            int x0 = Mathf.CeilToInt(minX / step), x1 = Mathf.FloorToInt(maxX / step);
            int y0 = Mathf.CeilToInt(minY / step), y1 = Mathf.FloorToInt(maxY / step);
            for (int x = x0; x <= x1; x++) {
                float at = x * step;
                AddQuad(vertices, colors, triangles, at - center.x, minY - center.y,
                    width * (x == 0 ? 2 : 1), maxY - minY, x == 0 ? axis : x % 5 == 0 ? major : minor);
            }
            for (int y = y0; y <= y1; y++) {
                float at = y * step;
                AddQuad(vertices, colors, triangles, minX - center.x, at - center.y,
                    maxX - minX, width * (y == 0 ? 2 : 1), y == 0 ? axis : y % 5 == 0 ? major : minor);
            }
            target.Clear();
            target.SetVertices(vertices); target.SetColors(colors); target.SetTriangles(triangles, 0);
            target.RecalculateBounds();
        }
        private static void AddQuad(List<Vector3> v, List<Color> c, List<int> t,
            float x, float y, float w, float h, Color color) {
            int i = v.Count;
            v.Add(new Vector3(x, y)); v.Add(new Vector3(x + w, y));
            v.Add(new Vector3(x + w, y + h)); v.Add(new Vector3(x, y + h));
            for (int j = 0; j < 4; j++) c.Add(color);
            t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }
        private void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
