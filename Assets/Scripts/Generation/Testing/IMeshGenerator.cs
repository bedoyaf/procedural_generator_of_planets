using UnityEngine;

namespace ProceduralMeshes
{

    public interface IMeshGenerator
    {
        int VertexCount { get; }

        int Resolution { get; set; }

        int IndexCount { get; }

        int JobLength { get; }

        Bounds Bounds { get; }

        void Execute<S>(int i, S streams) where S : struct, IMeshStreams;
    }
}