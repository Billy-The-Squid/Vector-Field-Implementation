using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FieldZone))]
public class VectorField : MonoBehaviour
{
    //[SerializeField]
    public FieldZone zone { get; set; }

    public ComputeBuffer positionsBuffer { get; protected set; }
    public ComputeBuffer vectorsBuffer { get; protected set; }
    public ComputeBuffer plotVectorsBuffer { get; protected set; }
    public ComputeBuffer vector2Buffer { get; protected set; }
    public ComputeBuffer vector3Buffer { get; protected set; }
    public ComputeBuffer magnitudesBuffer { get; protected set; }


    int numOfPoints;

    [SerializeField]
    ComputeShader computeShader;

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
    // Include the properties of the shader that we need to be able to update here. 

    [SerializeField]
    public Material pointerMaterial; // { get; protected set; }
    [SerializeField]
    Mesh mesh;

    // It is the user's responsibility to make sure that these selections align with those in FieldLibrary.hlsl
    public enum FieldType { Outwards, Swirl }
    [SerializeField]
    public FieldType fieldType;

    [SerializeField]
    bool canMove, isDynamic;





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
        Graphics.DrawMeshInstancedProcedural(mesh, 0, pointerMaterial, bounds, numOfPoints);
    }
}
