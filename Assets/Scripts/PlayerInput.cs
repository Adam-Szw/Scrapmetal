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

    public static bool one = false;
    public static bool two = false;
    public static bool leftclick = false;
    public static bool rightclick = false;

    void Update()
    {
        up = Input.GetKey(KeyCode.W);
        down = Input.GetKey(KeyCode.S);
        left = Input.GetKey(KeyCode.A);
        right = Input.GetKey(KeyCode.D);
        mousePos = Input.mousePosition;

        one = Input.GetKey(KeyCode.Alpha1);
        two = Input.GetKey(KeyCode.Alpha2);
        leftclick = Input.GetKey(KeyCode.Mouse0);
        rightclick = Input.GetKey(KeyCode.Mouse1);
    }
}
