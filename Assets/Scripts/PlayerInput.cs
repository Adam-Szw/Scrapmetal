using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public static bool up = false;
    public static bool down = false;
    public static bool left = false;
    public static bool right = false;
    public static Vector3 mousePos = new Vector3(0.0f, 0.0f, 0.0f);

    void Update()
    {
        up = Input.GetKey(KeyCode.UpArrow);
        down = Input.GetKey(KeyCode.DownArrow);
        left = Input.GetKey(KeyCode.LeftArrow);
        right = Input.GetKey(KeyCode.RightArrow);
        mousePos = Input.mousePosition;
    }
}
