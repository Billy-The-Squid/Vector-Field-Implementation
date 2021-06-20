using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldDetector : MonoBehaviour
{
    protected bool inField;
    public VectorField detectedField { get; protected set; }

    public virtual void EnteredField(VectorField graph)
    {
        inField = true;
        detectedField = graph;
    }

    public virtual void ExitedField(VectorField graph)
    {
        inField = false;
        if(detectedField == graph)
        {
            detectedField = null;
        } // Better programming practice
    }
}
