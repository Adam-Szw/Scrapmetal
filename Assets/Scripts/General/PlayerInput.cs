using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public static bool up = false;
    public static bool down = false;
    public static bool left = false;
    public static bool right = false;

    public static bool one = false;
    public static bool two = false;
    public static bool leftclick = false;
    public static bool rightclick = false;

    public static bool esc = false;

    public static Vector3 mousePos = new Vector3(0.0f, 0.0f, 0.0f);

    void Update()
    {
        up = Input.GetKey(KeyCode.W);
        down = Input.GetKey(KeyCode.S);
        left = Input.GetKey(KeyCode.A);
        right = Input.GetKey(KeyCode.D);

        one = Input.GetKeyDown(KeyCode.Alpha1);
        two = Input.GetKeyDown(KeyCode.Alpha2);
        leftclick = Input.GetKeyDown(KeyCode.Mouse0);
        rightclick = Input.GetKeyDown(KeyCode.Mouse1);

        esc = Input.GetKeyDown(KeyCode.Escape);

        mousePos = Input.mousePosition;
    }
}
