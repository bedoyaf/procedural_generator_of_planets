using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "PureFractalLayer", menuName = "Planet Generation/Pure Fractal Layer", order = 103)]
public class PureFractalLayerSO : TerrainLayerSO
{
    [Header("Fractal Noise Settings")]
    [Range(0.1f, 4)] public float noiseScale = 1.0f;
    [Range(0.1f, 4)] public float heightMultiplier = 1.0f;

    [Range(1, 12)] public int octaves = 6;
    [Range(0.1f, 1.0f)] public float persistence = 0.5f;
    [Range(1.0f, 4.0f)] public float lacunarity = 2.0f;

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


        computeShader.SetVector("noiseOffset", randomOffset);
        computeShader.SetInt("octaves", octaves);
        computeShader.SetFloat("persistence", persistence);
        computeShader.SetFloat("lacunarity", lacunarity);
    }

    public override void ReleaseAnySpecificBuffers()
    {

    }
}