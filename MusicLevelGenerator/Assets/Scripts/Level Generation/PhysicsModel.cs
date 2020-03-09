using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PhysicsModel
{
    public float gravity;
    public float velocity;
    public float jumpAcceleration;

    public float jumpHeight;
    public float jumpDistance;

    public void CalculatePhysicsModel()
    {
        //Since final velocity is always 0 at jump height, use -initial velocity
        //(vf - vi) / -g
        float timeToReachHighestPoint = -jumpAcceleration / -gravity;

        //Time of whole jump in seconds
        float timeInAir = timeToReachHighestPoint * 2;

        //jumpHeight = (vi * t) - ½(g * t²)
        jumpHeight = (jumpAcceleration * timeToReachHighestPoint) - 0.5f * (gravity * Mathf.Pow(timeToReachHighestPoint, 2));

        //distance = time * units per second
        jumpDistance = timeInAir * velocity;
    }
}
