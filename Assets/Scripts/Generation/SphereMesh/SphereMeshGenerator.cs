using UnityEngine;
using System.Collections.Generic;

// Doesn't need to be a MonoBehaviour, just a helper class
public class SphereMeshGenerator
{
    // Enum can live here or in PlanetGenerator, depends on preference
    public enum SphereAlgorithm { Nothing = 0, SebastianLeague = 1, Optimal = 2 }

    public Vector3[] BaseVertices { get; private set; }
    public int[] Triangles { get; private set; }
    public int NumVertices { get; private set; }
    public List<int> EdgeIndices { get; private set; } // Store edge indices if needed for wireframe

    private ISphereMesh currentSphereMeshGenerator;

    public bool Generate(SphereAlgorithm algorithm, int resolution, float radius) // Use float radius internally if needed by algos
    {
        Debug.Log($"Generating Sphere Data: Algorithm={algorithm}, Resolution={resolution}, Radius={radius}");
        if (algorithm == SphereAlgorithm.Nothing) return false;

        currentSphereMeshGenerator = null; // Reset previous generator

        // --- Select and Run Algorithm ---
        // Note: Your Optimal algo takes int radius, adapt if necessary
        if (algorithm == SphereAlgorithm.SebastianLeague) currentSphereMeshGenerator = new SphereMesh(resolution);
        else if (algorithm == SphereAlgorithm.Optimal) currentSphereMeshGenerator = new SphereMeshOptimal(resolution, (int)radius);
        // Add other algorithms if needed

        if (currentSphereMeshGenerator == null)
        {
            Debug.LogError("Selected Sphere Algorithm not implemented or invalid.");
            BaseVertices = null;
            Triangles = null;
            NumVertices = 0;
            EdgeIndices = null;
            return false;
        }

        // --- Store Results ---
        BaseVertices = currentSphereMeshGenerator.Vertices;
        Triangles = currentSphereMeshGenerator.Triangles;
        NumVertices = BaseVertices?.Length ?? 0; // Handle potential null vertices

        if (NumVertices == 0)
        {
            Debug.LogError("Sphere mesh generation resulted in 0 vertices.");
            BaseVertices = null;
            Triangles = null;
            EdgeIndices = null;
            return false;
        }

        // --- Generate Edge Indices (Optional, for wireframe) ---
        EdgeIndices = currentSphereMeshGenerator.CreateEdgeIndices(Triangles);

        Debug.Log($"Sphere Data Generated: Vertices={NumVertices}, Triangles={Triangles.Length / 3}");
        return true;
    }
}