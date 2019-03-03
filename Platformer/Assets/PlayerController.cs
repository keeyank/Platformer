using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class PlayerController : PhysicsObject
{

    [SerializeField] private float maxSpeed = 13f;
    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float acceleration = 1f;
    [SerializeField] private float wallMinSpeed = 1.4f;
    [SerializeField] private float gravityCounteractReduce = 5f;
    private float currentSpeed;

    protected override void Start() {
        base.Start();
        currentSpeed = minSpeed;
    }
    protected override void Update()
    {
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


            // Equalize movement with respect to wallJump Speed
            if (wallJumpSpeed > 0 && currentSpeed > Mathf.Abs(wallJumpSpeed)) {
                Move(Vector3.right, (currentSpeed - Mathf.Abs(wallJumpSpeed)) * Time.deltaTime);
            }
            else if (wallJumpSpeed > 0 && currentSpeed <= Mathf.Abs(wallJumpSpeed)) {
                // Do nothing
            }
            else { // wallJumpSpeed <= 0
                Move(Vector3.right, currentSpeed * Time.deltaTime);
            }
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
            if (wallJumpSpeed < 0 && currentSpeed > Mathf.Abs(wallJumpSpeed)) {
                Move(Vector3.left, (currentSpeed - Mathf.Abs(wallJumpSpeed)) * Time.deltaTime);
            }
            else if (wallJumpSpeed < 0 && currentSpeed <= Mathf.Abs(wallJumpSpeed)){
                // Do nothing
            }
            else { // wallJumpSpeed >= 0
                Move(Vector3.left, currentSpeed * Time.deltaTime);
            }
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