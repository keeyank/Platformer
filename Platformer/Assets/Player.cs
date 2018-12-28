using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Body
{
    // speed = 2
    Player() : base(2) { }

    protected override void Update()
    {
        base.Update();

        // Move player each frame if keys are held down
        if (Input.GetKey(KeyCode.D)) {
            MoveBody(Vector2.right, speed);
        }

        if (Input.GetKey(KeyCode.A)) {
            MoveBody(Vector2.left, speed);
        }
    }
}
