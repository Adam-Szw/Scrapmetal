using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using static CreatureAnimations;
using static HumanoidAnimations;

// Humanoid characters require extra animations for hands etc. This class deals with it
public class HumanoidAnimations : CreatureAnimations, Saveable<HumanoidAnimationData>
{

    public enum handsState
    {
        empty, oneHand, twoHand
    }

    private Animator bodyAnimator;
    private Animator armsAnimator;
    private Animator legsAnimator;
    private handsState stateHands;

    private Joint head;
    private Joint torso;
    private Joint arm_up_r;
    private Joint arm_low_r;
    private Joint arm_up_l;
    private Joint arm_low_l;
    private Joint hand_r;

    // Animators have to come in order: body, arms, legs
    public HumanoidAnimations(Transform transform, List<Animator> animators, string[] jointNames, GameObject aimingBone) :
        base(transform, animators, jointNames, aimingBone)
    {
        useRunAnimation = true;
        useWalkBackwardAnimation = true;
        speedRunThreshold = 2.8f;
        speedMaxAnimationSpeed = 8.0f;
        bodyAnimator = animators[0];
        armsAnimator = animators[1];
        legsAnimator = animators[2];
        head = GetJointByName("Head_Parent").GetValueOrDefault();
        torso = GetJointByName("Torso_Parent").GetValueOrDefault();
        arm_up_r = GetJointByName("Arm_Up_R_Parent").GetValueOrDefault();
        arm_low_r = GetJointByName("Arm_Low_R_Parent").GetValueOrDefault();
        arm_up_l = GetJointByName("Arm_Up_L_Parent").GetValueOrDefault();
        arm_low_l = GetJointByName("Arm_Low_L_Parent").GetValueOrDefault();
        hand_r = GetJointByName("Hand_R_Parent").GetValueOrDefault();
    }

    public override void UpdateRotations()
    {
        // Bend head to look at the target
        if (head.obj && torso.obj) UpdateFacing();
        // Bend arm and hand to aim at the target and holding weapon
        if (stateHands == handsState.oneHand || stateHands == handsState.twoHand)
        {
            if (arm_up_r.obj && arm_low_r.obj) UpdateRightArmBend();
            if (hand_r.obj) UpdateHandBend();
        }
        if (stateHands == handsState.twoHand) UpdateLeftArmBend();
    }

    // Update state and cause animation update
    public void SetStateHands(handsState state)
    {
        stateHands = state;
        UpdateAnimators();
    }

    public handsState GetStateHands() { return stateHands; }

    public override void PlayFlinch()
    {
        bodyAnimator.Play("Body_Flinch");
        if (stateMovement == movementState.idle) legsAnimator.Play("Legs_Flinch");
        if (stateHands == handsState.empty) armsAnimator.Play("Arms_Flinch");
    }

    public new void UpdateAnimators()
    {
        base.UpdateAnimators();
        armsAnimator.SetInteger("HandsState", (int)stateHands);
    }

    // Updates bend of torso and head to look at the characters target
    private void UpdateFacing()
    {
        Vector2 aimingVector = GetAimingVector();
        if (aimingVector.magnitude < 0.7f) return;

        const float HEAD_BEND_MAX_ANGLE = 30.0f;
        const float HEAD_BEND_STEP = 60.0f;
        const float TORSO_BEND_MAX_ANGLE = 10.0f;
        const float TORSO_BEND_STEP = 20.0f;

        float angleFraction = Vector2.SignedAngle(new Vector2(aimingVector.x, 0.0f), aimingVector) / 90.0f;
        angleFraction *= Mathf.Sign(aimingVector.x);
        RotateJoint(head, HEAD_BEND_MAX_ANGLE * angleFraction, HEAD_BEND_STEP);
        RotateJoint(torso, TORSO_BEND_MAX_ANGLE * angleFraction, TORSO_BEND_STEP);
    }

