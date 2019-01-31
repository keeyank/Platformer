using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : PhysicsObject
{
    private float speed = 0.1f;

    // Must implement controllable movement here - AFTER grounded is computed
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Must over
        if (Input.GetKey(KeyCode.D)) {
            Move(Vector3.right, speed);
        }
        if (Input.GetKey(KeyCode.A)) {
            Move(Vector3.left, speed);
        }
        //if (Input.GetKey(KeyCode.W)) {
        //    Move(Vector3.up, speed);
        //}
        if (Input.GetKey(KeyCode.S)) {
            Move(Vector3.down, speed);
        }

        // DEBUG
        if (Input.GetKey(KeyCode.LeftShift)) {
            rb2d.position = new Vector2(1, 1);
        }
    }
}
