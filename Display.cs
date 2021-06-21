using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Display : MonoBehaviour
{
    public Bounds bounds;
    /// <summary>
    /// The material used to draw the vector field. Must be capable of handling GPU instancing. 
    /// </summary>
    [SerializeField]
    public Material pointerMaterial;
    /// <summary>
    /// The mesh to draw the pointers from.
    /// </summary>
    [SerializeField]
    protected Mesh pointerMesh; // Should this be universal?

    public float maxVectorLength;

    /// <summary>
    /// Displays the vectors in vectorsBuffer at the positions in positionBuffer.
    /// </summary>
    /// <param name="positionBuffer"></param>
    /// <param name="vectorsBuffer"></param>
    public abstract void DisplayVectors(ComputeBuffer positionBuffer, ComputeBuffer vectorsBuffer);
}
