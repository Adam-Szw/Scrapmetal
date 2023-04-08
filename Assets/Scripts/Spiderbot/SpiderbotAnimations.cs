using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CreatureAnimations;
using static HumanoidAnimations;

public class SpiderbotAnimations : CreatureAnimations
{

    private Animator bodyAnimator;

    private Joint sensor;
    private Joint turret;

    /* Animators should come in order: body, arms, legs
     */
    public SpiderbotAnimations(Transform transform, List<Animator> animators, string[] jointNames, string aimingBoneName) :
        base(transform, animators, jointNames, aimingBoneName)
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

    // Updates bend of torso to look at the target
    private void UpdateTurretAngle()
    {
        const float TURRET_BEND_MAX_ANGLE = 50.0f;
        const float TURRET_BEND_STEP = 40.0f;

        float angleFraction = Vector2.SignedAngle(new Vector2(facingVector.x, 0.0f), facingVector) / 90.0f;
        angleFraction *= Mathf.Sign(facingVector.x);
        RotateJoint(turret, TURRET_BEND_MAX_ANGLE * angleFraction, TURRET_BEND_STEP);
    }

    // Updates bend of sensor to look at the target
    private void UpdateSensorAngle()
    {
        const float SENSOR_BEND_MAX_ANGLE = 60.0f;
        const float SENSOR_BEND_STEP = 120.0f;

        float angleFraction = Vector2.SignedAngle(new Vector2(facingVector.x, 0.0f), facingVector) / 90.0f;
        angleFraction *= Mathf.Sign(facingVector.x);
        RotateJoint(sensor, SENSOR_BEND_MAX_ANGLE * angleFraction, SENSOR_BEND_STEP);
    }

    public new SpiderbotAnimationData Save()
    {
        SpiderbotAnimationData data = new SpiderbotAnimationData(base.Save());
        data.aimingVector = HelpFunc.VectorToArray(GetAimingVector());
        return data;
    }

    public void Load(SpiderbotAnimationData data)
    {
        base.Load(data);
        aimingVector = HelpFunc.DataToVec2(data.aimingVector);
    }

}

[Serializable]
public class SpiderbotAnimationData : CreatureAnimationData
{
    public SpiderbotAnimationData() { }

    public SpiderbotAnimationData(CreatureAnimationData data)
    {
        this.stateMovement = data.stateMovement;
        this.facingVector = data.facingVector;
        this.movementVector = data.movementVector;
        this.animatorsState = data.animatorsState;
        this.jointsAngles = data.jointsAngles;
    }

    public float[] aimingVector;
}