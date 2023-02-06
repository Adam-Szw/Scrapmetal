using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] public Camera currentCamera;
    [SerializeField] public GameObject player;

    [SerializeField] public float minSize;
    [SerializeField] public float maxSize;
    [SerializeField] public float growthStartMultiplier;
    [SerializeField] public float growthMultiplier;

    void Update()
    {
        Vector2 playerPos = player.transform.position;
        Vector2 newPos = (GetPointerWorldSpace() + playerPos) / 2.0f;
        currentCamera.transform.position = new Vector3(newPos.x, newPos.y, -10.0f);

        Vector2 posRelative = newPos - playerPos;
        currentCamera.orthographicSize = Mathf.Max(minSize, Mathf.Min(posRelative.magnitude * growthMultiplier
            + growthStartMultiplier * minSize, maxSize));
    }

    public static Vector2 GetPointerWorldSpace()
    {
        Vector3 mouseRelative = Camera.main.ScreenToWorldPoint(PlayerInput.mousePos);
        mouseRelative.z = 0;
        return mouseRelative;
    }
}
