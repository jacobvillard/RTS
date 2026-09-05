using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI {
    /// <summary>Small code-drawn pixel UI shapes, independent of font glyph support.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public class LevelCardGraphic : MaskableGraphic {
        public enum Shape { Frame, Star, LeftArrow, RightArrow, Lock, CrossedSwords, Banner }
        [SerializeField] private Shape shape;
        [SerializeField] private Color fill = new Color(0.035f, 0.045f, 0.02f, 0.98f);

        public void Configure(Shape value, Color tint) {
            shape = value;
            color = tint;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh) {
            mesh.Clear();
            var r = GetPixelAdjustedRect();
            if (shape == Shape.Banner) {
                Quad(mesh, r, new Color(0.035f, 0.05f, 0.01f));
                var inset = Inset(r, 3);
                Quad(mesh, inset, color * new Color(0.4f, 0.4f, 0.4f, 1));
                Quad(mesh, Inset(r, 6), color);
                for (var i = 0; i < 5; i++) {
                    var width = r.width * (0.5f - i * 0.09f);
                    Quad(mesh, new Rect(r.center.x - width / 2, r.yMin - i * 3, width, 3), color);
                }
                return;
            }
            if (shape == Shape.CrossedSwords) {
                var swordPixel = Mathf.Min(r.width, r.height) / 20;
                for (var side = -1; side <= 1; side += 2) {
                    for (var step = -6; step <= 7; step++) {
                        var p = r.center + new Vector2(step * side, step) * swordPixel;
                        Quad(mesh, new Rect(p.x - swordPixel, p.y - swordPixel, swordPixel * 2, swordPixel * 2), color);
                    }
                    for (var cross = -2; cross <= 2; cross++) {
                        var p = r.center + new Vector2((-3 + cross) * side, -3 - cross) * swordPixel;
                        Quad(mesh, new Rect(p.x - swordPixel, p.y - swordPixel, swordPixel * 2, swordPixel * 2), color);
                    }
                }
                return;
            }
            if (shape == Shape.Frame) {
                SteppedRect(mesh, r, 8, new Color(0.015f, 0.02f, 0.005f, 1));
                SteppedRect(mesh, Inset(r, 2), 6, color * new Color(0.45f, 0.45f, 0.45f, 1));
                SteppedRect(mesh, Inset(r, 4), 5, color);
                SteppedRect(mesh, Inset(r, 6), 4, fill);
                Quad(mesh, new Rect(r.x + 12, r.yMax - 8, r.width - 24, 1), Color.Lerp(color, Color.white, 0.25f));
                return;
            }

            if (shape == Shape.Star) {
                PolygonStar(mesh, r, 1, new Color(0.025f, 0.025f, 0.015f, 1));
                PolygonStar(mesh, r, 0.86f, color * new Color(0.62f, 0.62f, 0.62f, 1));
                PolygonStar(mesh, r, 0.68f, color);
                return;
            }

            if (shape == Shape.Lock) {
                var p = Mathf.Min(r.width / 14f, r.height / 17f);
                var o = r.center - new Vector2(7 * p, 8.5f * p);
                Quad(mesh, new Rect(o.x + p, o.y, 12 * p, 10 * p), new Color(0.025f, 0.025f, 0.025f, 1));
                Quad(mesh, new Rect(o.x + 2 * p, o.y + p, 10 * p, 8 * p), color);
                Quad(mesh, new Rect(o.x + 3 * p, o.y + 9 * p, 2 * p, 5 * p), color);
                Quad(mesh, new Rect(o.x + 9 * p, o.y + 9 * p, 2 * p, 5 * p), color);
                Quad(mesh, new Rect(o.x + 5 * p, o.y + 14 * p, 4 * p, 2 * p), color);
                Quad(mesh, new Rect(o.x + 6 * p, o.y + 3 * p, 2 * p, 4 * p), new Color(0.05f, 0.05f, 0.05f, 1));
                return;
            }

            var pixel = Mathf.Min(r.width / 9f, r.height / 17f);
            for (var pass = 0; pass < 2; pass++) {
                for (var row = 0; row < 15; row++) {
                    var x = Mathf.Abs(row - 7);
                    if (shape == Shape.RightArrow) x = 7 - x;
                    var cell = new Rect(r.center.x + (x - 4.5f) * pixel, r.center.y + (row - 7.5f) * pixel, pixel * 2, pixel);
                    if (pass == 0) Quad(mesh, new Rect(cell.x - pixel * 0.5f, cell.y - pixel * 0.5f, cell.width + pixel, cell.height + pixel), new Color(0.02f, 0.025f, 0.005f, 1));
                    else Quad(mesh, cell, color);
                }
            }
        }

        private static Rect Inset(Rect r, float amount) => new Rect(r.x + amount, r.y + amount, Mathf.Max(0, r.width - amount * 2), Mathf.Max(0, r.height - amount * 2));

        private static void SteppedRect(VertexHelper mesh, Rect r, float corner, Color tint) {
            Quad(mesh, new Rect(r.x + corner, r.y, r.width - corner * 2, r.height), tint);
            Quad(mesh, new Rect(r.x, r.y + corner, corner, r.height - corner * 2), tint);
            Quad(mesh, new Rect(r.xMax - corner, r.y + corner, corner, r.height - corner * 2), tint);
            Quad(mesh, new Rect(r.x + corner / 2, r.y + corner / 2, r.width - corner, r.height - corner), tint);
        }

        private static void PolygonStar(VertexHelper mesh, Rect r, float scale, Color tint) {
            var start = mesh.currentVertCount;
            mesh.AddVert(r.center, tint, Vector2.zero);
            for (var i = 0; i < 10; i++) {
                var angle = (90 + i * 36) * Mathf.Deg2Rad;
                var radius = Mathf.Min(r.width, r.height) * 0.5f * scale * (i % 2 == 0 ? 1 : 0.46f);
                var point = r.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                point = new Vector2(Mathf.Round(point.x), Mathf.Round(point.y));
                mesh.AddVert(point, tint, Vector2.zero);
            }
            for (var i = 0; i < 10; i++) mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % 10);
        }

        private static void Quad(VertexHelper mesh, Rect r, Color tint) {
            var i = mesh.currentVertCount;
            mesh.AddVert(new Vector3(r.xMin, r.yMin), tint, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMin, r.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax, r.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMax, r.yMin), tint, Vector2.zero);
            mesh.AddTriangle(i, i + 1, i + 2);
            mesh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
