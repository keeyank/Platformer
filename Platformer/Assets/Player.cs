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
        if (Input.GetKey(KeyCode.D))
        {
            MoveBody(Vector3.right, speed);
        }

        if (Input.GetKey(KeyCode.A))
        {
            MoveBody(Vector3.left, speed);
        }
        
        // Check if collider for this object is intersecting with collider for another object
        // If it is, use the saved position of the gameObject at the beginning of the frame (i.e., in the update function for Body
        // and revert to this position (save the position each update at the beginning in the Body script, then revert to this position)

        // Checking should be at the very end of the update (before the position is updated in the next frame, so the sprites never intersect)

    }

    //If your GameObject starts to collide with another GameObject with a Collider
    void OnCollisionEnter(Collision collision)
    {
        //Output the Collider's GameObject's name
        Debug.Log(collision.collider.name);
    }

}
