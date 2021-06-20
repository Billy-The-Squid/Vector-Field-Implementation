using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// This class is built to manage the interactions between the different scripts that create a vector field. 
/// Given a FieldZone, it calls upon that for the positions of each vector to be plotted, then uses that 
/// to calculate the value of the buffer at each point. Finally, it plots these results with GPU instancing.
/// 
/// Does not currently support a changing number of points. 
/// </summary>
[RequireComponent(typeof(FieldZone))]
public class VectorField : MonoBehaviour
{
    /// <summary>
    /// The <cref>FieldZone</cref> object used to determine positions.
    /// </summary>
    public FieldZone zone { get; set; }
    // Why is this not serializable? %%%%%%%%%%

    /// <summary>
    /// The buffer in which the vector worldspace positions are stored.
    /// </summary>
    public ComputeBuffer positionsBuffer { get; protected set; }
    /// <summary>
    /// The buffer in which the vector values are stored. 
    /// Same indexing scheme as <cref>positionsBuffer</cref>.
    /// </summary>
    public ComputeBuffer vectorsBuffer { get; protected set; }
    /// <summary>
    /// The buffer in which the visual magnitudes of each vector are stored. 
    /// Same indexing scheme as <cref>positionsBuffer</cref>.
    /// </summary>
    public ComputeBuffer plotVectorsBuffer { get; protected set; }
    /// <summary>
    /// One of two buffers in which values used for calculating the transformation matrix for vectors are stored.
    /// Same indexing scheme as <cref>positionsBuffer</cref>.
    /// 
    /// Contains vectors orthogonal to those in <cref>plotVectorsBuffer</cref>, with the same magnitude, in order 
    /// to generate an orthogonal basis. 
    /// </summary>
    public ComputeBuffer vector2Buffer { get; protected set; }
    /// <summary>
    /// One of two buffers in which values used for calculating the transformation matrix for vectors are stored.
    /// Same indexing scheme as <cref>positionsBuffer</cref>.
    /// 
    /// Contains vectors orthogonal to those in <cref>plotVectorsBuffer</cref>, with the same magnitude, in order 
    /// to generate an orthogonal basis. 
    /// </summary>
    public ComputeBuffer vector3Buffer { get; protected set; }
    /// <summary>
    /// Stores the magnitudes of the vectors in <cref>vectorsBuffer</cref>. 
    /// Same indexing scheme as <cref>positionsBuffer</cref>.
    /// </summary>
    public ComputeBuffer magnitudesBuffer { get; protected set; }

    /// <summary>
    /// The number of points at which vectors will be plotted and the number of values in each buffer.
    /// </summary>
    int numOfPoints;

    /// <summary>
    /// The compute shader used to generate the vector field. 
    /// </summary>
    [SerializeField]
    ComputeShader computeShader;

    // Property IDs used to send values to various shaders.
    static readonly int
        centerID = Shader.PropertyToID("_CenterPosition"),
        positionsBufferID = Shader.PropertyToID("_Positions"),
        vectorBufferID = Shader.PropertyToID("_Vectors"),
        plotVectorsBufferID = Shader.PropertyToID("_PlotVectors"),
        vector2BufferID = Shader.PropertyToID("_Vectors2"),
        vector3BufferID = Shader.PropertyToID("_Vectors3"),
        magnitudesBufferID = Shader.PropertyToID("_Magnitudes"),
        maxVectorLengthID = Shader.PropertyToID("_MaxVectorLength"),
        fieldIndexID = Shader.PropertyToID("_FieldIndex");

    /// <summary>
    /// The material used to draw the vector field. Must be capable of handling GPU instancing. 
    /// </summary>
    [SerializeField]
    public Material pointerMaterial;
    /// <summary>
    /// The mesh to draw the pointers from.
    /// </summary>
    [SerializeField]
    Mesh pointerMesh;

    /// <summary>
    /// The possible types of field to display. 
    /// It is the user's responsibility to make sure that these selections align with those in FieldLibrary.hlsl
    /// </summary>
    public enum FieldType { Outwards, Swirl }
    /// <summary>
    /// The type of field to be displayed. Cannot be changed in Play Mode if <cref>isDynamic</cref> is set to False.
    /// </summary>
    [SerializeField]
    public FieldType fieldType;

