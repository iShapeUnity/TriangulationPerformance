using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;

namespace Source
{
    public class PShape : MonoBehaviour
    {

        private MeshFilter meshFilter;
        private Mesh mesh;
        public Text sceneText;

        public TriangulationJobHandler triangulationJobHandler;

        private int state = 1;
        
        public void ButtonClick()
        {
            if (state == 0)
            {
                state = 1;
                sceneText.text = "Delaunay";
            }
            else if (state == 1)
            {
                state = 2;
                sceneText.text = "Earcut";
            }
            else
            {
                state = 0;
                sceneText.text = "Monotone";
            }
        }

        private void Awake()
        {
            this.meshFilter = gameObject.GetComponent<MeshFilter>();
            this.mesh = new Mesh();
            this.mesh.MarkDynamic();
            this.meshFilter.mesh = mesh;
        }

        private float k = 0.2f;
        private float d = 0.005f;

        private void Update()
        {
            if (k < 0.1f)
            {
                d = 0.005f;
            }
            else if (k > 0.5f)
            {
                d = -0.005f;
            }

            k += d;
            this.UpdateMesh(64, 8f);
        }

        private void LateUpdate()
        {
            if (triangulationJobHandler != null)
            {
                var triangles = triangulationJobHandler.Complete();
                this.mesh.triangles = triangles;
                triangulationJobHandler = null;
            }
        }

        private void UpdateMesh(int n, float radius)
        {

            float da0 = 2f * Mathf.PI / n;
            float da1 = 16f * Mathf.PI / n;
            float delta = k * radius;

            var hull = new NativeArray<Vector2>(n, Allocator.Temp);

            float a0 = 0f;
            float a1 = 0f;

            var vertices = new Vector3[2 * n];

            for (int i = 0; i < n; ++i)
            {
                float r = radius + delta * Mathf.Sin(a1);
                float x = r * Mathf.Cos(a0);
                float y = r * Mathf.Sin(a0);
                a0 -= da0;
                a1 -= da1;
                hull[i] = new Vector2(x, y);

                vertices[i] = new Vector3(x, y);
            }

            var hole = new NativeArray<Vector2>(n, Allocator.Temp);

            for (int i = 0; i < n; ++i)
            {
                var v = 0.5f * hull[n - i - 1];
                hole[i] = v;
                vertices[n + i] = v;
            }

            this.mesh.vertices = vertices;
            switch (state)
            {
                case 0:
                    triangulationJobHandler = new TriangulationJobHandler();
                    triangulationJobHandler.Invoke(hull, hole, false);
                    break;
                case 1:
                    triangulationJobHandler = new TriangulationJobHandler();
                    triangulationJobHandler.Invoke(hull, hole, true);
                    break;
                default:
                    this.mesh.triangles = UpdateC(hull, hole);
                    break;
            }

            hull.Dispose();
            hole.Dispose();
        }

        private int[] UpdateC(NativeArray<Vector2> hull, NativeArray<Vector2> hole)
        {
            int n = hull.Length;
            int m = hole.Length;

            var vertices = new float[2 * (n + m)];

            int j = 0;
            for (int i = 0; i < n; ++i)
            {
                var p = hull[i];
                vertices[j++] = p.x;
                vertices[j++] = p.y;
            }

            for (int i = 0; i < m; ++i)
            {
                var p = hole[i];
                vertices[j++] = p.x;
                vertices[j++] = p.y;
            }

            var data = vertices.ToList();
            var holeIndices = new[] {n}.ToList();

            var indices = EarcutLibrary.Earcut(data, holeIndices, 2);

            return indices.ToArray();
        }

    }
}