using iShape.Geometry;
using iShape.Triangulation.Shape;
using iShape.Triangulation.Shape.Delaunay;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace Source {
    
    [BurstCompile]
    public struct TriangulationJob: IJob {

        [ReadOnly]
        internal PlainShape plainShape;
        
        [ReadOnly]
        internal bool isDelaunay;

        [WriteOnly]
        internal NativeArray<int> triangles;
        
        [WriteOnly]
        internal NativeArray<int> length;

        public void Execute() {
            NativeArray<int> triangles;

            if (isDelaunay) {
                triangles = plainShape.DelaunayTriangulate(Allocator.Temp);    
            } else {
                triangles = plainShape.Triangulate(Allocator.Temp);
            }

            int n = triangles.Length;
            length[0] = n;

            this.triangles.Slice(0, n).CopyFrom(triangles.Slice(0, n));
            
            triangles.Dispose();
        }

        public void Dispose() {
            plainShape.Dispose();
            triangles.Dispose();
            length.Dispose();
        }
    }
}