using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsObject : MonoBehaviour
{
    private float gravityCurr = 0.01f;

    private const float gravityMax = 0.15f;
    private const float gravityMin = 0.01f;
    private const float gravityMod = 0.005f;

    protected bool grounded;
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
        // Simulate gravity
        Move(Vector2.down, gravityCurr);
        if (gravityCurr < gravityMax) {
            gravityCurr += gravityMod;
        }
    }

    // Move object speed units towards direction
    // If there is a collidable in the way, object will not intersect it
    // Grounded is updated each time gravity is calculated 
    // ASSERTION: Called by FixedUpdate
    protected void Move(Vector2 direction, float speed) {
        Vector2 newPos = rb2d.position + (direction * speed);

        // Default grounded to false, set to true if a downwards collision is found
        if (direction == Vector2.down) { grounded = false; } // NOTE: Check notes at button (1)

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
                gravityCurr = gravityMin;
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
grounded is set to false here, which may seem like there is a point in time where grounded is false when it should be true (which will be computed later)
But it works because we override fixed update in the player controller, and the grounded bool will be already calculated correctly by then since gravity
is the very first thing that uses the moveBody function. This must be always the case - MoveBody must be used only in FixedUpdate and AFTER gravity is computed
*/
