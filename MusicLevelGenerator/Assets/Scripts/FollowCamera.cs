using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform playerTransform;

    // Update is called once per frame
    void LateUpdate()
    {
        this.transform.position = new Vector3(playerTransform.position.x, this.transform.position.y, this.transform.position.z);
    }
}
