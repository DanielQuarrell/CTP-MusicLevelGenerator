using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform playerTransform;

    public Vector3 offset;

    [SerializeField]
    bool setOffsetOnStart = false;

    private void Start()
    {
        if(setOffsetOnStart)
        {
            offset = this.transform.position - playerTransform.position;
        }
    }

    private void LateUpdate()
    {
        this.transform.position = new Vector3(playerTransform.position.x + offset.x, this.transform.position.y, playerTransform.position.z + offset.z);
    }
}
