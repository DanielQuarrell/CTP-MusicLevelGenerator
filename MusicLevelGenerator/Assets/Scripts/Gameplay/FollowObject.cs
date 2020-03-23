using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowObject : MonoBehaviour
{
    public bool active = true;
    public Transform followTransform;
    public Vector3 offset;

    private void Awake()
    {
        if(active)
        {
            offset = this.transform.position - followTransform.position;
        }
    }

    public void SetTransformWithOffset(Transform transform)
    {
        followTransform = transform;
        offset = this.transform.position - followTransform.position;
    }

    public void SetTransformWithoutOffset(Transform transform)
    {
        followTransform = transform;
    }

    private void LateUpdate()
    {
        if(active)
        {
            this.transform.position = new Vector3(followTransform.position.x + offset.x, this.transform.position.y, followTransform.position.z + offset.z);
        }
    }
}
