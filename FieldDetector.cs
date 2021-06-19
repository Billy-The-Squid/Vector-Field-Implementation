using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldDetector : MonoBehaviour
{
    protected bool inField;
    protected VectorField field;

    public virtual void EnteredField(VectorField graph)
    {
        inField = true;
        field = graph;
    }

    public virtual void ExitedField(VectorField graph)
    {
        inField = false;
        if(field == graph)
        {
            field = null;
        } // Better programming practice
    }
}
