﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.U2D.Animation;
using static CreatureAnimations;

/* Handles both dumb bullets and guided missiles
 */
public class ProjectileBehaviour : ObjectBehaviour
{
    // bullets behaviour
    public float speedInitial;
    public float acceleration;
    public float lifespan;
    public float lifeRemaining;
    public float damage;

    // guided missile behaviour
    /* Value here indicates how quickly the missile will turn to hit a target. For example
     * value 360 means it will turn a full circle within a second to hit a target.
     */
    public float guidanceStep = 0;
    [HideInInspector] public ulong guidanceTargetID = 0;
    [HideInInspector] public GameObject guidanceTarget = null;

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
        // If guidance used, turn missile accordingly
        if (guidanceTarget != null && guidanceStep > 0) TurnMissile();
        // Acquire target if ID is given but target not found
        else if (guidanceTargetID != 0) guidanceTarget = HelpFunc.FindGameObjectByBehaviourID(guidanceTargetID);
        // Update velocity vector to match rotation
        float angle = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        float x = Mathf.Cos(angle);
        float y = Mathf.Sin(angle);
        SetVelocity(new Vector2(x, y));
        // Update speed
        SetSpeed(Mathf.Max(GetSpeed() + acceleration * Time.deltaTime, 0.0f));
        // Update lifetime
        lifeRemaining -= Time.deltaTime;
        if (lifeRemaining < 0.0f) Destroy(gameObject);
    }
    public static GameObject Spawn(ProjectileData data)
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>(data.prefabPath));
        obj.GetComponent<ProjectileBehaviour>().Load(data);
        return obj;
    }

    private void TurnMissile()
    {
        Vector2 targetPos = guidanceTarget.transform.position;
        Vector2 missilePos = transform.position;
        Vector2 targetVec = targetPos - missilePos;
        float targetAngle = HelpFunc.Vec2ToAngle(targetVec);
        float currAngle = transform.rotation.eulerAngles.z;
        currAngle = HelpFunc.NormalizeAngle(currAngle);
        float angleDiff = -HelpFunc.SmallestAngle(targetAngle, currAngle);
        float maxStep = guidanceStep * Time.deltaTime;
        float step = Mathf.Clamp(angleDiff, -maxStep, maxStep);
        transform.Rotate(0.0f, 0.0f, step);
    }

    new public ProjectileData Save()
    {
        ProjectileData data = new ProjectileData(base.Save());
        data.speedInitial = speedInitial;
        data.acceleration = acceleration;
        data.lifespan = lifespan;
        data.damage = damage;
        data.lifeRemaining = lifeRemaining;
        return data;
    }

    public void Load(ProjectileData data)
    {
        base.Load(data);
        speedInitial = data.speedInitial;
        acceleration = data.acceleration;
        lifespan = data.lifespan;
        damage = data.damage;
        ownerID = data.ownerID;
        lifeRemaining = data.lifeRemaining;
    }

}

[Serializable]
public class ProjectileData : ObjectData
{
    public ProjectileData() { }

    public ProjectileData(ObjectData data) : base(data)
    {
        this.prefabPath = data.prefabPath;
        this.ownerID = data.ownerID;
    }

    public float speedInitial;
    public float acceleration;
    public float lifespan;
    public float damage;
    public float lifeRemaining;
    public List<string> graphicsData;
}