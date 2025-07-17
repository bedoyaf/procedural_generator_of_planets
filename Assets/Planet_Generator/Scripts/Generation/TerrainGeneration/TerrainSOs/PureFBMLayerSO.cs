using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Layer from TerrainLayerSO, focused on utilising just fBm
/// </summary>
[CreateAssetMenu(fileName = "PureFBMLayer", menuName = "Planet Generation/Pure FBM Layer", order = 103)]
public class PureFBMLayerSO : TerrainLayerSO
{
    [Header("Fractal Noise Settings")]
    [SerializeField] Vector3 noiseOffset = Vector3.zero;
    [Range(0.1f, 4)] public float noiseScale = 1.0f;
    [Range(0.1f, 4)] public float heightMultiplier = 1.0f;

    [Range(1, 12)] public int octaves = 6;
    [Range(0.1f, 1.0f)] public float persistence = 0.5f;
    [Range(1.0f, 4.0f)] public float lacunarity = 2.0f;

    /// <summary>
    /// Sets up the shader with the buffers and serialized fields
    /// </summary>
    /// <param name="positionBuffer">buffer of the mesh positions</param>
    /// <param name="heightBuffer">buffer of the heights(start at 0)</param>
    /// <param name="numVertices">the number of vertices in the mesh</param>
    public override void SetShaderParameters( ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices)
    {
        // Basic validation
        if (!layerEnabled || computeShader == null || kernelHandle < 0 || positionBuffer == null || heightBuffer == null)
        {
            Debug.LogError($"Skipping layer '{this.name}' due to missing requirements (enabled={layerEnabled}, computeShader={(computeShader != null)}, kernel={kernelHandle}, posBuffer={(positionBuffer != null)}, heightBuffer={(heightBuffer != null)})", this);
            return;
        }
        computeShader.SetBuffer(kernelHandle, "vertices", positionBuffer); 
        computeShader.SetBuffer(kernelHandle, "heights", heightBuffer);  
        computeShader.SetInt("numVertices", numVertices);


        computeShader.SetFloat("noiseScale", noiseScale);
        computeShader.SetFloat("heightMultiplier", heightMultiplier);

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f),
            UnityEngine.Random.Range(-1000f, 1000f)
        );


        computeShader.SetVector("noiseOffset", (randomOffset+noiseOffset));
        computeShader.SetInt("octaves", octaves);
        computeShader.SetFloat("persistence", persistence);
        computeShader.SetFloat("lacunarity", lacunarity);
    }

    public override void ReleaseAnySpecificBuffers(){}
}