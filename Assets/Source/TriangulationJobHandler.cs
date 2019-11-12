using iShape.Geometry;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Source {

    public class TriangulationJobHandler {

        public struct Result {
            internal int[] triangles;
            internal Vector2[] points;
        }

        private TriangulationJob job;
        private JobHandle jobHandle;
        
        public void Invoke(NativeArray<Vector2> hull, NativeArray<Vector2> hole, bool isDelaunay) {
            var iGeom = IntGeom.DefGeom;

            var iHull = iGeom.Int(hull.ToArray());
            var iHoles = new IntVector[1][] { iGeom.Int(hole.ToArray()) };

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

        public Result Complete() {
            this.jobHandle.Complete();
            
            int n = this.job.length[0];
            var triangles = new NativeArray<int>(n, Allocator.Temp);
            triangles.Slice(0, n).CopyFrom(job.triangles.Slice(0, n));

            var iGeom = IntGeom.DefGeom;
            var points = IntGeom.DefGeom.Float(job.plainShape.points.ToArray());
            
            this.job.Dispose();
            
            var array = triangles.ToArray();
            triangles.Dispose();

            return new Result() {
                triangles = array,
                points = points,
            };
        }

    }
}