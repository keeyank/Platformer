using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Body : MonoBehaviour
{

    private static int gravity = 5;
    protected int speed;

    protected Body(int speed)
    {
        this.speed = speed;
    }

    // Move body towards a certain direction, by a certain value
    protected void MoveBody(Vector3 direction, float speed)
    {
        Vector3 newPos = transform.position + (direction * speed * Time.deltaTime);

        Vector3 ySize = new Vector3(0, GetComponent<BoxCollider2D>().size.y/2, 0);
        Vector3 startPos = transform.position - ySize;
        int layerMask = 1 << 8; // bit sequence 1000 0000 - Only 'collidable' layer

        // Cast raycast and find a collider it hits
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, speed * Time.deltaTime, layerMask);
        Debug.DrawRay(startPos, direction * speed * Time.deltaTime, Color.yellow);
        if (hit.collider != null)
        {
            Debug.Log(hit.collider.gameObject.name);    
        }



        // Update gameObject's transform.position here based on whether a collidable was found
    }

    protected virtual void Update()
    {
        MoveBody(Vector3.down, gravity);
    }
}
