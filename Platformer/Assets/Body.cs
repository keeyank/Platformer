using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Body : MonoBehaviour {

    private static int gravity = 5;
    protected float speed;

    protected Body(float speed) {
        this.speed = speed;
    }

    // Move body towards a certain direction, by a certain value
    // Direction can only be up, down, left, or right
    // Body must consist of a single Box Collider 2D
    // Assertion: "Collidable" tiles use a single BoxCollider2D
    protected void MoveBody(Vector2 direction, float speed) {
        Assert.IsTrue(direction == Vector2.down || direction == Vector2.up
            || direction == Vector2.left || direction == Vector2.right);

        BoxCollider2D coll = GetComponent<BoxCollider2D>();
        Vector3 newPos = transform.position + (Vector3)(direction * speed * Time.deltaTime);

        //// Compute points of rectangle
        //// Rectangle encompasses area the hitbox will take up after the frame
        //Vector2 pointA = new Vector2(coll.bounds.max.x, coll.bounds.min.y); // brCorner
        //Vector2 blCorner = new Vector2(coll.bounds.min.x, coll.bounds.min.y);
        //Vector2 pointB = blCorner + (direction * speed * Time.deltaTime);

        //Debug.DrawLine(pointA, pointB);

        //// Layer mask ensures only collidable objects are found in raycast
        //int layerMask = 1 << 8; // bit sequence 1000 0000 - Only 'collidable' layer

        //// Find any collisions via the rectangle
        //// Collider with maximum y value is the one the body's collider will hug
        //Collider2D[] colliders = Physics2D.OverlapAreaAll(pointA, pointB, layerMask);
        //float maxY = Mathf.NegativeInfinity;
        //BoxCollider2D maxYCollidable = null;
        //foreach (BoxCollider2D collidable in colliders) {
        //    if (collidable.bounds.max.y > maxY) {
        //        maxY = collidable.bounds.max.y;
        //        maxYCollidable = collidable;
        //    }
        //}

        ////  if a collidable was found, update newPos to reflect the max y of the collidable
        //if (maxYCollidable != null) {
        //    newPos = new Vector3(newPos.x, maxYCollidable.bounds.max.y + coll.bounds.extents.y, newPos.z);
        //}
         
        //// Update body's position
        //transform.position = newPos;

        // Define offset to add to raycast, since raycast begins at origin of collider
        float offset;
        if (direction == Vector2.left || direction == Vector2.right)
        {
            offset = coll.size.x/2;
        }
        else // direction is up or down
        {
            offset = coll.size.y/2;
        }

        // Layer mask ensures only collidable objects are found in raycast
        int layerMask = 1 << 8; // bit sequence 1000 0000 - Only 'collidable' layer

        // Cast boxcast 
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, coll.bounds.size, 
            transform.eulerAngles.z, direction, speed * Time.deltaTime, layerMask);
        ExtDebug.DrawBoxCastBox(transform.position, coll.bounds.extents, Quaternion.identity,
            direction, speed * Time.deltaTime, Color.magenta);


        // Snap newPos to edge of any colliders found
        if (hit.collider != null)
        {
            if (direction == Vector2.left)
            {
                newPos = new Vector3(hit.point.x + coll.size.x / 2, newPos.y, newPos.z);
            }
            else if (direction == Vector2.right)
            {
                newPos = new Vector3(hit.point.x - coll.size.x / 2, newPos.y, newPos.z);
            }
            else if (direction == Vector2.down)
            {
                newPos = new Vector3(newPos.x, hit.point.y + coll.size.y / 2, newPos.z);
            }
            else if (direction == Vector2.up)
            {
                newPos = new Vector3(newPos.x, hit.point.y - coll.size.y / 2, newPos.z);
            }
        }

        // Update the body's position
        transform.position = newPos;
    }

    protected virtual void Update() {   
        MoveBody(Vector2.down, gravity);
    }
}
