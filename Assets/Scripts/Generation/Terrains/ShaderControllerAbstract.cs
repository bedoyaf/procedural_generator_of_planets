using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public abstract class ShaderControllerAbstract : MonoBehaviour
{
    protected MeshFilter _filter;
    protected Vector3[] originalVertices;
    protected Vector3[] deformedVertices;

    [SerializeField] protected ComputeShader computeShader;
    protected ComputeBuffer craterBuffer;
    protected ComputeBuffer verticesBuffer;
    protected ComputeBuffer heightsBuffer;

    protected int numVertices;

    protected float[] heights;


    [ContextMenu("Run Compute Shader")]
    public abstract void RunComputeShader();
}
