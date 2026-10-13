using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConvexHullDemo {
    /// <summary>
    /// 使用 Andrew 单调链算法计算二维凸包：可以想象成用橡皮筋围住所有点后收紧得到的边界。
    /// 先排序，再构造下边界和上边界；总时间复杂度为 O(n log n)，额外空间为 O(n)。
    /// 只返回边界的拐角，重复坐标和共线边上的中间点不会重复计入。
    /// </summary>
    public static class ConvexHull2D {
        /// <summary>
        /// 输入任意顺序的点，返回逆时针排列的凸包顶点，末尾不重复第一个顶点。
        /// 空集返回空列表；单点返回该点；两点或全部共线时只返回端点。
        /// </summary>
        public static List<Vector2> Compute(IEnumerable<Vector2> points) {
            if (points == null) throw new ArgumentNullException(nameof(points));
            // 复制到新列表，避免排序、去重修改调用者的点集。
            var sorted = new List<Vector2>();
            foreach (var p in points) {
                // NaN（非数值）和无穷大无法参与正常的排序与几何计算，提前拒绝。
                if (float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.y))
                    throw new ArgumentException("Coordinates must be finite.", nameof(points));
                sorted.Add(p);
            }
            // 第一步：先按 x 从小到大排序，x 相同再按 y 排序。
            // 这样就能从左向右扫描；完全相同的坐标也会排在一起。
            sorted.Sort((a, b) => a.x == b.x ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            // 第二步：原地压缩重复坐标。i 是读取位置，unique 是下一个写入位置。
            // 列表前 unique 个元素始终是已经找到的不同坐标；后面的旧数据最后统一删除。
            // Equals 比较精确坐标，不会把“非常接近但不同”的点合并。
            // 这里只处理算法的副本，场景中的重合点仍可以分别存在和删除。
            int unique = 0;
            for (int i = 0; i < sorted.Count; i++)
                if (unique == 0 || !sorted[i].Equals(sorted[unique - 1])) sorted[unique++] = sorted[i];
            sorted.RemoveRange(unique, sorted.Count - unique);
            // 不足三个不同坐标时无法围成有面积的多边形，直接返回排序后的点。
            if (unique < 3) return sorted;
            // 第三步：从左向右构造下链。列表当作栈使用，只在末尾加入或移除候选顶点。
            // unique * 2 只是预留容量，减少扩容；并不是最终凸包的顶点数。
            var chain = new List<Vector2>(unique * 2);
            foreach (var p in sorted) {
                // 设末尾两点为 A、B，新点为 P。Cross(A, B, P) > 0 表示左转。
                // 右转说明当前候选链出现凹陷，需要移除 B；等于 0 时移除共线中间点 B。
                // 必须反复检查：移除 B 后，新的末尾两点与 P 仍可能形成右转。
                // 被下链移除的点仍会参与上链计算，不代表它一定不属于最终凸包。
                while (chain.Count >= 2 && Cross(chain[chain.Count - 2], chain[chain.Count - 1], p) <= 0)
                    chain.RemoveAt(chain.Count - 1);
                chain.Add(p);
            }
            // 第四步：从右向左构造上链，并直接接在下链后面。
            // lower 记录已经完成的下链长度，防止上链回退时删除下链顶点。
            int lower = chain.Count;
            // 最右端的点已在下链末尾，所以从倒数第二个点开始，避免重复加入连接点。
            for (int i = unique - 2; i >= 0; i--) {
                var p = sorted[i];
                // chain.Count > lower 表示上链至少加入了一个点：
                // 它与下链末尾的连接点组成两个候选点，才可以检查转向。
                // 与下链一样，只保留左转；由于扫描方向反过来了，这次得到的是上边界。
                while (chain.Count > lower && Cross(chain[chain.Count - 2], chain[chain.Count - 1], p) <= 0)
                    chain.RemoveAt(chain.Count - 1);
                chain.Add(p);
            }
            // 上链最终回到起点。删除这个重复起点，保持“每个顶点只出现一次”的返回约定。
            // 上下链相连后按逆时针排列；全部共线时，中间点被移除，最终只剩两个端点。
            chain.RemoveAt(chain.Count - 1);
            return chain;
        }

        /// <summary>
        /// 计算向量 AB 与 AC 的二维叉积：(bx-ax)*(cy-ay) - (by-ay)*(cx-ax)。
        /// 在 X 向右、Y 向上的坐标系中：正数表示 A→B→C 左转，负数表示右转，零表示共线。
        /// 结果也是三角形 ABC 有符号面积的两倍。
        /// </summary>
        // 在做减法之前转换为 double，减少后续减法和乘法的舍入误差。
        // 输入 Vector2 本身仍使用 float，因此这不等于任意精度的几何运算。
        public static double Cross(Vector2 a, Vector2 b, Vector2 c) =>
            ((double)b.x - a.x) * ((double)c.y - a.y) - ((double)b.y - a.y) * ((double)c.x - a.x);

        /// <summary>
        /// 计算已按边界顺序排列的凸包面积；输入应为 Compute 的结果，而不是无序点集。
        /// </summary>
        public static double Area(IReadOnlyList<Vector2> hull) {
            // 以 hull[0] 为共同顶点，把凸多边形拆成互不重叠的三角扇：
            // (0,1,2)、(0,2,3)、(0,3,4)……共有 h-2 个三角形。
            // 先累加每个三角形的有符号面积的两倍，最后统一除以 2。
            double area = 0;
            for (int i = 1; i + 1 < hull.Count; i++) area += Cross(hull[0], hull[i], hull[i + 1]);
            // 顶点不足三个时循环不执行，面积自然为 0。
            // 取绝对值，使顺时针、逆时针边界都能得到非负面积。
            return Math.Abs(area) * 0.5;
        }
    }
}