    /// <summary>
    /// Set this to true if the field should update when the transform is moved in Play Mode. 
    /// Requires more GPU time. Sets <cref>isDynamic</cref> to true as well. 
    /// </summary>
    [SerializeField]
    bool canMove;
    /// <summary>
    /// Set this to true if the field values should be updated each frame. 
    /// Requires more GPU time. 
    /// </summary>
    [SerializeField]
    bool isDynamic;





    private void Awake()
    {
        if (zone == null)
        {
            zone = GetComponent<FieldZone>();
        }
    }

    private void OnEnable()
    {
        zone.SetPositions();

        positionsBuffer = zone.positionBuffer;
        numOfPoints = positionsBuffer.count;

        unsafe // <-- This could maybe be a source of problems.
        {
            vectorsBuffer = new ComputeBuffer(numOfPoints, sizeof(Vector3)); // last arg: size of single object
            plotVectorsBuffer = new ComputeBuffer(numOfPoints, sizeof(Vector3));
            vector2Buffer = new ComputeBuffer(numOfPoints, sizeof(Vector3));
            vector3Buffer = new ComputeBuffer(numOfPoints, sizeof(Vector3));
            magnitudesBuffer = new ComputeBuffer(numOfPoints, sizeof(float));
        }

        CalculateVectors();
    }

    private void OnDisable()
    {
        vectorsBuffer.Release();
        vectorsBuffer = null;

        plotVectorsBuffer.Release();
        plotVectorsBuffer = null;

        vector2Buffer.Release();
        vector2Buffer = null;

        vector3Buffer.Release();
        vector3Buffer = null;

        magnitudesBuffer.Release();
        magnitudesBuffer = null;
    }



    // Update is called once per frame
    void Update()
    {
        if(canMove) {
            zone.SetPositions();
            isDynamic = true;
        }

        if(isDynamic)
        {
            CalculateVectors();
        }
    }

    private void LateUpdate()
    {
        PlotResults();
    }


    /// <summary>
    /// Interfaces with the <cref>computeShader</cref> and calculates the value of the vectors at each point, storing them in the buffers. 
    /// </summary>
    private void CalculateVectors()
    {
        // The data is sent to the computeShader for calculation
        computeShader.SetVector(centerID, zone.fieldOrigin);
        computeShader.SetFloat(maxVectorLengthID, zone.maxVectorLength);
        computeShader.SetInt(fieldIndexID, (int)fieldType);

        int kernelID = (int)fieldType;
        computeShader.SetBuffer(kernelID, positionsBufferID, positionsBuffer);
        computeShader.SetBuffer(kernelID, vectorBufferID, vectorsBuffer);
        computeShader.SetBuffer(kernelID, plotVectorsBufferID, plotVectorsBuffer);
        computeShader.SetBuffer(kernelID, vector2BufferID, vector2Buffer);
        computeShader.SetBuffer(kernelID, vector3BufferID, vector3Buffer);
        computeShader.SetBuffer(kernelID, magnitudesBufferID, magnitudesBuffer);
        // Why does this need to be redone every frame?

        // This does the math and stores information in the positionsBuffer.
        int groups = Mathf.CeilToInt(numOfPoints / 64f);
        computeShader.Dispatch(kernelID, groups, 1, 1);
    }


    /// <summary>
    /// Interfaces with the <cref>pointerMaterial</cref> to display the vector field. 
    /// </summary>
    void PlotResults()
    {
        // Then the data from the computeShader is sent to the shader to be rendered.
        pointerMaterial.SetBuffer(positionsBufferID, positionsBuffer);
        pointerMaterial.SetBuffer(plotVectorsBufferID, plotVectorsBuffer);
        pointerMaterial.SetBuffer(vector2BufferID, vector2Buffer);
        pointerMaterial.SetBuffer(vector3BufferID, vector3Buffer);
        pointerMaterial.SetBuffer(magnitudesBufferID, magnitudesBuffer);

        // Setting the bounds and giving a draw call
        var bounds = zone.bounds;
        Graphics.DrawMeshInstancedProcedural(pointerMesh, 0, pointerMaterial, bounds, numOfPoints);
    }
}
