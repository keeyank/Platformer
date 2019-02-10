using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class PlayerController : PhysicsObject
{
    private float currentSpeed = minSpeed;
    private const float maxSpeed = 6f;
    private const float minSpeed = 2f;
    private const float acceleration = 0.5f;
    private const float wallMinSpeed = 1.4f;

    protected override void Update()
    {
        base.Update();

        // Reduce gravityCounteract for next frame once player lets go of jump button
        if (Input.GetKeyUp(KeyCode.Space) && isMovingUp) {
            gravityCounteract -= 5f;
            if (gravityCounteract < gravity) {
                gravityCounteract = gravity;
            }
        }

        // Movement with acceleration
        if (Input.GetKey(KeyCode.D)) {
            // If user is hugging a wall, reduce acceleration to give more time for wall jump
            if (huggingLeftWall && !grounded[0]) {
                currentSpeed = wallMinSpeed;
            }

            currentSpeed += acceleration;
            if (currentSpeed > maxSpeed) { currentSpeed = maxSpeed; }
            Move(Vector3.right, currentSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.A)) {

            if (huggingRightWall && !grounded[0]) {
                currentSpeed = wallMinSpeed;
            }

            currentSpeed += acceleration;
            if (currentSpeed > maxSpeed) { currentSpeed = maxSpeed; }
            Move(Vector3.left, currentSpeed * Time.deltaTime);
        }

        // Reset acceleration when player lets go of keys
        if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D)) {
            currentSpeed = minSpeed;
        }

        // DEBUG
        if (Input.GetKey(KeyCode.LeftShift)) {
            rb2d.position = new Vector2(1, 1);
        }
    }

    protected override void ProcessJumpRequests() {
        // Player requests a jump when jump button pressed
        // jumpRequest is satisfied as soon as player lands within timer
        // wall jump requests satisfied immediately in SimulatePhysics function
        if (Input.GetKeyDown(KeyCode.Space)) {
            requestJump();
        }
    }
}