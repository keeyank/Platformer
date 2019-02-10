using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

//TODO: Move the code that sets gravityCounteract = 0 up to where grounded is being computed. I think that maeks more sense, but make sure it totally works
// both logically and via testing before you commit to it, since this won't fix any current bugs and the games working fine at this state.
// Also do the same thing with when gravityCounteract is set to gravity for upwards collisions
//TODO: Refactor how jumping works, I think it's kinda weird. It's weird how the cases take into account a case where the jump was requested at a previous frame,
// and a case where the jump was requested right away. Maybe it's ok I dunno, I can't think of a better way to refactor it right now but my brain is totally
// fried so whatever if you can htink of something better go for it but it's probly ok honestly, it seems to be working perfectly from testing.
//TODO: Refactor the wall Jumping code if you think it can be better, modify wall jump so that you can't move the direction of the wall jump if you're
// currently doing a wall jump (you can do this with math, doing stuff with checking wallJumpSpeed and currentSpeed, like only allowing to player to go
// wallJumpSpeed - playerSpeed in that direction or something like that. Or maybe some if statements that check if wallJumpSpeed is greater than a certain value)
public class PhysicsObject : MonoBehaviour {
    protected const float gravity = 11f;
    protected float gravityCounteract;
    private const float decay = 0.75f;

    private const float buffer = 0.01f; // Used to fix weird bug with collision detection
                                        // Player ends up slightly floating above platforms (corrected via platform hitboxes)

    protected const float jumpCounteract = 25f; // TODO: should be different for each instance of a physics body so use a constructur to fix this
    private bool requestedJump;
    private float timeWhenJumpRequested;
    private const float timeAllowableToSatisfyJumpRequest = 0.1f; // Allowable seconds passed before jumpRequest denied and reset to false if player not grounded

    protected float wallJumpSpeed;
    protected const float wallJumpSpeedMax = 12.5f;
    protected const float wallJumpDecay = 1.0f;
    protected const float wallJumpCounteract = 22.5f;
    protected const float wallJumpBuffer = 0.085f; // Allow some leeway for huggingLeftWall and huggingRightWall to be set to true
    protected bool huggingLeftWall;
    protected bool huggingRightWall;
    protected bool requestedRightWallJump;
    protected bool requestedLeftWallJump;


