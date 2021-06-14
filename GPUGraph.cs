using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    [SerializeField, Min(1)]
    int sideLength = 1;
    [SerializeField]
    float spacing = 1;
    //int numCharges;

    Vector3 originPosition;

    //[SerializeField]
    //const float coulombConstant = 8.9875517923e9f; // J m / C^2

    ComputeBuffer positionsBuffer,
        vectorsBuffer,  // Should we instead use registers to globally bind these extra buffers?
        vector2Buffer,
        vector3Buffer;
        //chargesBuffer,
        //chargePositionsBuffer;

    [SerializeField]
    ComputeShader computeShader;

    static readonly int
        //coulombID = Shader.PropertyToID("_CoulombConstant"),
        spacingID = Shader.PropertyToID("_Spacing"),
        originID = Shader.PropertyToID("_OriginPosition"),
        sideLengthID = Shader.PropertyToID("_SideLength"),
        positionsBufferID = Shader.PropertyToID("_Positions"),
        vectorBufferID = Shader.PropertyToID("_Vectors"),
        vector2BufferID = Shader.PropertyToID("_Vectors2"),
        vector3BufferID = Shader.PropertyToID("_Vectors3"),
        //numChargesID = Shader.PropertyToID("_NumberOfCharges"),
        //chargesID = Shader.PropertyToID("_Charges"),
        //chargePositionsID = Shader.PropertyToID("_ChargePositions");
    // Include the properties of the shader that we need to be able to update here. 

    [SerializeField]
    Material material;
    // Should these two be different for a blender file?
    [SerializeField]
    Mesh mesh;
    [SerializeField]
    Transform prefab;

    //Charge[] chargeArray;  // The actual charge GameObjects,
    //float[] charges;           // their charges, 
    //Vector3[] chargePositions; // and their positions.






    private void OnEnable()
    {
        // sideLength = 2 * size + 1;
        originPosition = transform.position;

        unsafe // This could maybe be a source of problems.
        {
            positionsBuffer = new ComputeBuffer((int)Mathf.Pow(sideLength, 3), sizeof(Vector3));
            vectorsBuffer = new ComputeBuffer((int)Mathf.Pow(sideLength, 3), sizeof(Vector3)); // last arg: size of single object
            vector2Buffer = new ComputeBuffer((int)Mathf.Pow(sideLength, 3), sizeof(Vector3));
            vector3Buffer = new ComputeBuffer((int)Mathf.Pow(sideLength, 3), sizeof(Vector3));
            //chargesBuffer = new ComputeBuffer(20, sizeof(float));
            //// This is a hard limit, but it'd be nice if we could dynamically increase the buffer size. 
            //chargePositionsBuffer = new ComputeBuffer(20, sizeof(Vector3));
        }
    }
    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;

        vectorsBuffer.Release();
        vectorsBuffer = null;

        vector2Buffer.Release();
        vector2Buffer = null;

        vector3Buffer.Release();
        vector3Buffer = null;

        //chargesBuffer.Release();
        //chargesBuffer = null;

        //chargePositionsBuffer.Release();
        //chargePositionsBuffer = null;
    }



    // Update is called once per frame
    void Update()
    {
        //chargeArray = FindObjectsOfType<Charge>();
        //numCharges = chargeArray.Length;

        //charges = new float[chargeArray.Length];
        //chargePositions = new Vector3[chargeArray.Length];

        //for (int i = 0; i < chargeArray.Length; i++)
        //{
        //    Charge charge = chargeArray[i];
        //    charges[i] = charge.GetComponent<Charge>().charge;
        //    chargePositions[i] = charge.GetComponent<Transform>().localPosition;
        //}

        UpdateGPU();
    }



    void UpdateGPU()
    {
        // The data is sent to the computeShader for calculation %%%%%%%%%
        //computeShader.SetFloat(coulombID, coulombConstant);
        computeShader.SetInt(sideLengthID, sideLength);
        computeShader.SetFloat(spacingID, spacing);
        computeShader.SetVector(originID, originPosition);

        computeShader.SetBuffer(0, positionsBufferID, positionsBuffer);
        computeShader.SetBuffer(0, vectorBufferID, vectorsBuffer);
        computeShader.SetBuffer(0, vector2BufferID, vector2Buffer);
        computeShader.SetBuffer(0, vector3BufferID, vector3Buffer);
        //computeShader.SetInt(numChargesID, numCharges);
        //computeShader.SetBuffer(0, chargesID, chargesBuffer);
        //computeShader.SetBuffer(0, chargePositionsID, chargePositionsBuffer);
        // Why does this need to be redone every frame?

        // Sending actual values to a couple of these buffers. 
        //chargesBuffer.SetData(charges);
        //chargePositionsBuffer.SetData(chargePositions);

        // This does the math and stores information in the positionsBuffer. %%%%%%%%%
        int numGroups = Mathf.CeilToInt(sideLength / 4f); // Why this?
        computeShader.Dispatch(0, numGroups, numGroups, numGroups);

        // Then the data from the computeShader is sent to the shader to be rendered. %%%%%%%%
        material.SetBuffer(positionsBufferID, positionsBuffer);
        material.SetBuffer(vectorBufferID, vectorsBuffer);
        material.SetBuffer(vector2BufferID, vector2Buffer);
        material.SetBuffer(vector3BufferID, vector3Buffer);

        // Here should be information about bounds and a call to draw...
        var bounds = new Bounds(transform.position + 0.5f * Vector3.one * (sideLength + 1) * spacing, 
            transform.position + Vector3.one * (sideLength + 1) * spacing);
        // This boundary needs revision
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, (int)Mathf.Pow(sideLength, 3));
    }
}
