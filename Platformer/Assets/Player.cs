using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : PhysicsObject
{
    // Must implement controllable movement here - AFTER grounded is computed
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Must over
        if (Input.GetKey(KeyCode.D)) {
            Move(Vector3.right, (float)0.01);
        }
        if (Input.GetKey(KeyCode.A)) {
            Move(Vector3.left, (float)0.01);
        }
        if (Input.GetKey(KeyCode.W)) {
            Move(Vector3.up, (float)0.02);
        }
        if (Input.GetKey(KeyCode.S)) {
            Move(Vector3.down, (float)0.01);
        }
        if (Input.GetKey(KeyCode.Space)) {
            Debug.Log(grounded);
        }
    }
}
