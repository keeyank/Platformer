using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : PhysicsObject
{
    private const float speed = 6f;

    protected override void Update()
    {
        UpdateGroundedHistory();
        if (withinJumpBuffer && Input.GetKeyDown(KeyCode.Space)) {
            gravityCounteract = jumpCounteract;
            if (!grounded[0] && !grounded[1]) {
                gravityCounteract += gravity; // (1)
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

/* (1)
 * We have to do this if the user is falling and making use of the buffer to jump.
 * If we don't, then gravityCounteract won't be added since grounded[1] isn't true.
 */ 