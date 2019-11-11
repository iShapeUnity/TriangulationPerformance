using System.Linq;
using iShape.Mesh.Util;
using UnityEngine;
using UnityEngine.UI;
using Unity.Collections;

namespace Source {

    public class PShape : MonoBehaviour {

        private MeshFilter meshFilter;
        private Mesh mesh;
        public Text sceneText;
        public TriangulationJobHandler triangulationJobHandler;

        private int state = 0;
        public NativeArray<Vector2> points; 

        public void ButtonClick() {
            switch (state) {
                case 0:
                    state = 1;
                    sceneText.text = "Delaunay";
                    break;
                case 1:
                    state = 2;
                    sceneText.text = "Earcut";
                    break;
                default:
                    state = 0;
                    sceneText.text = "Monotone";
                    break;
            }
        }

        private void Awake() {
            Application.targetFrameRate = 60;
            this.meshFilter = gameObject.GetComponent<MeshFilter>();
            this.mesh = new Mesh();
            this.mesh.MarkDynamic();
            this.meshFilter.mesh = mesh;
            this.ButtonClick();
        }

        private float k = 0.0f;
        private float d = 0.005f;

        private void Update() {
            if (k < -0.5f) {
                d = 0.005f;
            } else if (k > 0.5f) {
                d = -0.005f;
            }

            k += d;
            // this.UpdateShape(128, 4f);
            this.UpdateShape(32, 4f);
//            this.UpdateMesh(64, 8f);
        }

        private void LateUpdate() {
            if (triangulationJobHandler != null) {
                var triangles = triangulationJobHandler.Complete();
                this.UpdateMesh(triangles);
                triangulationJobHandler = null;
            }
        }

        private void UpdateShape(int n, float radius) {
            float da0 = 2f * Mathf.PI / n; 
            float da1 = 16f * Mathf.PI / n;
            float delta = k * radius;

            var hull = new NativeArray<Vector2>(n, Allocator.Temp);

            float a0 = 0f;
            float a1 = 0f;

            for (int i = 0; i < n; ++i) {
                // float r = radius + delta * Mathf.Sin(a1);
                float r = radius + delta;
                float x = r * Mathf.Cos(a0);
                float y = r * Mathf.Sin(a0);
                a0 -= da0;
                // a1 -= da1;
                hull[i] = new Vector2(x, y);
            }

            var hole = new NativeArray<Vector2>(n, Allocator.Temp);

            for (int i = 0; i < n; ++i) {
                var v = 0.5f * hull[n - i - 1];
                hole[i] = v;
            }

            switch (state) {
                case 0:
                    triangulationJobHandler = new TriangulationJobHandler();
                    triangulationJobHandler.Invoke(hull, hole, false);
                    break;
                case 1:
                    triangulationJobHandler = new TriangulationJobHandler();
                    triangulationJobHandler.Invoke(hull, hole, true);
                    break;
                default:
                    UpdateC(hull, hole);
                    break;
            }

            points = new NativeArray<Vector2>(n << 1, Allocator.Persistent);
            points.Slice(0, n).CopyFrom(hull.Slice(0, n));
            points.Slice(n, n).CopyFrom(hull.Slice(0, n));

            hull.Dispose();
            hole.Dispose();
        }

        private void UpdateC(NativeArray<Vector2> hull, NativeArray<Vector2> hole) {
            int n = hull.Length;
            int m = hole.Length;

            var vertices = new float[2 * (n + m)];

            int j = 0;
            for (int i = 0; i < n; ++i) {
                var p = hull[i];
                vertices[j++] = p.x;
                vertices[j++] = p.y;
            }

            for (int i = 0; i < m; ++i) {
                var p = hole[i];
                vertices[j++] = p.x;
                vertices[j++] = p.y;
            }

            var data = vertices.ToList();
            var holeIndices = new[] {n}.ToList();

            var indices = EarcutLibrary.Earcut(data, holeIndices, 2);

            this.UpdateMesh(indices.ToArray());
        }

        private void UpdateMesh(int[] triangles) {
            
            var indices = new NativeArray<int>(triangles, Allocator.Temp);
            var nMesh = NetBuilder.Build(points, indices, 0.05f, Allocator.Temp);
            // var nMesh = PathBuilder.BuildClosedPath(points, 0.1f, Allocator.Temp);
            indices.Dispose();
            this.mesh.vertices = nMesh.vertices.ToArray();
            this.mesh.triangles = nMesh.triangles.ToArray();

            nMesh.Dispose();
            points.Dispose();
        }


    }

}