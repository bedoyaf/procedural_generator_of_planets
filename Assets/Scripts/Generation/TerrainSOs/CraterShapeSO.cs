using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic; // Required for List
using System.Runtime.InteropServices; // Required for StructLayout

[CreateAssetMenu(fileName = "CraterLayer", menuName = "Planet Generation/Crater Layer", order = 101)]
public class CraterLayerSO : TerrainLayerSO
{
    // Define the Crater struct exactly as the compute shader expects it
    [StructLayout(LayoutKind.Sequential, Size = 20)] // Match HLSL size (float3 + float + float = 12 + 4 + 4 = 20 bytes)
    private struct CraterData // Renamed to avoid conflict with CraterGenerator.Crater if it still exists
    {
        public float3 center;
        public float radius;
        public float depth; // Keep depth if shader uses it, or remove
    }

    [Header("Crater Settings")]
    public int numCraters = 10;
    public Vector2 craterRadiusRange = new Vector2(0.05f, 0.2f);
    public Vector2 floorHeightRange = new Vector2(-0.5f, -0.2f); // Represents depth/displacement
    public Vector2 rimWidthRange = new Vector2(0.2f, 0.4f);
    public Vector2 rimSteepnessRange = new Vector2(1f, 3f);
    public float smoothness = 0.1f;

    // Buffer to hold crater data
    private ComputeBuffer craterBuffer;
    private List<CraterData> craterList = new List<CraterData>();
    // Cache original positions for crater placement
    private Vector3[] cachedOriginalPositions;

    // Need a way to know when to regenerate crater positions (e.g., seed change)
    private int lastUsedSeed = -1;
    private int lastNumVertices = -1;


    public override void SetShaderParameters(ComputeShader shader, int kernel, ComputeBuffer positionBuffer, ComputeBuffer heightBuffer, int numVertices, float radius)
    {
        if (!enabled || shader == null || kernel < 0) return;

        // --- Regenerate Craters if Needed ---
        // We need access to the original vertex data for placement,
        // and the seed for reproducibility. This is a bit awkward here.
        // A better approach might involve passing originalPositions explicitly
        // or having the PlanetGenerator manage crater generation.
        // For now, let's assume we can cache it somehow (or get it passed).
        // **THIS PART NEEDS REFINEMENT BASED ON HOW YOU MANAGE SEED/VERTICES**
        int currentSeed = UnityEngine.Random.state.GetHashCode(); // Or use your specific seed management
        if (craterBuffer == null || craterList.Count != numCraters || lastUsedSeed != currentSeed || lastNumVertices != numVertices)
        {
            // Need original positions - This is a design challenge for SOs.
            // We'll simulate getting them here. In reality, PlanetGenerator needs
            // to provide this or the generation logic moves.
            Vector3[] originalPositions = GetOriginalPositions(positionBuffer, numVertices); // **Helper needed**
            if (originalPositions != null)
            {
                GenerateCraterData(originalPositions, numVertices, currentSeed);
                lastUsedSeed = currentSeed;
                lastNumVertices = numVertices;
            }
            else
            {
                Debug.LogError("Could not get original positions for crater generation.", this);
                return; // Cannot proceed
            }
        }
        // ------------------------------------

        if (craterBuffer == null || craterList.Count == 0)
        {
            Debug.LogWarning($"Crater buffer not ready or empty for layer '{this.name}'. Skipping.", this);
            return; // Skip if no craters generated
        }
        this.radius = radius;

        shader.SetBuffer(kernel, "vertices", positionBuffer);
        shader.SetBuffer(kernel, "heights", heightBuffer);
        shader.SetInt("numVertices", numVertices);
        shader.SetFloat("sphereRadius", radius);

        shader.SetBuffer(kernel, "craters", craterBuffer); // Use consistent naming
        shader.SetInt("numCraters", craterList.Count);
        shader.SetFloat("smoothness", smoothness);
        // Pass random range values or fixed values based on desired behavior
        shader.SetFloat("rimSteepness", UnityEngine.Random.Range(rimSteepnessRange.x, rimSteepnessRange.y));
        shader.SetFloat("floorHeight", UnityEngine.Random.Range(floorHeightRange.x, floorHeightRange.y));
        shader.SetFloat("rimWidth", UnityEngine.Random.Range(rimWidthRange.x, rimWidthRange.y));

    }