    // grounded[0] represents grounded state at current frame, grounded[1] represents grounded state 1 frame ago, 
    // Key assertion: If the object is in grounded[0] state, object is not in isJumping state
    protected bool[] grounded = new bool[10];
    private float timeWhenFellOffLedge;
    private const float timeNetLedgeFall = 0.15f; // Allowable
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
        UpdateCanRequestJumps();
        ProcessJumpRequests();
        SimulatePhysics();

    }

    // Update grounded history, including current frame's grounded
    // Should be done at the beginning of every frame
    protected void UpdateGroundedHistory() {
        // Update past grounded values
        for (int i = grounded.Length - 1; i > 0; i--) {
            grounded[i] = grounded[i - 1];
        }

        // Update current grounded value
        grounded[0] = false;
        int count = rb2d.Cast(Vector2.down, contactFilter, hitResults, buffer);
        if (CompatibleCollisionFound(hitResults, count, Vector2.down)) {
            grounded[0] = true;
            isJumping = false; // Grounded -> user is not jumping
        }
    }

    // Updates all the canRequestJumps booleans for this frame
    protected void UpdateCanRequestJumps() {

        huggingLeftWall = false;
        gameObject.GetComponent<SpriteRenderer>().color = Color.white;
        int count = rb2d.Cast(Vector2.left, contactFilter, hitResults, buffer + wallJumpBuffer);
        if (CompatibleCollisionFound(hitResults, count, Vector2.left)) {
            huggingLeftWall = true;
        }
        huggingRightWall = false;
        count = rb2d.Cast(Vector2.right, contactFilter, hitResults, buffer + wallJumpBuffer);
        if (CompatibleCollisionFound(hitResults, count, Vector2.right)) {
            huggingRightWall = true;
        }

    }

    // Override to determine when jumps are requested for physics object
    protected virtual void ProcessJumpRequests() {
        requestedJump = false;
        requestedRightWallJump = false;
        requestedLeftWallJump = false;
    }

    // Pull object downards. Creates illusion of downwards acceleration
    // Handle requests to jump, as well as cases such as falling off ledges
    protected void SimulatePhysics() {
        CalculateMovement();

        // Move the object, and properly reflect the direction of movement for the object
        Move(Mathf.Sign(-gravity + gravityCounteract) * Vector2.up, Mathf.Abs(-gravity + gravityCounteract) * Time.deltaTime);
        Move(Mathf.Sign(wallJumpSpeed) * Vector2.right, Mathf.Abs(wallJumpSpeed) * Time.deltaTime);

        UpdateMotionBools();
    }

    // Calculate movement speed based on Jump requests (4)
    private void CalculateMovement() {

        // Set requestedJump to false if the time since the last jump that was requested has expired
        if ((Time.realtimeSinceStartup - timeWhenJumpRequested) > timeAllowableToSatisfyJumpRequest) {
            requestedJump = false;
        }

        /* 1. Process Jump Requests based on objects current position (4)*/
        
        // Object has requested to jump and is currently in a grounded state
        // This case has priority over wall jumping (both cases can be true at same time)
        if (grounded[0] && requestedJump && !isJumping) {
            gravityCounteract = jumpCounteract;
            isJumping = true;
            ResetJumpRequests();
        }

        // Object has recently left grounded state by falling off ledge and has requested to jump
        // Has priority over wall jumping (can be true while the condition for wall jumping is true)
        else if (requestedJump && !isJumping && !grounded[0] &&
            (Time.realtimeSinceStartup - timeWhenFellOffLedge) < timeNetLedgeFall) {
            gravityCounteract = jumpCounteract;
            isJumping = true;
            ResetJumpRequests();
        }

        // Object has requested to wall jump
        else if (requestedRightWallJump || requestedLeftWallJump) {
            if (requestedRightWallJump) {
                wallJumpSpeed = wallJumpSpeedMax;
            }
            if (requestedLeftWallJump) {
                wallJumpSpeed = -wallJumpSpeedMax;
            }
            gravityCounteract = wallJumpCounteract;
            isJumping = true;
            ResetJumpRequests();
        }

        // Object has left grounded state via falling off a ledge (not in jump state)
        if (grounded[1] && !grounded[0] && !isJumping) {
            gravityCounteract = gravity;
            timeWhenFellOffLedge = Time.realtimeSinceStartup;
        }

        /* 2. Decay each movement to approach 0  */
        // Decay gravityCounteract each frame
        gravityCounteract -= decay;
        if (gravityCounteract < 0) { gravityCounteract = 0; }

        // Make wallJumpSpeed approach 0 each frame
        if (wallJumpSpeed > 0) {
            wallJumpSpeed -= wallJumpDecay;
            if (wallJumpSpeed < 0) { wallJumpSpeed = 0; }
        }
        else if (wallJumpSpeed < 0) {
            wallJumpSpeed += wallJumpDecay;
            if (wallJumpSpeed > 0) { wallJumpSpeed = 0; }
        }
    }

    private void ResetJumpRequests() {
        requestedJump = false;
        requestedLeftWallJump = false;
        requestedRightWallJump = false;
    }

    // Compute booleans on current motion of object this frame
    private void UpdateMotionBools() {
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

    // Request a jump and note the time that the jump is requested
    protected void requestJump() {
        timeWhenJumpRequested = Time.realtimeSinceStartup;
        requestedJump = true;
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

/* (4) How jumping works
 * An object can request a jump at any point in time
 * The request is only fulfilled if within a certain amount of time the player is grounded, or if the player has recently left the grounded state NOT by jumping
 * (aka by falling off something or being teleported or something)
 * If the player is grounded, the request will be fulfilled right away, otherwise it will be fulfilled as soon as the player lands if it's within the timer
 * The timer resets everytime the player hits space
 * The player may also jump if he is hugging a wall, which will occur if neither of the first 2 conditions are satisfied and the third is
 * After a successful jump occurs, all other jump requests are reset to false
 * 
 * Key Point: If any form of a jump occurs, all jump requests must be immediately reset (the player's jump input has been used up so we don't want it to be used 
 * again at any point). 
 */

