using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

/* Class responsible for controlling in-game view
 */
public class CameraControl
{
    public Camera currentCamera;
    public GameObject player;

    private float minSize = 5.0f;
    private float maxSize = 20.0f;
    private float growthStartMultiplier = 0.6f;
    private float growthMultiplier = 0.4f;

    // Using this camera as a reference, determines world space location of mouse pointer
    public static Vector2 GetPointerWorldSpace()
    {
        Vector3 mouseRelative = Camera.main.ScreenToWorldPoint(PlayerInput.mousePos);
        mouseRelative.z = 0;
        return mouseRelative;
    }

    public void AdjustCameraToPlayer()
    {
        Vector2 playerPos = player.transform.position;
        Vector2 newPos = (GetPointerWorldSpace() + playerPos) / 2.0f;
        currentCamera.transform.position = new Vector3(newPos.x, newPos.y, -10.0f);

        Vector2 posRelative = newPos - playerPos;
        currentCamera.orthographicSize = Mathf.Max(minSize, Mathf.Min(posRelative.magnitude * growthMultiplier
            + growthStartMultiplier * minSize, maxSize));
    }

    public CameraData Save()
    {
        CameraData data = new CameraData();
        data.position = HelpFunc.VectorToArray(currentCamera.transform.localPosition);
        data.rotation = HelpFunc.QuaternionToArray(currentCamera.transform.localRotation);
        data.scale = HelpFunc.VectorToArray(currentCamera.transform.localScale);
        data.minSize = this.minSize;
        data.maxSize = this.maxSize;
        data.growthStartMultiplier = this.growthStartMultiplier;
        data.growthMultiplier = this.growthMultiplier;
        return data;
    }

    public void Load(CameraData data)
    {
        this.currentCamera.transform.localPosition = HelpFunc.DataToVec3(data.position);
        this.currentCamera.transform.localRotation = HelpFunc.DataToQuaternion(data.rotation);
        this.currentCamera.transform.localScale = HelpFunc.DataToVec3(data.scale);
        this.minSize = data.minSize;
        this.maxSize = data.maxSize;
        this.growthStartMultiplier = data.growthStartMultiplier;
        this.growthMultiplier = data.growthMultiplier;
    }
}

[Serializable]
public class CameraData
{
    public float[] position;
    public float[] rotation;
    public float[] scale;
    public float minSize;
    public float maxSize;
    public float growthStartMultiplier;
    public float growthMultiplier;
}