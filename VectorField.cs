using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(FieldZone))]
public class VectorField : MonoBehaviour
{
    [SerializeField]
    public FieldZone zone { get; protected set; }

    ComputeBuffer positionsBuffer,
        vectorsBuffer,  // Should we instead use registers to globally bind these extra buffers?
        plotVectorsBuffer,
        vector2Buffer,
        vector3Buffer,
        magnitudesBuffer;

    int volume;

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
        maxVectorLengthID = Shader.PropertyToID("_MaxVectorLength");
    // Include the properties of the shader that we need to be able to update here. 

    [SerializeField]
    Material material;
    [SerializeField]
    Mesh mesh;






    private void OnEnable()
    {
        if(zone == null) {
            zone = GetComponent<FieldZone>();
        }

        zone.SetPositions();

        positionsBuffer = zone.positionBuffer;
        volume = positionsBuffer.count;

        unsafe // This could maybe be a source of problems.
        {
            vectorsBuffer = new ComputeBuffer(volume, sizeof(Vector3)); // last arg: size of single object
            plotVectorsBuffer = new ComputeBuffer(volume, sizeof(Vector3));
            vector2Buffer = new ComputeBuffer(volume, sizeof(Vector3));
            vector3Buffer = new ComputeBuffer(volume, sizeof(Vector3));
            magnitudesBuffer = new ComputeBuffer(volume, sizeof(float));
        }
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
        UpdateGPU();
    }



    void UpdateGPU()
    {
        // The data is sent to the computeShader for calculation
        computeShader.SetVector(centerID, zone.fieldOrigin);
        computeShader.SetFloat(maxVectorLengthID, zone.maxVectorLength);

        computeShader.SetBuffer(0, positionsBufferID, positionsBuffer);
        computeShader.SetBuffer(0, vectorBufferID, vectorsBuffer);
        computeShader.SetBuffer(0, plotVectorsBufferID, plotVectorsBuffer);
        computeShader.SetBuffer(0, vector2BufferID, vector2Buffer);
        computeShader.SetBuffer(0, vector3BufferID, vector3Buffer);
        computeShader.SetBuffer(0, magnitudesBufferID, magnitudesBuffer);
        // Why does this need to be redone every frame?

        // This does the math and stores information in the positionsBuffer. %%%%%%%%%
        int groups = Mathf.CeilToInt(volume / 64f);
        computeShader.Dispatch(0, volume, 1, 1);

        // Then the data from the computeShader is sent to the shader to be rendered. %%%%%%%%
        material.SetBuffer(positionsBufferID, positionsBuffer);
        material.SetBuffer(plotVectorsBufferID, plotVectorsBuffer);
        material.SetBuffer(vector2BufferID, vector2Buffer);
        material.SetBuffer(vector3BufferID, vector3Buffer);
        material.SetBuffer(magnitudesBufferID, magnitudesBuffer);

        // Here should be information about bounds and a call to draw...
        var bounds = zone.bounds;
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, volume);
    }
}
