using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class allows us to make custom bindings to the keys
 */
public class PlayerInput : MonoBehaviour
{
    public static Camera currCamera = null;

    public static bool up = false;
    public static bool down = false;
    public static bool left = false;
    public static bool right = false;

    public static bool num1 = false;
    public static bool num2 = false;
    public static bool num3 = false;
    public static bool num4 = false;
    public static bool leftclick = false;
    public static bool rightclick = false;

    public static bool esc = false;
    public static bool tab = false;

    public static Vector2 mousePos = Vector2.zero;

    // Last frame values
    private static bool upLast = false;
    private static bool downLast = false;
    private static bool leftLast = false;
    private static bool rightLast = false;

    private static int mouseLastQuadrant = 0;

    void Update()
    {
        up = Input.GetKey(KeyCode.W);
        down = Input.GetKey(KeyCode.S);
        left = Input.GetKey(KeyCode.A);
        right = Input.GetKey(KeyCode.D);

        num1 = Input.GetKeyDown(KeyCode.Alpha1);
        num2 = Input.GetKeyDown(KeyCode.Alpha2);
        num3 = Input.GetKeyDown(KeyCode.Alpha3);
        num4 = Input.GetKeyDown(KeyCode.Alpha4);
        leftclick = Input.GetKeyDown(KeyCode.Mouse0);
        rightclick = Input.GetKeyDown(KeyCode.Mouse1);

        esc = Input.GetKeyDown(KeyCode.Escape);
        tab = Input.GetKeyDown(KeyCode.Tab);

        mousePos = GetMousePositionRelative();
    }

    public static bool InputChanged()
    {
        bool inputUpdated = false;

        // Check key input
        if (up != upLast) inputUpdated = true;
        if (down != downLast) inputUpdated = true;
        if (left != leftLast) inputUpdated = true;
        if (right != rightLast) inputUpdated = true;
        upLast = up;
        downLast = down;
        leftLast = left;
        rightLast = right;

        // Check mouse quadrant
        int mouseQuadrant = GetMouseQuadrant();
        if (mouseQuadrant != mouseLastQuadrant) inputUpdated = true;
        mouseLastQuadrant = mouseQuadrant;
        return inputUpdated;
    }

    public static Vector2 GetMousePositionRelative()
    {
        Vector2 mouseRelative = currCamera.ScreenToWorldPoint(Input.mousePosition);
        return mouseRelative;
    }

    private static int GetMouseQuadrant()
    {
        int quadrant;
        Vector2 playerPos = GlobalControl.GetPlayerTransform().position;
        if (mousePos.y > playerPos.y)
        {
            if (mousePos.x > playerPos.x) quadrant = 0;
            else quadrant = 1;
        }
        else
        {
            if (mousePos.x <= playerPos.x) quadrant = 2;
            else quadrant = 3;
        }
        return quadrant;
    }

}
