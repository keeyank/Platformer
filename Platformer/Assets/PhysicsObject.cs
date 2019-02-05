using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO: Move the code that sets gravityCounteract = 0 up to where grounded is being computed. I think that maeks more sense, but make sure it totally works
// both logically and via testing before you commit to it, since this won't fix any current bugs and the games working fine at this state.
// Also do the same thing with when gravityCounteract is set to gravity for upwards collisions
//TODO: (5)
public class PhysicsObject : MonoBehaviour
{
    protected const float gravity = 10f;
    protected float gravityCounteract;
    private const float decay = 0.75f;

    private float buffer = 0.01f; // Used to fix weird bug with collision detection
                                  // Player ends up slightly floating above platforms (corrected via platform hitboxes)

    protected float jumpCounteract = 25f; // TODO: should be different for each instance of a physics body so use a constructur to fix this
    protected float jumpBuffer = 0.2f; // (3)
    protected bool canRequestJump;
    protected bool requestedJump;


    // grounded[0] represents grounded state at current frame, grounded[1] represents grounded state 1 frame ago, 
    // Key assertion: If the object is in grounded[0] state, object is not in isJumping state
    protected bool[] grounded = new bool[10];
    protected int jumpFrameBuffer = 3; // Max amount of frames from when object falls off a ledge such canJump is true
                                       // Must be at most the same size as the grounded array
    protected bool isJumping;
    protected bool isMovingUp;
    protected bool isMovingDown;

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
        ProcessJumpRequests();
        SimulateGravity();

    }

    // Update grounded history, including current frame's grounded
    // Should be done at the beginning of every frame
    protected void UpdateGroundedHistory() {
        // Update past grounded values
        for (int i = grounded.Length-1; i > 0; i--) {
            grounded[i] = grounded[i - 1];
        }

        // Update current grounded value
        grounded[0] = false; 
        int count = rb2d.Cast(Vector2.down, contactFilter, hitResults, buffer);
        if (CompatibleCollisionFound(hitResults, count, Vector2.down)) {
            grounded[0] = true;
            isJumping = false; // Grounded -> user is not jumping
        }

        // Update whether player can request jump this frame (4)
        canRequestJump = false;
        count = rb2d.Cast(Vector2.down, contactFilter, hitResults, buffer + jumpBuffer);
        if (CompatibleCollisionFound(hitResults, count, Vector2.down)
            || withinJumpFrameBuffer()) {
            canRequestJump = true;
        }
    }

    protected virtual void ProcessJumpRequests() {
        requestedJump = false;
    }

    // Pull object downards. Creates illusion of downwards acceleration
    // Handle requests to jump, as well as cases such as falling off ledges
    protected void SimulateGravity() { 

        // Part 1: Process how much counteract to add to gravityCounteract based on the current state (4)
        // Object has left grounded state via falling off a ledge (not in jump state)
        if (grounded[1] && !grounded[0] && !isJumping && !requestedJump) { 
            gravityCounteract = gravity; 
        }

        // Object has requested to jump and is currently in a grounded state
        else if (grounded[0] && requestedJump && !isJumping) {
            gravityCounteract = jumpCounteract;
            isJumping = true;
            requestedJump = false;
        }

        // Object has recently left grounded state by falling off ledge and has requested to jump
        else if (requestedJump && withinJumpFrameBuffer()) {
            gravityCounteract = jumpCounteract;
            isJumping = true;
            requestedJump = false;
        }

        // Part 2: Process gravity's affect on the object this frame based on gravityCounteract
        // Decay gravityCounteract each frame
        gravityCounteract -= decay;
        if (gravityCounteract < 0) { gravityCounteract = 0; }

        // Move the object, and properly reflect the direction of movement for the object
        Move(Mathf.Sign(-gravity + gravityCounteract) * Vector2.up, Mathf.Abs(-gravity + gravityCounteract) * Time.deltaTime);

        // Compute booleans on current motion of object
        if (gravityCounteract > gravity) { 
            isMovingUp = true;
            isMovingDown = false;
        }
        else if (gravityCounteract < gravity && !grounded[0]) {
            isMovingUp = false;
            isMovingDown = true;
        }
        else {
            isMovingUp = false;
            isMovingDown = false;
        }
    }

    // Returns true when a user has recently when from grounded to not grounded state
    // Returns false if user is currently jumping, or too many frames have passed
    // Frames to determine whether it returns true in int jumpFrameBuffer
    private bool withinJumpFrameBuffer() {
        if (isJumping) {
            return false;
        }

        for (int i = 1; i < jumpFrameBuffer; i++) { // (5)
            if (grounded[i] == true) {
                return true;
            }
        }
        return false;
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

/* (4) How jumping works
 * An object can request a jump if any of these 3 conditions hold
 * 1. The object is grounded this frame
 * 2. The object is within the jumpBuffer of the platform this frame, but not grounded
 * 3. The object is not grounded nor within the jumpBuffer of any platform, but has recently walked off a ledge exactly jumpFrameBuffer frames ago NOT by jumping
 * If these conditions hold and an object requests a jump, we must process the request very carefully
 * If case 1 or case 2, we counteract gravity once the object is grounded (may not be right away)
 * If case 3, we must immediately process the request and counteract gravity even though the object is not grounded
 */ 

/* (5) Potential exploit with jumpBufferFrame
 * If the user has a very low framerate, Time.deltaTime will equalize it to make the game run at the same pace
 * But this function will work the exact same way, which means theoretically, a player could run off a ledge, go a huge distance if they have low framerate 
 * (within 5 frames they may go a significantly large distance) and then they could exploit this mechanic to jump alot further then they should be able to
 * This is why jumpBufferFrame should always be kept very low, but we should come up with a way to fix this eventually. 
 */