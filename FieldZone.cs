using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public abstract class FieldZone : MonoBehaviour
{
    /// <summary>
    /// The worldspace positions of each vector.
    /// </summary>
    public ComputeBuffer positionBuffer { get; protected set; }
    /// <summary>
    ///  The maximum displayed length of any vector in the field.
    /// </summary>
    [System.NonSerialized]
    public float maxVectorLength = 1;
    /// <summary>
    /// The worldspace point that the field calculations will treat as (0,0,0).
    /// </summary>
    public Vector3 fieldOrigin { get; protected set; }
    /// <summary>
    /// The bounds (worldspace) to be used when drawing the field.
    /// </summary>
    public Bounds bounds { get; protected set; }
    /// <summary>
    /// The number of points in the position buffer.
    /// </summary>
    protected int numberOfPoints;

    /// <summary>
    /// A triggering collider, to be used in conjuction with field detectors. 
    /// </summary>
    [SerializeField]
    public Collider triggerCollider;

    /// <summary>
    /// Initializes the variables that will not change throughout a game session,
    /// but which must be created before <cref>SetPositions</cref> may be called. 
    /// </summary>
    public abstract void Initialize();

    /// <summary>
    /// Fills <cref>positionBuffer</cref> with values. 
    /// </summary>
    public abstract void SetPositions();


    


    private void Start() {
        if(triggerCollider == null) {
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
