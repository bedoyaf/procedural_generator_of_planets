using UnityEngine;
/*
public class GradientTextureGenerator : MonoBehaviour
{
    [SerializeField] private Gradient biomeGradient;
    [SerializeField, Range(16, 1024)] private int resolution = 256;
    [SerializeField] private Material targetMaterial;

    private void Start()
    {
        Texture2D gradientTex = GenerateGradientTexture(biomeGradient, resolution);
        gradientTex.wrapMode = TextureWrapMode.Clamp;
        gradientTex.filterMode = FilterMode.Bilinear;

        targetMaterial.SetTexture("_BiomeGradient", gradientTex);
    }

    private Texture2D GenerateGradientTexture(Gradient gradient, int width)
    {
        Texture2D tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);

        for (int x = 0; x < width; x++)
        {
            float t = x / (float)(width - 1);
            tex.SetPixel(x, 0, gradient.Evaluate(t));
        }

        tex.Apply();
        return tex;
    }
}*/
