using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CreatureAnimations;
using static HumanoidAnimations;

public class ZapperAnimations : CreatureAnimations, Saveable<ZapperAnimationData>
{

    private Animator bodyAnimator;

    private Joint sensor;
    private Joint turret;

    /* Animators should come in order: body, arms, legs
     */
    public ZapperAnimations(Transform transform, List<Animator> animators, string[] jointNames, GameObject aimingBone) :
        base(transform, animators, jointNames, aimingBone)
    {
        bodyAnimator = animators[0];
        sensor = GetJointByName("Sensor_Parent").GetValueOrDefault();
        turret = GetJointByName("Turret_Parent").GetValueOrDefault();
    }

    // Increments progress of joint rotations. Should be called on update in owner object
    public override void UpdateRotations()
    {
        // Bend turret to look at target
        if (turret.obj) UpdateTurretAngle();
        // Same for sensor
        if (sensor.obj) UpdateSensorAngle();
    }

    // Plays flinching animation once
    public override void PlayFlinch()
    {
        bodyAnimator.Play("Body_Flinch");
    }

    // Updates turret to always face directly towards target
    private void UpdateTurretAngle()
    {
        const float TURRET_BEND_STEP = 40.0f;

        Vector2 aimingVector = GetAimingVector();

        float targetAngle = HelpFunc.Vec2ToAngle(aimingVector);
        targetAngle += turret.obj.transform.lossyScale.x > 0 ? 0.0f : -180.0f;
        RotateJoint(turret, targetAngle, TURRET_BEND_STEP, false);
    }

    // Updates bend of sensor to look at the target
    private void UpdateSensorAngle()
    {
        const float SENSOR_BEND_MAX_ANGLE = 90.0f;
        const float SENSOR_BEND_STEP = 220.0f;

        float angleFraction = Vector2.SignedAngle(new Vector2(facingVector.x, 0.0f), facingVector) / 90.0f;
        angleFraction *= Mathf.Sign(facingVector.x);
        RotateJoint(sensor, SENSOR_BEND_MAX_ANGLE * angleFraction, SENSOR_BEND_STEP);
    }

    public new ZapperAnimationData Save()
    {
        ZapperAnimationData data = new ZapperAnimationData(base.Save());
        return data;
    }

    public void Load(ZapperAnimationData data, bool loadTransform = true)
    {
        base.Load(data);
    }

}

[Serializable]
public class ZapperAnimationData : CreatureAnimationData
{
    public ZapperAnimationData() { }

    public ZapperAnimationData(CreatureAnimationData data)
    {
        this.alive = data.alive;
        this.stateMovement = data.stateMovement;
        this.facingVector = data.facingVector;
        this.movementVector = data.movementVector;
        this.aimingLocation = data.aimingLocation;
        this.speed = data.speed;
        this.animatorsState = data.animatorsState;
        this.jointsAngles = data.jointsAngles;
    }

}