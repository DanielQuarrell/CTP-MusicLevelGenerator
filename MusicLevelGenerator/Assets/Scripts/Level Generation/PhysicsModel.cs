using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsModel : MonoBehaviour
{
    public float gravity;
    public float velocity;
    public float jumpAcceleration;

    public float jumpHeight;
    public float jumpDistance;

    public void CalculatePhysicsModel()
    {
        //Since velocity is always 0 at jump height, use -velocity
        //(vf - vi) / -g
        float timeToReachHighestPoint = -velocity / -gravity;

        //Time of whole jump
        float timeInAir = timeToReachHighestPoint * 2;

        //jumpHeight = (vi * t) - ½(g * t²)
        jumpHeight = (velocity * timeToReachHighestPoint) - 0.5f * (gravity * Mathf.Pow(timeToReachHighestPoint, 2));

        jumpDistance = timeInAir * velocity;
    }
}
