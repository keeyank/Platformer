using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: Move the code that sets gravityCounteract = 0 up to where grounded is being computed. I think that maeks more sense, but make sure it totally works
// both logically and via testing before you commit to it, since this won't fix any current bugs and the games working fine at this state.
//TODO: It's kinda weird how (*) works, maybe make it so that can only happen if the player isn't currently jumping? (player starts jump when request fulfilled,
// ends jump when the player becomes grounded)
public class PhysicsObject : MonoBehaviour
{
    protected const float gravity = 10f;
    protected float gravityCounteract;
    private const float decay = 0.75f;

    private float buffer = 0.01f; // Used to fix weird bug with collision detection
                                  // Player ends up slightly floating above platforms (corrected via platform hitboxes)

    protected float jumpCounteract = 25f; // TODO: should be different for each instance of a physics body so use a constructur to fix this
    protected float jumpBuffer = 0.2f; // (3)
    protected bool withinJumpBuffer;
    protected bool requestedJump;


    // grounded[0] represents grounded state at current frame, grounded[1] represents grounded state 1 frame ago, 
    protected bool[] grounded = new bool[2];
    protected bool isJumping;


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

    protected virtual void Update() {
        UpdateGroundedHistory();
        SimulateGravity();

    }

    // Update grounded history, including current frame's grounded
    // Should be done at the beginning of every frame
    protected void UpdateGroundedHistory() {
        for (int i = grounded.Length-1; i > 0; i--) {
            grounded[i] = grounded[i - 1];
        }

        // Determine if user collides with ground
        grounded[0] = false; 
        int count = rb2d.Cast(Vector2.down, contactFilter, hitResults, buffer);
        if (CompatibleCollisionFound(hitResults, count, Vector2.down)) {
            grounded[0] = true;
            isJumping = false; // Grounded -> user is not jumping
        }

        // Determine if user can jump this frame
        withinJumpBuffer = false;
        count = rb2d.Cast(Vector2.down, contactFilter, hitResults, buffer + jumpBuffer);
        if (CompatibleCollisionFound(hitResults, count, Vector2.down)) {
            withinJumpBuffer = true;
        }
    }

    // Pull object downards. Creates illusion of downwards acceleration
    protected void SimulateGravity() {
        // Increase gravityCounteract if user recently went to not grounded state
        if (grounded[1] && !grounded[0] && !isJumping) { 
            gravityCounteract += gravity; 
        }

        // If user is grounded and has previously requested a jump within their jumpBuffer, increase gravity counteract
        if (grounded[0] && requestedJump) {
            gravityCounteract = jumpCounteract;
            isJumping = true;
            requestedJump = false;
        }

        // Decay gravityCounteract each frame
        gravityCounteract -= decay;
        if (gravityCounteract < 0) { gravityCounteract = 0; }

        // If gravityCounteract is greater than gravityCounteract, move downwards
        // Otherwise move upwards
        Move(Mathf.Sign(-gravity + gravityCounteract) * Vector2.up, Mathf.Abs(-gravity + gravityCounteract) * Time.deltaTime);
    }


    // Move object speed units towards direction
    // If there is a collidable in the way, object will not intersect it
    // ASSERTION: direction is up, down, right, or left
    protected void Move(Vector2 direction, float speed) {
        Vector2 newPos = rb2d.position + (direction * speed);

        // Determine if there will be a collision
        int count = rb2d.Cast(direction, contactFilter, hitResults, speed);

        // Determine how to update newPos
        if (CompatibleCollisionFound(hitResults, count, direction)) {
            Vector2 extents = GetComponent<BoxCollider2D>().bounds.extents;
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
                // Add a buffer - Makes user slightly float above tiles, but prevents unwanted collisions - Hitboxes made smaller to counteract this
                newPos = new Vector2(newPos.x, maxPointY + extents.y + buffer);
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

    // Return true only if there is a collision found with a normal opposite to direction
    // This filters out any collisions that should be ignored
    // ASSERTION: Used after a Cast which returns a hitResults array
    private bool CompatibleCollisionFound(RaycastHit2D[] hits, int count, Vector2 direction) {
        for (int i = 0; i < count; i++) {
            if (hits[i].normal == -direction) {
                return true;
            }
        }
        return false;
    }
}

/* (1) 
 * grounded is set to false here, which may seem like there is a point in time where grounded is false when it should be true (which will be computed later)
 * But it works because we override fixed update in the player controller, and the grounded bool will be already calculated correctly by then since gravity
 * is the very first thing that uses the moveBody function. This must be always the case - MoveBody must be used only in FixedUpdate and AFTER gravity is computed
 */

/* (2)
 * Gravitycounteract should be reset whenever a player is grounded
 * We also set gravityCounteract to gravity whenever the an object collides with something above it, so the object can smoothly fall back down
 */

/* (3)
 * When there is a large jump buffer, some weird stuff can happen via the input saving feature. 
 * For example, a player can click jump and immediately click jump again. It's valid since the player is within the jump buffer since the jump just began
 * However, this means that another jump has been requested, and the request will pull through much later when he actually lands.
 * A possible fix might be to have it be automatically set to false after 3 frames if it was set to true the previous 3, in order to give the players exactly
 * 3 frames of a safety net for their jumping. 
 * However, these bugs only occur when the jump buffer is very large, and it's really difficult to do something like this when the jump buffer is small, so 
 * it should be OK to not fix for now, unless another bug comes from it. 
 * Also, if the player happens to click jump while in the jump buffer of a platform, then goes past it by going right and landing on another platform below it,
 * this will also cause the player to jump.
 */