    // **Helper function - Needs proper implementation**
    // This is a placeholder. You need a way to get the initial sphere positions.
    // Maybe PlanetGenerator passes them when calling SetShaderParameters,
    // or reads them back from the buffer if the first layer initializes it.
    private Vector3[] GetOriginalPositions(ComputeBuffer positionBuffer, int numVertices)
    {
        if (cachedOriginalPositions != null && cachedOriginalPositions.Length == numVertices)
        {
            return cachedOriginalPositions;
        }
        // Attempt to read from buffer - ONLY works if buffer contains unmodified positions
        // This might be unreliable depending on the pipeline order.
        // It's safer for PlanetGenerator to provide the base mesh vertices.
        Debug.LogWarning("Attempting to read original positions from Compute Buffer. This might be unreliable.");
        Vector3[] positions = new Vector3[numVertices];
        positionBuffer.GetData(positions);
        cachedOriginalPositions = positions;
        return positions;

        // --- SAFER APPROACH ---
        // public override void SetShaderParameters(..., Vector3[] originalVertices) {
        //     // Use originalVertices passed in
        // }
        // PlanetGenerator would need to supply this array.
    }


    private void GenerateCraterData(Vector3[] originalVertices, int numVertices, int seed)
    {
        if (originalVertices == null || originalVertices.Length != numVertices)
        {
            Debug.LogError("Invalid original vertices for crater generation.", this);
            return;
        }

        UnityEngine.Random.State previousState = UnityEngine.Random.state; // Save current random state
        UnityEngine.Random.InitState(seed); // Use the provided seed for reproducibility

        craterList.Clear();
        for (int i = 0; i < numCraters; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, numVertices);
            Vector3 randomCenter = originalVertices[randomIndex].normalized * radius; // Place on sphere surface

            float randomRadius = UnityEngine.Random.Range(craterRadiusRange.x, craterRadiusRange.y);
            // Depth is essentially floorHeight in the shader context now
            // float randomDepth = UnityEngine.Random.Range(floorHeightRange.x, floorHeightRange.y);

            craterList.Add(new CraterData
            {
                center = randomCenter,
                radius = randomRadius,
                depth = 0 // Depth might be calculated inside shader based on floorHeight etc.
            });
        }

        // Create or update crater buffer
        if (craterBuffer != null)
            craterBuffer.Release();

        if (craterList.Count > 0)
        {
            // Use Marshal.SizeOf for accurate struct size
            craterBuffer = new ComputeBuffer(craterList.Count, Marshal.SizeOf(typeof(CraterData)));
            craterBuffer.SetData(craterList);
        }
        else
        {
            craterBuffer = null; // Ensure buffer is null if no craters
        }


        UnityEngine.Random.state = previousState; // Restore random state
        Debug.Log($"Generated {craterList.Count} craters for layer '{this.name}'. Buffer size: {Marshal.SizeOf(typeof(CraterData))}", this);
    }

    // Release buffer when SO is unloaded or destroyed
    private void OnDisable()
    {
        ReleaseBuffers();
    }

    private void OnDestroy()
    {
        ReleaseBuffers();
    }

    public void ReleaseBuffers()
    {
        craterBuffer?.Release();
        craterBuffer = null;
        cachedOriginalPositions = null; // Clear cache
        Debug.Log($"Released crater buffer for layer '{this.name}'.");
    }
    /*
    protected override void Reset()
    {
        kernelName = "ComputeCrater"; // Set default kernel name
        base.Reset();
    }*/
}