using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

namespace ProceduralMeshes.Generators
{

    public struct UVSphere : IMeshGenerator 
    {
        public int VertexCount => (Resolution + 1) * (Resolution + 1);

        public int IndexCount => 6 * Resolution * Resolution;

        public int JobLength => Resolution + 1;

        public int Resolution { get; set; }

        public void Execute<S>(int u, S streams) where S : struct, IMeshStreams {
            int vi = (Resolution + 1) * u, ti = 2 * Resolution * (u - 1);

            var vertex = new Vertex();
            //vertex.normal.y = 1f;
            vertex.tangent.w = -1f;

            vertex.position.x = sin(2f * PI * u / Resolution);
            vertex.position.z = -cos(2f * PI * u / Resolution);
            vertex.normal = vertex.position;
            vertex.tangent.x = -vertex.position.z;
            vertex.tangent.z = vertex.position.x;

            vertex.texCoord0.x = (float)u / Resolution;
            streams.SetVertex(vi, vertex);
            vi += 1;


            vi += 1;

            for (int v = 1; v <= Resolution; v++, vi++, ti+=2) {
                vertex.position.y = (float)v / Resolution;
                vertex.texCoord0.y = (float)v / Resolution;
                streams.SetVertex(vi, vertex);
                if (u > 0)
                {
                    streams.SetTriangle(
                    ti + 0, vi + int3(-Resolution - 2, -Resolution-1, - 1)
                    );
                    streams.SetTriangle(
                        ti + 1, vi + int3(- 1,-Resolution -1, 0)
                    );
                }
            }
        }

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
    }
}