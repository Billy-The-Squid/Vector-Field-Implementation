using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldDetector : MonoBehaviour
{
    /// <summary>
    /// True if the transform is within the collider of a <cref>FieldZone</cref>.
    /// </summary>
    protected bool inField;
    /// <summary>
    /// A reference to the <cref>VectorField</cref> that the detector is inside of.
    /// </summary>
    public VectorField detectedField { get; protected set; }
    // Multiple fields are not supported. 

    /// <summary>
    /// Called by a <cref>FieldZone</cref> when entering a field.
    /// </summary>
    /// <param name="graph"></param>
    public virtual void EnteredField(VectorField graph)
    {
        inField = true;
        detectedField = graph;
    }

    /// <summary>
    /// Called by a <cref>FieldZone</cref> when exiting a field. 
    /// </summary>
    /// <param name="graph"></param>
    public virtual void ExitedField(VectorField graph)
    {
        inField = false;
        if(detectedField == graph) {
            detectedField = null;
        } // Better programming practice
    }
}
