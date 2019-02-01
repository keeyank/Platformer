using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: Bug with jumping - If jumpCounteract is ever less than 0.15, then since moveBody(down) is calculated first,
// it will occur first and at this point it will reset gravityCounteract to 0 since the boolean there will return false
// TODO: Look at (3)

public class PhysicsObject : MonoBehaviour
{
    protected const float gravity = 0.15f;
    protected float gravityCounteract;
    private const float decay = 0.01f;

    protected float jumpCounteract = 0.225f; // TODO: should be different for each instance of a physics body so use a constructur to fix this
                                
    protected bool grounded;
    private bool groundedLastFrame; // NOTE: Can ONLY be used here, will give an INCORRECT RESULT if used in any children of PhysicsObject
                                    // (See FixedUpdate function to see why)
    protected Rigidbody2D rb2d;
    protected ContactFilter2D contactFilter;
    protected RaycastHit2D[] hitResults = new RaycastHit2D[16];


    void OnEnable() {
        rb2d = GetComponent<Rigidbody2D>();
    }

    void Start() {
        // Set up contact filter to include only collidable layer objects
        //contactFilter.useTriggers = false; // Don't include triggers in results
        contactFilter.SetLayerMask(1 << 8); // Include from collidable layer only
        contactFilter.useLayerMask = true;
    }

    protected virtual void FixedUpdate() {
        SimulateGravity();
    }

    // Pull object downards. Creates illusion of downwards acceleration
    private void SimulateGravity() {
        grounded = false; // reset grounded (will be computed again if downwards movement)

        // If gravityCounteract is greater than gravityCounteract, move downwards
        // Otherwise move upwards
        Move(Mathf.Sign(-gravity + gravityCounteract) * Vector2.up, Mathf.Abs(-gravity + gravityCounteract));

        // If physics body went from grounded state to not grounded,
        // Add a decaying counteraction towards gravity
        if (groundedLastFrame == true && grounded == false) {
            gravityCounteract += gravity; // TODO: (3)
        }

        // Decay gravityCounteract
        gravityCounteract -= decay;
        if (gravityCounteract < 0) { gravityCounteract = 0; }

        groundedLastFrame = grounded;
    }


    // Move object speed units towards direction
    // If there is a collidable in the way, object will not intersect it
    // Grounded is updated each time gravity is calculated 
    // ASSERTION: Called by FixedUpdate
    protected void Move(Vector2 direction, float speed) {
        Vector2 newPos = rb2d.position + (direction * speed);

        // Determine if there will be a collision
        int count = rb2d.Cast(direction, contactFilter, hitResults, speed);

        // Ensure there is a compatible collision before collision checking
        bool compatibleCollisionFound = false;
        for (int i = 0; i < count; i++) { 
            if (hitResults[i].normal == -direction) {
                compatibleCollisionFound = true;
            }
        }

        // Determine how to update newPos
        if (compatibleCollisionFound) {
            Vector2 extents = GetComponent<BoxCollider2D>().bounds.extents;
            float buffer = (float)0.01;
            if (direction == Vector2.down) {

                // Loop through hits to find the hit with the max or min point wrt direction
                // Ignore any hits with an incompatible normal
                float maxPointY = float.NegativeInfinity;
                for (int i = 0; i < count; i++) {
                    if (hitResults[i].normal == -direction) { // Only consider hits that are towards direction
                        if (hitResults[i].point.y > maxPointY) {
                            maxPointY = hitResults[i].point.y;
                        }
                    }
                }
                // Update newPos so that it isn't intersecting the other box collider
                // Add a .001 buffer - Makes user slightly float above tiles, but prevents unwanted collisions
                newPos = new Vector2(newPos.x, maxPointY + extents.y + buffer);
                grounded = true; // User is now touching ground
                gravityCounteract = 0; // (2)

            }

            else if (direction == Vector2.up) {
                float minPointY = float.PositiveInfinity;
                for (int i = 0; i < count; i++) {
                    if (hitResults[i].normal == -direction) {
                        if (hitResults[i].point.y < minPointY) {
                            minPointY = hitResults[i].point.y;
                        }
                    }
                }

                newPos = new Vector2(newPos.x, minPointY - extents.y - buffer);
                gravityCounteract = gravity; // (2)
            }

            else if (direction == Vector2.left) {
                float maxPointX = float.NegativeInfinity;
                for (int i = 0; i < count; i++) {
                    if (hitResults[i].normal == -direction) { 
                        if (hitResults[i].point.x > maxPointX) {
                            maxPointX = hitResults[i].point.x;
                        }
                    }
                }
                newPos = new Vector2(maxPointX + extents.x + buffer, newPos.y);
            }

            else if (direction == Vector2.right) {
                float minPointX = float.PositiveInfinity;
                for (int i = 0; i < count; i++) {
                    if (hitResults[i].normal == -direction) {
                        if (hitResults[i].point.x < minPointX) {
                            minPointX = hitResults[i].point.x;
                        }
                    }
                }
                newPos = new Vector2(minPointX - extents.x - buffer, newPos.y);
            }
        }

        // Update rigidbody position
        rb2d.position = newPos;
    }
}


/* (1) 
* grounded is set to false here, which may seem like there is a point in time where grounded is false when it should be true (which will be computed later)
* But it works because we override fixed update in the player controller, and the grounded bool will be already calculated correctly by then since gravity
* is the very first thing that uses the moveBody function. This must be always the case - MoveBody must be used only in FixedUpdate and AFTER gravity is computed
*/

/* (2)
 * Here we set the counteract to 0. This is because if we don't set it to 0 and counteract is above 0, it will make the object go up after the initial downwards
 * collision has occured - I.e., after the object is places on the platform due to gravity, it will be brought back up by exactly counteract points. 
 * This is very easily fixed by simply setting the counteract to 0 every time the object is registered as grounded
 * We also set gravityCounteract to 15 whenever the an object collides with something above it, so the object can smoothly fall back down
 */ 

/* (3)
 * There is a small bug here that is pretty much not noticable to the player, but probly should be fixed to make the code better and more readable.
 * The problem is that gravityCounteract is being added here after the jump has already been calculated if the player pressed space last frame.
 * So the added value to gravityCounteract is not going to be computed in the player's actual movement until the next frame.
 * Possible fix: Create a seperate IsGrounded function to compute whether the user is grounded, if he is not grounded but was grounded last frame, add gravity 
 * to gravityCounteract, and then once it's computed, call Move. Also, you'd have to remove grounded calculation in the Move function, so don't forget that.
 */