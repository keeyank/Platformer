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
    protected void MoveBody(Vector2 direction, float speed) {
        Assert.IsTrue(direction == Vector2.down || direction == Vector2.up
            || direction == Vector2.left || direction == Vector2.right);

        BoxCollider2D coll = GetComponent<BoxCollider2D>();
        Vector3 newPos = transform.position + (Vector3)(direction * speed * Time.deltaTime);

        // Layer mask ensures only collidable objects are found in raycast
        int layerMask = 1 << 8; // bit sequence 1000 0000 - Only 'collidable' layer

        // Define offset to add to raycast, since raycast begins at origin of collider
        Vector2 offset = direction * (coll.size/2) + 
            direction * (speed*Time.deltaTime/2);
        Vector2 boxSize;
        if (direction == Vector2.left || direction == Vector2.right) {
            boxSize = new Vector2(speed * Time.deltaTime, coll.size.y);
        }
        else {
            boxSize = new Vector2(coll.size.x, speed * Time.deltaTime);
        }

        // Cast boxcast to hit first collider in direction body is headed
        RaycastHit2D hit = Physics2D.BoxCast((Vector2)transform.position + offset, boxSize, 
            transform.eulerAngles.z, Vector2.zero, 0, layerMask);
        ExtDebug.DrawBoxCastBox((Vector2)transform.position + offset, boxSize/2, Quaternion.identity,
            Vector2.zero, 0, Color.magenta);


        // Snap newPos to edge of the collider found
        if (hit.collider != null) {
            Debug.Log(hit.point.y);
            Debug.Log(hit.point.y + coll.size.y / 2);
            if (direction == Vector2.left) {
                newPos = new Vector3(hit.collider.bounds.max.x + coll.size.x / 2, newPos.y, newPos.z);
            }
            else if (direction == Vector2.right) {
                newPos = new Vector3(hit.collider.bounds.min.x - coll.size.x / 2, newPos.y, newPos.z);
            }
            else if (direction == Vector2.down) {
                newPos = new Vector3(newPos.x, hit.collider.bounds.max.y + coll.size.y / 2, newPos.z); 
            }
            else if (direction == Vector2.up) { 
                newPos = new Vector3(newPos.x, hit.collider.bounds.min.y - coll.size.y / 2, newPos.z);
            }   
        }

        // Update the body's position
        transform.position = newPos;

    }

    protected virtual void Update() {   
        MoveBody(Vector2.down, gravity);
    }
}
