using UnityEngine;

[CreateAssetMenu(fileName = "FractalLayerDetail", menuName = "Planet Generation/Fractal Layer Detail", order = 103)]
public class FractalLayerDetail : TerrainLayerSO
{
    [Header("Small-Scale Ocean Detail Settings")]
    public float detailScale = 12.0f;
    public float detailLacunarity = 2.2f;
    public float detailPersistence = 0.4f;
    public int detailOctaves = 2;
    public float heightMultiplier = 1.0f;

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
        computeShader.SetVector("noiseOffset", offset);
    }

    public override void ReleaseAnySpecificBuffers()
    {

    }
}