    // Updates arm to point towards characters target when wielding weapon
    private void UpdateRightArmBend()
    {
        Vector2 aimingVector = GetAimingVector();
        if (aimingVector.magnitude < 0.7f) return;

        const float ARM_UP_R_BEND_MAX_ANGLE = 60.0f;
        const float ARM_UP_R_BEND_OFFSET_ANGLE = 30.0f;
        const float ARM_UP_R_BEND_STEP = 170.0f;
        const float ARM_LOW_R_BEND_MAX_ANGLE = 30.0f;
        const float ARM_LOW_R_BEND_OFFSET_ANGLE = -30.0f;
        const float ARM_LOW_R_BEND_STEP = 200.0f;

        float angleFraction = Vector2.SignedAngle(new Vector2(aimingVector.x, 0.0f), aimingVector) / 90.0f;
        angleFraction *= Mathf.Sign(aimingVector.x);
        RotateJoint(arm_up_r, (ARM_UP_R_BEND_MAX_ANGLE * angleFraction) + ARM_UP_R_BEND_OFFSET_ANGLE, ARM_UP_R_BEND_STEP);
        RotateJoint(arm_low_r, (ARM_LOW_R_BEND_MAX_ANGLE * angleFraction) + ARM_LOW_R_BEND_OFFSET_ANGLE, ARM_LOW_R_BEND_STEP);
    }

    // 2-handed weapons require special animation for left arm too
    private void UpdateLeftArmBend()
    {
        Vector2 aimingVector = GetAimingVector();
        if (aimingVector.magnitude < 0.7f) return;

        const float ARM_UP_L_BEND_MAX_ANGLE = 120.0f;
        const float ARM_UP_L_BEND_OFFSET_ANGLE = 30.0f;
        const float ARM_UP_L_BEND_STEP = 240.0f;
        const float ARM_LOW_L_BEND_MAX_ANGLE = 30.0f;
        const float ARM_LOW_L_BEND_OFFSET_ANGLE = -30.0f;
        const float ARM_LOW_L_BEND_STEP = 200.0f;

        float angleFraction = Vector2.SignedAngle(new Vector2(aimingVector.x, 0.0f), aimingVector) / 90.0f;
        angleFraction *= Mathf.Sign(aimingVector.x);
        RotateJoint(arm_up_l, (ARM_UP_L_BEND_MAX_ANGLE * angleFraction) + ARM_UP_L_BEND_OFFSET_ANGLE, ARM_UP_L_BEND_STEP);
        RotateJoint(arm_low_l, (ARM_LOW_L_BEND_MAX_ANGLE * angleFraction) + ARM_LOW_L_BEND_OFFSET_ANGLE, ARM_LOW_L_BEND_STEP);
    }

    // When using weapons, the hand that holds the weapon must point towards the target
    private void UpdateHandBend()
    {
        Vector2 aimingVector = GetAimingVector();
        if (aimingVector.magnitude < 0.7f) return;

        const float HAND_R_BEND_STEP = 300.0f;

        float targetAngle = HelpFunc.Vec2ToAngle(aimingVector);
        targetAngle += hand_r.obj.transform.lossyScale.x > 0 ? 0.0f : -180.0f;
        RotateJoint(hand_r, targetAngle, HAND_R_BEND_STEP, false);
    }

    public new HumanoidAnimationData Save()
    {
        HumanoidAnimationData data = new HumanoidAnimationData(base.Save());
        data.stateHands = stateHands;
        return data;
    }

    public void Load(HumanoidAnimationData data, bool loadTransform = true)
    {
        base.Load(data);
        SetStateHands(data.stateHands);
    }
}

[Serializable]
public class HumanoidAnimationData : CreatureAnimationData
{
    public HumanoidAnimationData() { }

    public HumanoidAnimationData(CreatureAnimationData data)
    {
        alive = data.alive;
        stateMovement = data.stateMovement;
        facingVector = data.facingVector;
        movementVector = data.movementVector;
        aimingLocation = data.aimingLocation;
        speed = data.speed;
        animatorsState = data.animatorsState;
        jointsAngles = data.jointsAngles;
    }

    public handsState stateHands;
}