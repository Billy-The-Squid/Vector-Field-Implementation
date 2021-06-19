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

    [SerializeField]
    public Collider triggerCollider;

    // Fills up the positionBuffer with values
    public abstract void SetPositions();

    private void Start()
    {
        if(triggerCollider == null)
        {
            triggerCollider = GetComponent<Collider>();
        }
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        try {
            //Debug.Log("Detected collider");
            other.GetComponent<FieldDetector>().EnteredField(this.GetComponent<VectorField>());
        }
        catch (System.NullReferenceException) {
            ;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        try {
            //Debug.Log("Detected collider");
            other.GetComponent<FieldDetector>().ExitedField(this.GetComponent<VectorField>());
        }
        catch (System.NullReferenceException) {
            ;
        }
    }
}
