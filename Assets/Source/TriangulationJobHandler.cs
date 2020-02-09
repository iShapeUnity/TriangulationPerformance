using iShape.Geometry;
using iShape.Geometry.Container;
using iShape.MeshUtil;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Source {

    public class TriangulationJobHandler {

        private TriangulationJob job;
        private JobHandle jobHandle;
        
        public void Invoke(NativeArray<Vector2> hull, NativeArray<Vector2> hole, bool isDelaunay) {
            var iGeom = IntGeom.DefGeom;

            var iHull = iGeom.Int(hull.ToArray());
            var iHoles = new[] { iGeom.Int(hole.ToArray()) };

            var iShape = new IntShape(iHull, iHoles);
            var pShape = new PlainShape(iShape, Allocator.TempJob);

            int totalCount = pShape.points.Length * 3;

            var triangles = new NativeArray<int>(totalCount, Allocator.TempJob);
            var length = new NativeArray<int>(1, Allocator.TempJob);
            
            this.job = new TriangulationJob {
                plainShape = pShape,
                isDelaunay = isDelaunay,
                triangles = triangles,
                length = length
            };
            
            this.jobHandle = this.job.Schedule();
        }

        public NetBuilder.Shape Complete(Allocator allocator) {
            this.jobHandle.Complete();
            
            int n = this.job.length[0];
            var triangles = new NativeArray<int>(n, allocator);
            triangles.Slice(0, n).CopyFrom(job.triangles.Slice(0, n));
            
            var plainShape = job.plainShape;
            
            var points = IntGeom.DefGeom.Float(plainShape.points, allocator);
            
            n = plainShape.layouts.Length;
            var paths = new NativeArray<NetBuilder.Path>(n, allocator);
            for (int i = 0; i < n; ++i) {
                var layout = plainShape.layouts[i];
                paths[i] = new NetBuilder.Path(layout.begin, layout.end, layout.isClockWise);
            }

            this.job.Dispose();

            return new NetBuilder.Shape(triangles, points, paths);
        }

    }
}