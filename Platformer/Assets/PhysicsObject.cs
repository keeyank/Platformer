using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class PhysicsObject : MonoBehaviour {

    protected float currentSpeed;

    [SerializeField] protected float maxSpeed;
    [SerializeField] protected float minSpeed;
    [SerializeField] protected float acceleration;
    [SerializeField] protected float wallMinSpeed;
    [SerializeField] protected float gravityCounteractReduce;
    [SerializeField] protected float wallJumpLockedInSpeed; // Assert: wallJumpLockedInSpeed <= wallJumpSpeed

    [SerializeField] protected float gravity;
    protected float gravityCounteract;
    [SerializeField] private float decay;

    private const float buffer = 0.01f; // Used to fix weird bug with collision detection
                                        // Player ends up slightly floating above platforms (corrected via platform hitboxes)
    [SerializeField] protected float jumpCounteract;
    private bool requestedJump;
    private float timeWhenJumpRequested;
    private const float timeAllowableToSatisfyJumpRequest = 0.065f; // Allowable seconds passed before jumpRequest denied and reset to false if player not grounded

    protected float wallJumpSpeed;
    [SerializeField] protected float wallJumpSpeedMax;
    [SerializeField] protected float wallJumpDecay;
    [SerializeField] protected float wallJumpCounteract;
    protected const float wallJumpBuffer = 0.1f; // Allow some leeway for huggingLeftWall and huggingRightWall to be set to true
    protected bool huggingLeftWallJumpWall;
    protected bool huggingRightWallJumpWall;

    // grounded[0] represents grounded state at current frame, grounded[1] represents grounded state 1 frame ago, 
    // Key assertion: If the object is in grounded[0] state, object is not in isJumping state
    protected bool[] grounded = new bool[2];
    private float timeWhenFellOffLedge;
    private const float timeNetLedgeFall = 0.15f; // Allowable time for player to jump after falling off a ledge

    protected bool isJumping;
    protected bool isMovingUp;
    protected bool isMovingDown;

    protected Rigidbody2D rb2d;
    protected ContactFilter2D contactFilter;
    protected RaycastHit2D[] hitResults = new RaycastHit2D[16];


    void OnEnable() {
        rb2d = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start() {
        // Set up contact filter to include only collidable layer objects
        //contactFilter.useTriggers = false; // Don't include triggers in results
        contactFilter.SetLayerMask(1 << 8); // Include from collidable layer only
        contactFilter.useLayerMask = true;
    }

    protected virtual void Update() {
        UpdateGroundedHistory();
        UpdateHuggingWalls();
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
        if (CompatibleCollisionFound(hitResults, count, Vector2.down) && gravityCounteract <= 0) { // (2)
            grounded[0] = true;
            isJumping = false; // Grounded -> user is not jumping
        }
    }

    // Updates all the huggingWalls booleans for this frame
    protected void UpdateHuggingWalls() {

        huggingLeftWallJumpWall = false;
        gameObject.GetComponent<SpriteRenderer>().color = Color.white;
        int count = rb2d.Cast(Vector2.left, contactFilter, hitResults, buffer + wallJumpBuffer);
        if (CompatibleCollisionFound(hitResults, count, Vector2.left)) {

            // Loop through the hits gameobjects to see if any are wall jump walls
            for (int i = 0; i < count; i++) {
                if (hitResults[i].collider.gameObject.tag == "WallJump") {
                    huggingLeftWallJumpWall = true;
                }
            }
        }
        huggingRightWallJumpWall = false;
        count = rb2d.Cast(Vector2.right, contactFilter, hitResults, buffer + wallJumpBuffer);
        if (CompatibleCollisionFound(hitResults, count, Vector2.right)) {

            // Loop through the hits gameobjects to see if any are wall jump walls
            for (int i = 0; i < count; i++) {
                if (hitResults[i].collider.gameObject.tag == "WallJump") {
                    huggingRightWallJumpWall = true;
                }
            }
        }
    }

    // Override to determine when jumps are requested for physics object
    protected virtual void ProcessJumpRequests() {
        requestedJump = false;
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
        // If the jump is satisfied this frame, this will never reset requestedJump
        if ((Time.realtimeSinceStartup - timeWhenJumpRequested) > timeAllowableToSatisfyJumpRequest) {
            requestedJump = false;
        }

        /* 1. Process Jump Requests based on objects current position (4)*/

        if (requestedJump) {
            // Object has requested to jump and is currently in a grounded state
            // This case has priority over wall jumping (both cases can be true at same time)
            if (grounded[0] && !isJumping) {
                gravityCounteract = jumpCounteract;
                isJumping = true;
                requestedJump = false;
            }

            // Object has requested to jump while next to a wall
            // Has priority over falling off ledge jumping
            else if (huggingRightWallJumpWall || huggingLeftWallJumpWall) {
                if (huggingLeftWallJumpWall) {
                    wallJumpSpeed = wallJumpSpeedMax;
                }
                if (huggingRightWallJumpWall) {
                    wallJumpSpeed = -wallJumpSpeedMax;
                }
                gravityCounteract = wallJumpCounteract;
                isJumping = true;
                requestedJump = false;
            }

            // Object has recently left grounded state by falling off ledge and has requested to jump
            // Least priority form of jumping
            else if (!isJumping && !grounded[0] &&
                (Time.realtimeSinceStartup - timeWhenFellOffLedge) < timeNetLedgeFall) {
                gravityCounteract = jumpCounteract;
                isJumping = true;
                requestedJump = false;
            }
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
                gravityCounteract = 0;

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
                gravityCounteract = gravity;
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

                // update horizontal speeds to be minimized
                if (wallJumpSpeed < 0) { wallJumpSpeed = 0; }
                currentSpeed = minSpeed;
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

                // Minimize horizontal speeds
                if (wallJumpSpeed > 0) { wallJumpSpeed = 0; }
                currentSpeed = minSpeed;
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
/* (2) 
 * This was put there to fix a bug where the player can actually be grounded while jumping upwards due to holding right next to a block with low upwards motion
 */

/* (4) How jumping works
 * An object can request a jump at any point in time
 * The request is only fulfilled if within a certain amount of time the player is grounded, or if the player has recently left the grounded state NOT by jumping
 * (aka by falling off something or being teleported or something)
 * If the player is grounded, the request will be fulfilled right away, otherwise it will be fulfilled as soon as the player lands if it's within the timer
 * The timer resets everytime the player hits space
 * The player may also jump if he is hugging a wall, which will occur if neither of the first 2 conditions are satisfied and the third is
 * After a successful jump occurs, the jump request is set to false
 */

