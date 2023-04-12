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
public class ProjectileBehaviour : ObjectBehaviour, Saveable<ProjectileData>, Spawnable<ProjectileData>
{
    public GameObject spriteObject;

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

    new protected void Awake()
    {
        base.Awake();
        SetSpeed(speedInitial);
    }

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
        // If guidance used, turn missile accordingly
        if (guidanceTarget != null && guidanceStep > 0) TurnMissile();
        // Acquire target if ID is given but target not found
        else if (guidanceTargetID != 0) guidanceTarget = HelpFunc.FindEntityByID(guidanceTargetID);
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

    public void CreateStructureCollider(GameObject groundReference)
    {
        float referenceHeight = groundReference.transform.position.y;
        float myHeight = transform.position.y;
        GameObject structureCollider = new GameObject("Structure_Collider");
        structureCollider.transform.parent = transform;
        structureCollider.transform.localPosition = new Vector3(0f, referenceHeight - myHeight, 0f);
        structureCollider.layer = 10;
        CircleCollider2D collider = structureCollider.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.1f;
        ProjectileTrigger trigger = structureCollider.AddComponent<ProjectileTrigger>();
        trigger.behaviour = this;
    }

    public void RotateSprite(float angle)
    {
        spriteObject.transform.Rotate(0.0f, 0.0f, angle);
        // Update velocity vector to match rotation
        SetMoveVector(HelpFunc.EulerToVec2(spriteObject.transform.rotation.eulerAngles.z));
    }

    private void TurnMissile()
    {
        Vector2 targetPos = guidanceTarget.transform.position;
        Vector2 missilePos = transform.position;
        Vector2 targetVec = targetPos - missilePos;
        float targetAngle = HelpFunc.Vec2ToAngle(targetVec);
        float currAngle = spriteObject.transform.rotation.eulerAngles.z;
        currAngle = HelpFunc.NormalizeAngle(currAngle);
        float angleDiff = -HelpFunc.SmallestAngle(targetAngle, currAngle);
        float maxStep = guidanceStep * Time.deltaTime;
        float step = Mathf.Clamp(angleDiff, -maxStep, maxStep);
        RotateSprite(step);
    }

    new public ProjectileData Save()
    {
        ProjectileData data = new ProjectileData(base.Save());
        data.speedInitial = speedInitial;
        data.acceleration = acceleration;
        data.lifespan = lifespan;
        data.damage = damage;
        data.lifeRemaining = lifeRemaining;
        data.projectileRotation = spriteObject.transform.localEulerAngles.z;
        return data;
    }

    public void Load(ProjectileData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        speedInitial = data.speedInitial;
        acceleration = data.acceleration;
        lifespan = data.lifespan;
        damage = data.damage;
        ownerID = data.ownerID;
        lifeRemaining = data.lifeRemaining;
        spriteObject.transform.localEulerAngles = new Vector3(0f, 0f, data.projectileRotation);
    }

    public static GameObject Spawn(ProjectileData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = ObjectBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<ProjectileBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(ProjectileData data, Transform parent = null)
    {
        GameObject obj = ObjectBehaviour.Spawn(data, parent);
        obj.GetComponent<ProjectileBehaviour>().Load(data);
        return obj;
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
    public float projectileRotation;
}