using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class FieldZone : MonoBehaviour
{
    // The positions of each vector (worldspace)
    public ComputeBuffer positionBuffer { get; protected set; }
    public float maxVectorLength = 1;
    // The point for the field calculations to treat as (0,0,0) (worldspace)
    public Vector3 fieldOrigin { get; protected set; } // Relative to the implementing object
    public Bounds bounds { get; protected set; }
    protected int numberOfPoints;

    [SerializeField]
    public Collider triggerCollider;

    public abstract void Initialize();

    // Fills up the positionBuffer with values
    public abstract void SetPositions();


    


    private void Start()
    {
        if(triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }
        //triggerCollider.isTrigger = true;
    }



    private void OnTriggerEnter(Collider other) {
        FieldDetector detect = other.GetComponent<FieldDetector>();
        if (detect != null)
        {
            detect.EnteredField(this.GetComponent<VectorField>());
        }
    }
    private void OnTriggerExit(Collider other) {
        try {
            //Debug.Log("Detected collider");
            other.GetComponent<FieldDetector>().ExitedField(this.GetComponent<VectorField>());
        }
        catch (System.NullReferenceException) {
            ;
        }
    }
}
