using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VectorDisplay : Display
{
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

    static readonly int
        numPointsID = Shader.PropertyToID("_NumberOfPoints"),
        positionsBufferID = Shader.PropertyToID("_Positions"),
        vectorsBufferID = Shader.PropertyToID("_Vectors"),
        plotVectorsBufferID = Shader.PropertyToID("_PlotVectors"),
        vector2BufferID = Shader.PropertyToID("_Vectors2"),
        vector3BufferID = Shader.PropertyToID("_Vectors3"),
        magnitudesBufferID = Shader.PropertyToID("_Magnitudes"),
        maxVectorLengthID = Shader.PropertyToID("_MaxVectorLength");

    [SerializeField]
    ComputeShader displayComputer;

    private bool initialized = false;





    private void Initialize()
    {
        if(initialized) { return; }
        unsafe // <-- This could maybe be a source of problems.
        {
            plotVectorsBuffer = new ComputeBuffer(numOfPoints, sizeof(Vector3));
            vector2Buffer = new ComputeBuffer(numOfPoints, sizeof(Vector3));
            vector3Buffer = new ComputeBuffer(numOfPoints, sizeof(Vector3));
            magnitudesBuffer = new ComputeBuffer(numOfPoints, sizeof(float));
        }

        initialized = true;
    }

    private void OnDestroy()
    {
        if(plotVectorsBuffer != null) {
            plotVectorsBuffer.Release();
            plotVectorsBuffer = null;
        }
        if(vector2Buffer != null) {
            vector2Buffer.Release();
            vector2Buffer = null;
        }
        if(vector3Buffer != null) {
            vector3Buffer.Release();
            vector3Buffer = null;
        }
        if(magnitudesBuffer != null) {
            magnitudesBuffer.Release();
            magnitudesBuffer = null;
        }

        initialized = false;
    }

    public override void DisplayVectors(ComputeBuffer positionsBuffer, ComputeBuffer vectorsBuffer)
    {
        numOfPoints = positionsBuffer.count;

        Initialize();

        CalculateDisplay(positionsBuffer, vectorsBuffer);

        PlotResults(positionsBuffer);
    }

    /// <summary>
    /// Calculates the necessary values to display a vector. 
    /// </summary>
    /// <param name="positionsBuffer"></param>
    /// <param name="vectorsBuffer"></param>
    private void CalculateDisplay(ComputeBuffer positionsBuffer, ComputeBuffer vectorsBuffer)
    {
        int kernelID = 0;

        displayComputer.SetInt(numPointsID, numOfPoints);

        displayComputer.SetBuffer(kernelID, positionsBufferID, positionsBuffer);
        displayComputer.SetBuffer(kernelID, vectorsBufferID, vectorsBuffer);
        displayComputer.SetBuffer(kernelID, plotVectorsBufferID, plotVectorsBuffer);
        displayComputer.SetBuffer(kernelID, vector2BufferID, vector2Buffer);
        displayComputer.SetBuffer(kernelID, vector3BufferID, vector3Buffer);
        displayComputer.SetBuffer(kernelID, magnitudesBufferID, magnitudesBuffer);
        displayComputer.SetFloat(maxVectorLengthID, maxVectorLength);

        //Debug.Log("Number of points: " + numOfPoints);
        int numGroups = Mathf.CeilToInt(numOfPoints / 64f);
        displayComputer.Dispatch(kernelID, numGroups, 1, 1);
    }

    /// <summary>
    /// Interfaces with the <cref>pointerMaterial</cref> to display the vector field. 
    /// </summary>
    private void PlotResults(ComputeBuffer positionsBuffer)
    {
        // Then the data from the computeShader is sent to the shader to be rendered.
        pointerMaterial.SetBuffer(positionsBufferID, positionsBuffer);
        pointerMaterial.SetBuffer(plotVectorsBufferID, plotVectorsBuffer);
        pointerMaterial.SetBuffer(vector2BufferID, vector2Buffer);
        pointerMaterial.SetBuffer(vector3BufferID, vector3Buffer);
        pointerMaterial.SetBuffer(magnitudesBufferID, magnitudesBuffer);

        // Setting the bounds and giving a draw call
        Graphics.DrawMeshInstancedProcedural(pointerMesh, 0, pointerMaterial, bounds, numOfPoints);

        //// Debugging code
        //Vector3[] debugArray = new Vector3[numOfPoints];
        ////float[] debugArray = new float[numOfPoints];
        //plotVectorsBuffer.GetData(debugArray);
        //Debug.Log((("First three points in plot array: " + debugArray[0]) + debugArray[1]) + debugArray[2]);
        //Debug.Log((("Last three points in plot array: " + debugArray[numOfPoints - 1]) + debugArray[numOfPoints - 2]) + debugArray[numOfPoints - 3]);
    }
}
