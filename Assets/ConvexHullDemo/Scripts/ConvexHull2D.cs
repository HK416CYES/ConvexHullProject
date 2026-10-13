using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConvexHullDemo {
    /// <summary>Andrew monotone chain. Exact coordinate duplicates are removed; edge interiors are omitted.</summary>
    public static class ConvexHull2D {
        public static List<Vector2> Compute(IEnumerable<Vector2> points) {
            if (points == null) throw new ArgumentNullException(nameof(points));
            var sorted = new List<Vector2>();
            foreach (var p in points) {
                if (float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.y))
                    throw new ArgumentException("Coordinates must be finite.", nameof(points));
                sorted.Add(p);
            }
            sorted.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            int unique = 0;
            for (int i = 0; i < sorted.Count; i++)
                if (unique == 0 || !sorted[i].Equals(sorted[unique - 1])) sorted[unique++] = sorted[i];
            sorted.RemoveRange(unique, sorted.Count - unique);
            if (unique < 3) return sorted;
            var chain = new List<Vector2>(unique * 2);
            foreach (var p in sorted) {
                while (chain.Count >= 2 && Cross(chain[chain.Count - 2], chain[chain.Count - 1], p) <= 0)
                    chain.RemoveAt(chain.Count - 1);
                chain.Add(p);
            }
            int lower = chain.Count;
            for (int i = unique - 2; i >= 0; i--) {
                var p = sorted[i];
                while (chain.Count > lower && Cross(chain[chain.Count - 2], chain[chain.Count - 1], p) <= 0)
                    chain.RemoveAt(chain.Count - 1);
                chain.Add(p);
            }
            chain.RemoveAt(chain.Count - 1);
            return chain;
        }

        public static double Cross(Vector2 a, Vector2 b, Vector2 c) =>
            ((double)b.x - a.x) * ((double)c.y - a.y) - ((double)b.y - a.y) * ((double)c.x - a.x);

        public static double Area(IReadOnlyList<Vector2> hull) {
            double area = 0;
            for (int i = 1; i + 1 < hull.Count; i++) area += Cross(hull[0], hull[i], hull[i + 1]);
            return Math.Abs(area) * 0.5;
        }
    }
}
