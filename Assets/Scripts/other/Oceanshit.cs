using UnityEngine;

[ExecuteInEditMode]
public class OceanPostProcess : MonoBehaviour
{
    public Material oceanMat;
    public Transform oceanSphere;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (oceanMat && oceanSphere)
        {
            oceanMat.SetVector("_OceanCenter", oceanSphere.position);
            oceanMat.SetFloat("_OceanRadius", 1f); // assumes uniform scale
            Graphics.Blit(src, dest, oceanMat);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}
