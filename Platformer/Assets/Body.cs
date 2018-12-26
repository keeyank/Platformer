using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class Body : MonoBehaviour
{

    private static int gravity = 5;
    protected int speed;

    protected Body(int speed)
    {
        this.speed = speed;
    }

    // Move body towards a certain direction, by a certain value
    // Direction can only be up, down, left, or right
    // Body must consist of a single Box Collider 2D
    protected void MoveBody(Vector3 direction, float speed)
    {
        Assert.IsTrue(direction == Vector3.down || direction == Vector3.up
            || direction == Vector3.left || direction == Vector3.right);

        BoxCollider2D coll = GetComponent<BoxCollider2D>();
        Vector3 newPos = transform.position + (direction * speed * Time.deltaTime);

        // Define offset to add to raycast, since raycast begins at origin of collider
        float offset;
        if (direction == Vector3.left || direction == Vector3.right)
        {
            offset = coll.size.x/2;
        }
        else // direction is up or down
        {
            offset = coll.size.y/2;
        }

        // Layer mask ensures only collidable objects are found in raycast
        int layerMask = 1 << 8; // bit sequence 1000 0000 - Only 'collidable' layer

        // Cast raycast 
        RaycastHit2D hit = 
            Physics2D.Raycast(transform.position, direction, offset + speed * Time.deltaTime, layerMask);
        Debug.DrawRay(transform.position, 
            (direction * offset) +  (direction * speed * Time.deltaTime), Color.yellow);

        // Snap newPos to edge of any colliders found
        if (hit.collider != null)
        {
            if (direction == Vector3.left)
            {
                newPos = new Vector3(hit.point.x + coll.size.x / 2, newPos.y, newPos.z);
            }
            else if (direction == Vector3.right)
            {
                newPos = new Vector3(hit.point.x - coll.size.x / 2, newPos.y, newPos.z);
            }
            else if (direction == Vector3.down)
            {
                newPos = new Vector3(newPos.x, hit.point.y + coll.size.y / 2, newPos.z);
            }
            else if (direction == Vector3.up)
            {
                newPos = new Vector3(newPos.x, hit.point.y - coll.size.y / 2, newPos.z);
            }
        }

        // Update the body's position
        transform.position = newPos;
    }

    protected virtual void Update()
    {
        MoveBody(Vector3.down, gravity);
    }
}
