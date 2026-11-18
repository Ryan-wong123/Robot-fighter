using UnityEngine;
using UnityEngine.UI;

namespace WobbleBrawl.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ComicImpactShape : MaskableGraphic
    {
        private static readonly Color32 Ink = new Color32(53, 30, 52, 255);
        private ImpactKind kind;

        public void Show(ImpactKind impact, Color tint)
        {
            kind = impact;
            color = tint;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (kind == ImpactKind.Block || kind == ImpactKind.GuardBreak)
            {
                Ring(mesh, kind == ImpactKind.Block ? 12 : 7);
                return;
            }

            int rays = kind switch
            {
                ImpactKind.Light => 5,
                ImpactKind.Special => 12,
                ImpactKind.Knockout => 16,
                _ => 9
            };
            for (int ray = 0; ray < rays; ray++)
            {
                float angle = ray * 2f * Mathf.PI / rays;
                Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new Vector2(-axis.y, axis.x);
                float length = ray % 2 == 0 ? 52f : 43f;
                Triangle(mesh, axis * 19f - tangent * 10f, axis * length, axis * 19f + tangent * 10f, Ink);
                Triangle(mesh, axis * 21f - tangent * 7f, axis * (length - 4f), axis * 21f + tangent * 7f, color);
            }
            if (kind == ImpactKind.Heavy || kind == ImpactKind.Special)
                Ring(mesh, 12);
        }

        private void Ring(VertexHelper mesh, int segments)
        {
            for (int index = 0; index < segments; index++)
            {
                float angle = index * 2f * Mathf.PI / segments;
                Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new Vector2(-axis.y, axis.x);
                float radius = kind == ImpactKind.GuardBreak && index % 2 == 0 ? 51f : 39f;
                Triangle(mesh, axis * (radius - 10f), axis * (radius + 4f) - tangent * 10f,
                    axis * (radius + 4f) + tangent * 10f, Ink);
                Triangle(mesh, axis * (radius - 7f), axis * radius - tangent * 7f,
                    axis * radius + tangent * 7f, color);
            }
        }

        private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color32 tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
