using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; 

public class Player : MonoBehaviour
{
    [SerializeField] float jumpForce = 5;

    Rigidbody2D body;

    bool grounded = true;
    bool ducking = false;

    bool jumpButtonPressed;
    bool duckButtonPressed;

    void Start()
    {
        body = this.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        jumpButtonPressed = Input.GetKey(KeyCode.Space);
        duckButtonPressed = Input.GetKey(KeyCode.S);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (jumpButtonPressed)
        {
            Jump();
        }

        if (duckButtonPressed && grounded)
        {
            Duck();
        }
        else
        {
            Unduck();
        }
    }

    void Jump()
    {
        if(grounded)
        {
            body.velocity = Vector2.up * jumpForce;
            grounded = false;
        }
    }

    void Duck()
    {
        if(!ducking)
        {
            this.transform.DOScaleY(0.5f, 0.05f);

            ducking = true;
        }
    }

    void Unduck()
    {
        if (ducking)
        {
            this.transform.DOScaleY(1f, 0.05f);

            ducking = false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        grounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        grounded = false;
    }
}
