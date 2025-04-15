using UnityEngine;

public class NoiseShaderController2 : ShaderControllerAbstract
{
    public float baseRadius = 10f;
    public float noiseScale = 1.0f;
    public float heightMultiplier = 1.0f;
    public Vector3 noiseOffset = Vector3.zero;

    [Range(1, 12)] public int octaves = 6;
    [Range(0.1f, 1.0f)] public float persistence = 0.5f;
    [Range(1.0f, 4.0f)] public float lacunarity = 2.0f;

    [Range(0.0f, 1.0f)] public float ridgeFactor = 0.0f;
    [Range(0.1f, 5.0f)] public float powerExponent = 1.0f;

    // Keep a pristine copy of the original vertices
    private Vector3[] originalVerticesInternal;


    public override void SetupTerrainGenerator(MeshFilter meshFilter, Vector3[] originalVertices, float sphereRadius)
    {
        _filter = meshFilter;
        if (originalVertices == null || originalVertices.Length == 0)
        {
            Debug.LogError("SetupTerrainGenerator received null or empty originalVertices array.");
            return;
        }

        // --- Critical: Store a *copy* of the original vertices ---
        // This ensures our internal reference doesn't get changed if the input array is modified elsewhere,
        // and prevents accidental modification if we assigned by reference initially.
        this.originalVerticesInternal = (Vector3[])originalVertices.Clone();
        // ---

        this.numVertices = this.originalVerticesInternal.Length;

        // Allocate the array for deformed vertices (separate from originals)
        this.deformedVertices = new Vector3[this.numVertices];

        // Allocate the array for reading back heights
        this.heights = new float[this.numVertices];
        this.sphereRadius = sphereRadius;

        // Release existing buffers if they exist (e.g., if Setup is called again)
        ReleaseBuffers();

        // Create new compute buffers
        verticesBuffer = new ComputeBuffer(numVertices, sizeof(float) * 3);
        heightsBuffer = new ComputeBuffer(numVertices, sizeof(float));

        // Populate the vertices buffer *once* during setup with the pristine original data
        verticesBuffer.SetData(this.originalVerticesInternal);

        Debug.Log($"Setup complete for {numVertices} vertices.");
    }


    [ContextMenu("Run Compute Shader")]
    public override void RunComputeShader()
    {
        // Ensure setup has been done and buffers exist
        if (_filter == null || verticesBuffer == null || heightsBuffer == null || originalVerticesInternal == null || deformedVertices == null || heights == null)
        {
            Debug.LogError("Setup not completed or buffers are null. Please call SetupTerrainGenerator first.");
            return;
        }

        // Verify vertex count consistency (optional but good practice)
        if (_filter.mesh.vertexCount != this.numVertices)
        {
            Debug.LogWarning($"Mesh vertex count ({_filter.mesh.vertexCount}) differs from setup count ({this.numVertices}). Re-running setup might be necessary if the mesh changed.");
            // Optionally, you could attempt to re-run setup here, but it depends on your workflow.
            // For now, we'll proceed assuming the setup count is authoritative for the buffers.
        }

        // --- Ensure the verticesBuffer contains the ORIGINAL vertices ---
        // Since we set it in Setup and don't change it, it should already be correct.
        // If you needed to dynamically change the *input* mesh shape *before* applying noise,
        // you would update originalVerticesInternal and call verticesBuffer.SetData here.
        // But for reapplying noise to the *same base shape*, this is not needed.
        // verticesBuffer.SetData(this.originalVerticesInternal); // Usually not needed here if set in Setup

        // Find the kernel index (assuming the kernel name is correct)
        int kernelHandle = -1;
        try
        {
            // Use the correct kernel name from your updated shader
            kernelHandle = computeShader.FindKernel("GenerateSphereNoiseTrippy");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to find kernel 'GenerateSphereNoise' in Compute Shader '{computeShader.name}'. Exception: {e.Message}");
            return;
        }
        if (kernelHandle < 0)
        {
            Debug.LogError($"Kernel 'GenerateSphereNoise' not found in Compute Shader '{computeShader.name}'.");
            return;
        }


        // Set parameters (ensure these use the class fields)
        computeShader.SetInt("numVertices", this.numVertices);
        computeShader.SetFloat("baseRadius", baseRadius);
        computeShader.SetFloat("noiseScale", noiseScale);
        computeShader.SetFloat("heightMultiplier", heightMultiplier);
        computeShader.SetVector("noiseOffset", noiseOffset);
        computeShader.SetInt("octaves", octaves);
        computeShader.SetFloat("persistence", persistence);
        computeShader.SetFloat("lacunarity", lacunarity);
        computeShader.SetFloat("ridgeFactor", ridgeFactor);
        computeShader.SetFloat("powerExponent", powerExponent);

        // Set buffers (kernel index, buffer name, buffer object)
        computeShader.SetBuffer(kernelHandle, "vertices", verticesBuffer); // Input original positions
        computeShader.SetBuffer(kernelHandle, "heights", heightsBuffer);   // Output heights

        // Dispatch the compute shader
        int threadGroups = Mathf.CeilToInt(this.numVertices / 512.0f);
        computeShader.Dispatch(kernelHandle, threadGroups, 1, 1);

        // Get the results back from the GPU
        heightsBuffer.GetData(heights);

        // --- Apply deformation based on ORIGINAL vertices and calculated heights ---
        for (int i = 0; i < this.numVertices; i++)
        {
            // Calculate the new position by starting from the original position,
            // normalizing it (to get the direction from the center), and scaling by the calculated height.
            // This assumes originalVerticesInternal represent points on a sphere *before* baseRadius scaling.
            // If originalVerticesInternal are already at baseRadius, you might adjust this logic.
            // Assuming originalVerticesInternal are normalized directions or points on a unit sphere:
            deformedVertices[i] = originalVerticesInternal[i].normalized * heights[i];

            // If originalVerticesInternal were already points on the baseRadius sphere:
            // Vector3 direction = originalVerticesInternal[i].normalized;
            // deformedVertices[i] = direction * heights[i];
        }

        // Get the mesh reference
        Mesh mesh = _filter.mesh;

        // Apply the calculated vertices to the actual mesh
        mesh.vertices = deformedVertices;

        // Recalculate normals and bounds for correct lighting and rendering
        mesh.RecalculateNormals();
        mesh.RecalculateBounds(); // Important for culling and effects

        // Debug.Log($"Compute Shader executed. Dispatched {threadGroups} groups for {this.numVertices} vertices.");
    }
 
    void OnDestroy()
    {
        ReleaseBuffers();
    }

    // Public method to allow manual releasing if needed
    public void ReleaseBuffers()
    {
        verticesBuffer?.Release(); // Safely release using null-conditional operator
        verticesBuffer = null;
        heightsBuffer?.Release();
        heightsBuffer = null;
        // Debug.Log("Compute buffers released.");
    }

}
