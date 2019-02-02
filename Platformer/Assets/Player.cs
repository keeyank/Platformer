using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : PhysicsObject
{
    private const float speed = 6f;

    protected override void Update()
    {
        UpdateGroundedHistory();

        // Player requests a jump
        if (withinJumpBuffer && Input.GetKeyDown(KeyCode.Space)) {
            if (withinJumpBuffer && !requestedJump) {
                requestedJump = true;
            }
        }

        SimulateGravity();

        if (Input.GetKey(KeyCode.D)) {
            Move(Vector3.right, speed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.A)) {
            Move(Vector3.left, speed * Time.deltaTime);
        }

        // DEBUG
        if (Input.GetKey(KeyCode.LeftShift)) {
            rb2d.position = new Vector2(1, 1);
        }
    }
}