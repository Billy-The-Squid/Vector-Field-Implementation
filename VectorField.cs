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
[RequireComponent(typeof(FieldZone), typeof(Display))]
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
    /// Stores the extra float arguments used in the computation.
    /// Set your own indexing scheme. 
    /// </summary>
    public ComputeBuffer floatArgsBuffer { get; set; }
    /// <summary>
    /// Stores the extra vector arguments used in the computation.
    /// Set your own indexing scheme. 
    /// </summary>
    public ComputeBuffer vectorArgsBuffer { get; set; }
    

    /// <summary>
    /// The number of points at which vectors will be plotted and the number of values in each buffer.
    /// </summary>
    int numOfPoints;

    /// <summary>
    /// The compute shader used to generate the vector field. 
    /// </summary>
    [SerializeField]
    public ComputeShader computeShader;

    // Property IDs used to send values to various shaders.
    static readonly int
        centerID = Shader.PropertyToID("_CenterPosition"),
        positionsBufferID = Shader.PropertyToID("_Positions"),
        vectorBufferID = Shader.PropertyToID("_Vectors"),
        //plotVectorsBufferID = Shader.PropertyToID("_PlotVectors"),
        //vector2BufferID = Shader.PropertyToID("_Vectors2"),
        //vector3BufferID = Shader.PropertyToID("_Vectors3"),
        floatArgsID = Shader.PropertyToID("_FloatArgs"),
        vectorArgsID = Shader.PropertyToID("_VectorArgs");


    /// <summary>
    /// The possible types of field to display. 
    /// It is the user's responsibility to make sure that these selections align with those in FieldLibrary.hlsl
    /// </summary>
    public enum FieldType { Outwards, Swirl, Coulomb }
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

    [SerializeField]
    public Display display { get; protected set; }

    // DOCUMENT THESE %%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
    public delegate void Reminder();

    public Reminder preSetPositions;
    public Reminder preCalculations;
    public Reminder preDisplay;

    public float[] floatArgsArray { get; set; }
    public Vector3[] vectorArgsArray { get; set; }





    private void Awake()
    {
        if (zone == null)
        {
            zone = GetComponent<FieldZone>();
        }
        if (display == null)
        {
            display = GetComponent<Display>();
        }
    }

    private void OnEnable()
    {
        preSetPositions += Pass;
        preCalculations += Pass;
        preDisplay += Pass;


        preSetPositions();

        zone.SetPositions();

        positionsBuffer = zone.positionBuffer;
        numOfPoints = positionsBuffer.count;

        unsafe // <-- This could maybe be a source of problems.
        {
            vectorsBuffer = new ComputeBuffer(numOfPoints, sizeof(Vector3)); // last arg: size of single object
        }

        preCalculations();

        CalculateVectors();

        display.maxVectorLength = zone.maxVectorLength;
        display.bounds = zone.bounds;
    }



    private void OnDisable()
    {
        vectorsBuffer.Release();
        vectorsBuffer = null;

        if(floatArgsBuffer != null)
        {
            floatArgsBuffer.Release();
            floatArgsBuffer = null;
            floatArgsArray = null;
        }
        if(vectorArgsBuffer != null)
        {
            vectorArgsBuffer.Release();
            vectorArgsBuffer = null;
            vectorArgsArray = null;
        }
    }



    // Update is called once per frame
    void Update()
    {
        if(canMove) {
            preSetPositions();
            zone.SetPositions();
            isDynamic = true;
        }

        
        if(isDynamic)
        {
            preCalculations();

            
            // Debug code
            //Debug.Log("Extra args are null? " + (floatArgsBuffer == null));
            CalculateVectors();
        }

        // Debug code
        Vector3[] debugArray = new Vector3[numOfPoints];
        vectorsBuffer.GetData(debugArray);
        Debug.Log((("First three points in vector array: " + debugArray[0]) + debugArray[1]) + debugArray[2]);
        Debug.Log((("Last three points in vector array: " + debugArray[numOfPoints - 1]) + debugArray[numOfPoints - 2]) + debugArray[numOfPoints - 3]);
    }

    private void LateUpdate()
    {
        preDisplay();
        //PlotResults();
        display.DisplayVectors(positionsBuffer, vectorsBuffer);
    }


    /// <summary>
    /// Interfaces with the <cref>computeShader</cref> and calculates the value of the vectors at each point, storing them in the buffers. 
    /// </summary>
    private void CalculateVectors()
    {
        if(floatArgsBuffer == null && floatArgsArray.Length != 0)
        {
            //Debug.Log("Making new buffer...");
            floatArgsBuffer = new ComputeBuffer(floatArgsArray.Length, sizeof(float));
            //floatArgsArray[0] = -1.5f;
            floatArgsBuffer.SetData(floatArgsArray);
        }
        if(vectorArgsBuffer == null && vectorArgsArray.Length != 0)
        {
            unsafe
            {
                vectorArgsBuffer = new ComputeBuffer(vectorArgsArray.Length, sizeof(Vector3));
            }
            vectorArgsBuffer.SetData(vectorArgsArray);
        }

        // The data is sent to the computeShader for calculation
        computeShader.SetVector(centerID, zone.fieldOrigin);

        int kernelID = (int)fieldType;
        computeShader.SetBuffer(kernelID, positionsBufferID, positionsBuffer);
        computeShader.SetBuffer(kernelID, vectorBufferID, vectorsBuffer);
        if(floatArgsBuffer != null) {
            computeShader.SetBuffer(kernelID, floatArgsID, floatArgsBuffer);
            //Debug.Log("Sending float args");
            // Debug code
            //float[] debugArray = new float[floatArgsBuffer.count];
            //floatArgsBuffer.GetData(debugArray); 
        }
        if(vectorArgsBuffer != null) {
            computeShader.SetBuffer(kernelID, vectorArgsID, vectorArgsBuffer);
        }

        

        // This does the math and stores information in the positionsBuffer.
        int groups = Mathf.CeilToInt(numOfPoints / 64f);
        computeShader.Dispatch(kernelID, groups, 1, 1);
    }

    public void Pass()
    {
        ;
    }
}
