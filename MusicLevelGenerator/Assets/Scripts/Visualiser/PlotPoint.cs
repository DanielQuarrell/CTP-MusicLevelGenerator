using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlotPoint : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Transform connectingPlotTransform;
    public bool drawLine = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        connectingPlotTransform = this.transform;
    }

    void OnBecameVisible()
    {
        drawLine = true;
    }

    void OnBecameInvisible()
    {
        drawLine = false;
    }

    void OnDrawGizmos()
    {
        if(this.gameObject != null || this.connectingPlotTransform != null)
        {
            if (drawLine)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, connectingPlotTransform.position);
            }
        }
    }
}
