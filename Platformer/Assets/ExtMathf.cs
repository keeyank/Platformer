using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtMathf
{
    public static Vector2 Vector2Abs(Vector2 v) { 
        return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
    }
}
