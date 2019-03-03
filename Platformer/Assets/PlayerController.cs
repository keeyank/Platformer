using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class PlayerController : PhysicsObject
{

    protected override void Start() {
        base.Start();
        currentSpeed = minSpeed;
    }
    protected override void Update() {
        base.Update();

        // Reduce gravityCounteract for next frame once player lets go of jump button
        if (Input.GetKeyUp(KeyCode.Space) && isMovingUp) {
            gravityCounteract -= gravityCounteractReduce;
            if (gravityCounteract < gravity) {
                gravityCounteract = gravity;
            }
        }

        // Movement with acceleration
        if (Input.GetKey(KeyCode.D)) {
            // If user is hugging a wall, set speed low to give more time for wall jump
            if (huggingLeftWall && !grounded[0]) {
                currentSpeed = wallMinSpeed;
            }

            // Accelerate
            currentSpeed += acceleration;
            if (currentSpeed > maxSpeed) { currentSpeed = maxSpeed; }


            // Equalize movement wrt wall jump speed
            // Cancel movement if early in walljump, if late in wall jump end wall jump speed
            float speedThisFrame = currentSpeed * Time.deltaTime;
            if (wallJumpSpeed > 0 && currentSpeed > Mathf.Abs(wallJumpSpeed)) {
                speedThisFrame = (currentSpeed - Mathf.Abs(wallJumpSpeed)) * Time.deltaTime;
            }
            else if (wallJumpSpeed > 0 && currentSpeed <= Mathf.Abs(wallJumpSpeed)) {
                // Do nothing
            }
            else if (wallJumpSpeed < 0 && Mathf.Abs(wallJumpSpeed) > wallJumpLockedInSpeed) {
                speedThisFrame = 0;
            }

            Move(Vector3.right, speedThisFrame);
        }

        if (Input.GetKey(KeyCode.A)) {
            // Lower speed when hugging wall for more accurate wall jumps
            if (huggingRightWall && !grounded[0]) {
                currentSpeed = wallMinSpeed;
            }

            // Accelerate
            currentSpeed += acceleration;
            if (currentSpeed > maxSpeed) { currentSpeed = maxSpeed; }

            // Equalize movement wrt wall jump speed
            // Cancel movement if early in walljump
            float speedThisFrame = currentSpeed * Time.deltaTime;
            if (wallJumpSpeed < 0 && currentSpeed > Mathf.Abs(wallJumpSpeed)) {
                speedThisFrame = (currentSpeed - Mathf.Abs(wallJumpSpeed)) * Time.deltaTime;
            }
            else if (wallJumpSpeed < 0 && currentSpeed <= Mathf.Abs(wallJumpSpeed)) {
                // Do nothing
            }
            else if (wallJumpSpeed > 0 && Mathf.Abs(wallJumpSpeed) > wallJumpLockedInSpeed) { // Early in walljump
                speedThisFrame = 0;
            }

            Move(Vector3.left, speedThisFrame);
        }

        // Reset acceleration when player lets go of keys
        if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.D)) {
            currentSpeed = minSpeed;

            if (Input.GetKeyUp(KeyCode.A) && wallJumpSpeed > 0 ||
                Input.GetKeyUp(KeyCode.D) && wallJumpSpeed < 0) {
                // User let go of key to go opposite direction of wall jump late in jump
                if (Mathf.Abs(wallJumpSpeed) <= wallJumpLockedInSpeed) {
                    wallJumpSpeed = 0;
                }
            }
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