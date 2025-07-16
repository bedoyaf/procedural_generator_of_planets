using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Layer from TerrainLayerSO, focused on creating a rough surface
/// </summary>
[CreateAssetMenu(fileName = "FractalLayerDetail", menuName = "Planet Generation/Fractal Layer Detail", order = 103)]
public class FractalLayerDetail : TerrainLayerSO
{
    [Header("Small-Scale Ocean Detail Settings")]
    [SerializeField] Vector3 noiseOffset = Vector3.zero;
    [SerializeField,UnityEngine.Range(0.1f,100)] private float detailScale = 12.0f;
    [SerializeField, UnityEngine.Range(0.1f, 20)] private float detailLacunarity = 10f;
    [SerializeField, UnityEngine.Range(0.1f, 20)] private float detailPersistence = 5f;
    [SerializeField, UnityEngine.Range(1, 10)] private int detailOctaves = 3;
    [SerializeField, UnityEngine.Range(0, 3)] private float heightMultiplier = 0.01f;

    /// <summary>
    /// Sets up the shader with the buffers and serialized fields
    /// </summary>
    /// <param name="positionBuffer">buffer of the mesh positions</param>
    /// <param name="heightBuffer">buffer of the heights(start at 0)</param>
    /// <param name="numVertices">the number of vertices in the mesh</param>
    public override void SetShaderParameters( ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices)
    {
        if (!layerEnabled || computeShader == null || kernelHandle < 0 || positionBuffer == null || heightBuffer == null)
        {
            Debug.LogError($"Skipping layer '{this.name}' due to missing requirements.", this);
            return;
        }

        computeShader.SetBuffer(kernelHandle, "vertices", positionBuffer);
        computeShader.SetBuffer(kernelHandle, "heights", heightBuffer);
        computeShader.SetInt("numVertices", numVertices);

        computeShader.SetFloat("detailScale", detailScale);
        computeShader.SetFloat("detailLacunarity", detailLacunarity);
        computeShader.SetFloat("detailPersistence", detailPersistence);
        computeShader.SetInt("detailOctaves", detailOctaves);
        computeShader.SetFloat("heightMultiplier", heightMultiplier);

        Vector3 offset = new Vector3(
            Random.Range(-1000f, 1000f),
            Random.Range(-1000f, 1000f),
            Random.Range(-1000f, 1000f)
        );
        computeShader.SetVector("noiseOffset", (offset+noiseOffset));
    }

    public override void ReleaseAnySpecificBuffers(){}
}
