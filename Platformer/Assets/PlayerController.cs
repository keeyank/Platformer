using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : PhysicsObject
{
    private float currentSpeed = minSpeed;
    private const float maxSpeed = 6f;
    private const float minSpeed = 2f;
    private const float acceleration = 0.5f;

    protected override void Update()
    {
        UpdateGroundedHistory();

        // Player requests a jump
        if (canRequestJump && Input.GetKeyDown(KeyCode.Space)) {
            if (canRequestJump && !requestedJump) {
                requestedJump = true;
            }
        }

        SimulateGravity();

        if (Input.GetKey(KeyCode.D)) {
            currentSpeed += acceleration;
            if (currentSpeed > maxSpeed) { currentSpeed = maxSpeed; }
            Move(Vector3.right, currentSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.A)) {
            currentSpeed += acceleration;
            if (currentSpeed > maxSpeed) { currentSpeed = maxSpeed; }
            Move(Vector3.left, currentSpeed * Time.deltaTime);
        }

        if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D)) {
            currentSpeed = minSpeed;
        }

        // DEBUG
        if (Input.GetKey(KeyCode.LeftShift)) {
            rb2d.position = new Vector2(1, 1);
        }
    }
}