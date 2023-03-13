using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using static UnityEngine.GraphicsBuffer;

public class HumanoidAnimations
{
    private Transform transform;
    private Animator legsAnimator;
    private Animator armsAnimator;
    private Animator bodyAnimator;

    // Joints that can be controlled by scripts
    private GameObject pelvis = null;
    private GameObject torso = null;
    private GameObject head = null;
    private GameObject arm_up_r = null;
    private GameObject arm_low_r = null;
    private GameObject hand_r = null;
    private GameObject arm_up_l = null;
    private GameObject arm_low_l = null;
    private GameObject hand_l = null;
    private GameObject leg_up_r = null;
    private GameObject leg_low_r = null;
    private GameObject foot_r = null;
    private GameObject leg_up_l = null;
    private GameObject leg_low_l = null;
    private GameObject foot_l = null;

    public enum handsState
    {
        empty, pistol
    }

    public enum movementState
    {
        idle, walk, run
    }

    // These flags control animation state machine
    private bool alive = true;
    private handsState stateHands = handsState.empty;
    private movementState stateMovement = movementState.idle;
    private Vector2 facingVector = Vector2.zero;
    private Vector2 movementVector = Vector2.zero;

    // Useful flags and enum for calculations. Readonly for outside of this class
    private enum Direction
    {
        neutral, left, right, up, down
    }
    private Direction horizontalDirection = Direction.neutral;
    private Direction verticalDirection = Direction.neutral;
    private Direction facingHorizontalDirection = Direction.neutral;
    private Direction facingVerticalDirection = Direction.neutral;
    private bool movingBackward = false;        // these 2 are not the same. backwards refers only to horizontal
    private bool movingAgainstFacing = false;   // misalignment of running and facing direction
    private Vector2 aimingVector = Vector2.zero;

    // Parent refers to parent object of all animated joints in the object
    public HumanoidAnimations(GameObject parent, Animator legsAnimator, Animator armsAnimator, Animator bodyAnimator, Transform transform)
    {
        this.transform = transform;
        SetJoints(parent);
        SetAnimators(legsAnimator, armsAnimator, bodyAnimator);
    }

    /* Causes animation class to obtain all joints that can be script controlled, given their parent object.
     * We might want to call this again after constructor if original joint objects are replaced or removed
     */
    public void SetJoints(GameObject parent)
    {
        pelvis = HelpFunc.RecursiveFindChild(parent, "Pelvis_Parent");
        torso = HelpFunc.RecursiveFindChild(parent, "Torso_Parent");
        head = HelpFunc.RecursiveFindChild(parent, "Head_Parent");
        arm_up_r = HelpFunc.RecursiveFindChild(parent, "Arm_Up_R_Parent");
        arm_low_r = HelpFunc.RecursiveFindChild(parent, "Arm_Low_R_Parent");
        hand_r = HelpFunc.RecursiveFindChild(parent, "Hand_R_Parent");
        arm_up_l = HelpFunc.RecursiveFindChild(parent, "Arm_Up_L_Parent");
        arm_low_l = HelpFunc.RecursiveFindChild(parent, "Arm_Low_L_Parent");
        hand_l = HelpFunc.RecursiveFindChild(parent, "Hand_L_Parent");
        leg_up_r = HelpFunc.RecursiveFindChild(parent, "Leg_Up_R_Parent");
        leg_low_r = HelpFunc.RecursiveFindChild(parent, "Leg_Low_R_Parent");
        foot_r = HelpFunc.RecursiveFindChild(parent, "Foot_R_Parent");
        leg_up_l = HelpFunc.RecursiveFindChild(parent, "Leg_Up_L_Parent");
        leg_low_l = HelpFunc.RecursiveFindChild(parent, "Leg_Low_L_Parent");
        foot_l = HelpFunc.RecursiveFindChild(parent, "Foot_L_Parent");
    }

    // Updates animators
    public void SetAnimators(Animator legsAnimator, Animator armsAnimator, Animator bodyAnimator)
    {
        this.legsAnimator = legsAnimator;
        this.armsAnimator = armsAnimator;
        this.bodyAnimator = bodyAnimator;
    }

    // Increments progress of joint rotations. Should be called on update in owner object
    public void UpdateRotations()
    {
        // Bend head to look at the target
        if (head && torso) UpdateFacing();
        // Bend arm and hand to aim at the target and holding weapon
        if (stateHands == handsState.pistol)
        {
            if (arm_up_r && arm_low_r) UpdateArmBend();
            if (hand_r) UpdateHandBend();
        }
    }

    // Update alive flag and change state of animation based on that
    public void SetAlive(bool alive)
    {
        this.alive = alive;
        UpdateAnimators();
        if (!alive) ResetJoints();
    }

    // Update state and cause animation update
    public void SetStateHands(handsState state)
    {
        stateHands = state;
        UpdateAnimators();
    }

    public handsState GetStateHands() { return stateHands; }

    // Update state and cause animation update
    public void SetStateMovement(movementState state)
    {
        stateMovement = state;
        UpdateAnimators();
    }

    public movementState GetStateMovement() { return stateMovement; }

    /* Sets various vectors that control humanoid's animations, such as aiming location which will make the 
     * character look at that point. This will trigger animations to adjust to the new vectors
     */
    public void SetVectors(Vector2 movementVector, Vector2 facingVector, Vector2 aimingLocation)
    {
        if (!alive) return;
        this.movementVector = movementVector;
        this.facingVector = facingVector;
        aimingVector = (aimingLocation - (Vector2)hand_r.transform.position).normalized;

        // Update direction flags of movement and facing
        UpdateDirections(movementVector, facingVector);

        // Apply transformations based on facing direction
        switch (facingHorizontalDirection)
        {
            case Direction.right:
                FlipSprite(true);
                break;
            case Direction.left:
                FlipSprite(false);
                break;
        }
    }

    public Vector2 GetFacingVector() { return facingVector; }

    public Vector2 GetMovementVector() { return movementVector; }

    public Vector2 GetAimingVector() { return aimingVector; }

    // Rotate a script-controlled joint to a certain angle. Actual rotation in any given frame is split
    // into steps calculated from completion time so call this every frame to complete the rotation
    public void RotateJoint(Transform transform, float targetAngle, float step, bool local=true)
    {
        Quaternion targetRotation = Quaternion.Euler(0.0f, 0.0f, targetAngle);
        if(local) transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, Time.deltaTime * step);
        else transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Time.deltaTime * step);
    }

    // Resets script controlled joints to default (0 degrees) angle
    public void ResetJoints()
    {
        if (pelvis) pelvis.transform.localEulerAngles = Vector3.zero;
        if (torso) torso.transform.localEulerAngles = Vector3.zero;
        if (head) head.transform.localEulerAngles = Vector3.zero;
        if (arm_up_r) arm_up_r.transform.localEulerAngles = Vector3.zero;
        if (arm_low_r) arm_low_r.transform.localEulerAngles = Vector3.zero;
        if (hand_r) hand_r.transform.localEulerAngles = Vector3.zero;
        if (arm_up_l) arm_up_l.transform.localEulerAngles = Vector3.zero;
        if (arm_low_l) arm_low_l.transform.localEulerAngles = Vector3.zero;
        if (hand_l) hand_l.transform.localEulerAngles = Vector3.zero;
        if (leg_up_r) leg_up_r.transform.localEulerAngles = Vector3.zero;
        if (leg_low_r) leg_low_r.transform.localEulerAngles = Vector3.zero;
        if (foot_r) foot_r.transform.localEulerAngles = Vector3.zero;
        if (leg_up_l) leg_up_l.transform.localEulerAngles = Vector3.zero;
        if (leg_low_l) leg_low_l.transform.localEulerAngles = Vector3.zero;
        if (foot_l) foot_l.transform.localEulerAngles = Vector3.zero;
    }

    // Plays flinching animation once
    public void PlayFlinch()
    {
        bodyAnimator.Play("Body_Flinch");
        if (stateMovement == movementState.idle) legsAnimator.Play("Legs_Flinch");
        if (stateHands == handsState.empty) armsAnimator.Play("Arms_Flinch");
    }

    // Flips a sprite to a set direction
    public void FlipSprite(bool right)
    {
        Vector3 scale = transform.localScale;
        Vector3 newScale = scale;
        newScale.x = Mathf.Abs(newScale.x) * (right ? 1.0f : -1.0f);
        transform.localScale = newScale;
    }

    public bool IsMovingBackwards() { return movingBackward; }

    public bool IsMovingAgainstFacing() { return movingAgainstFacing; }

    public HumanoidAnimationData Save()
    {
        HumanoidAnimationData data = new HumanoidAnimationData();
        data.stateHands = GetStateHands();
        data.stateMovement = GetStateMovement();
        data.facingVector = HelpFunc.VectorToArray(GetFacingVector());
        data.movementVector = HelpFunc.VectorToArray(GetMovementVector());
        data.aimingVector = HelpFunc.VectorToArray(GetAimingVector());
        data.animatorsState = GetState();

        return data;
    }

    public void Load(HumanoidAnimationData data)
    {
        SetStateHands(data.stateHands);
        SetStateMovement(data.stateMovement);
        SetVectors(HelpFunc.DataToVec2(data.facingVector), HelpFunc.DataToVec2(data.movementVector), HelpFunc.DataToVec2(data.aimingVector));
        List<Tuple<int, float>> state = data.animatorsState;
        legsAnimator.Play(state[0].Item1, 0, state[0].Item2);
        armsAnimator.Play(state[1].Item1, 0, state[1].Item2);
        bodyAnimator.Play(state[2].Item1, 0, state[2].Item2);
    }

    /* Get the current state of each animator as a tuple of hashed state ID and progress
     * The order is: legs, arms, body animators
     */
    private List<Tuple<int, float>> GetState()
    {
        List<Tuple<int, float>> ret = new List<Tuple<int, float>>();
        int state = legsAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        float progress = legsAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        ret.Add(new Tuple<int, float>(state, progress));
        state = armsAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        progress = armsAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        ret.Add(new Tuple<int, float>(state, progress));
        state = bodyAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash;
        progress = bodyAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        ret.Add(new Tuple<int, float>(state, progress));
        return ret;
    }

    // todo
    private List<float> GetJointsAngles()
    {
        List<float> jointsAngles = new List<float>();
        foreach(string bodypart in HumanoidBehaviour.BODYPARTS)
        {
            
        }
        return jointsAngles;
    }

    // Updates movement and facing direction flags
    private void UpdateDirections(Vector3 velocityVector, Vector3 facingVector)
    {
        horizontalDirection = Direction.neutral;
        if (velocityVector.x > 0.0f) horizontalDirection = Direction.right;
        if (velocityVector.x < 0.0f) horizontalDirection = Direction.left;
        verticalDirection = Direction.neutral;
        if (velocityVector.y > 0.0f) verticalDirection = Direction.up;
        if (velocityVector.y < 0.0f) verticalDirection = Direction.down;
        facingHorizontalDirection = Direction.neutral;
        if (facingVector.x > 0.0f) facingHorizontalDirection = Direction.right;
        if (facingVector.x < 0.0f) facingHorizontalDirection = Direction.left;
        facingVerticalDirection = Direction.neutral;
        if (facingVector.y > 0.0f) facingVerticalDirection = Direction.up;
        if (facingVector.y < 0.0f) facingVerticalDirection = Direction.down;

        movingBackward = false;
        if (horizontalDirection != Direction.neutral &&
            facingHorizontalDirection != Direction.neutral &&
            horizontalDirection != facingHorizontalDirection) movingBackward = true;

        movingAgainstFacing = false;
        if (horizontalDirection != Direction.neutral && horizontalDirection != facingHorizontalDirection) movingAgainstFacing = true;
        if (verticalDirection != Direction.neutral && verticalDirection != facingVerticalDirection) movingAgainstFacing = true;
    }

    /* This is where animators are set based on whether or not the character holds weapons, is running etc.
     * Should be called whenever changes are made to the animations state
     */
    private void UpdateAnimators()
    {
        armsAnimator.SetBool("Alive", alive);
        legsAnimator.SetBool("Alive", alive);
        bodyAnimator.SetBool("Alive", alive);

        // Change animation to weapon stance
        if (stateHands != handsState.empty)
        {
            if(stateHands == handsState.pistol) armsAnimator.SetInteger("HandsState", 1);
            // more space for future weapons
        }
        // Change to empty hands stance otherwise
        else armsAnimator.SetInteger("HandsState", 0);

        if(stateMovement == movementState.idle)
        {
            armsAnimator.SetInteger("MovementState", 0);
            legsAnimator.SetInteger("MovementState", 0);
            bodyAnimator.SetInteger("MovementState", 0);
        }

        if(stateMovement == movementState.walk)
        {
            armsAnimator.SetInteger("MovementState", !IsMovingBackwards() ? 1 : 2);
            legsAnimator.SetInteger("MovementState", !IsMovingBackwards() ? 1 : 2);
            bodyAnimator.SetInteger("MovementState", !IsMovingBackwards() ? 1 : 2);
        }

        if(stateMovement == movementState.run)
        {
            armsAnimator.SetInteger("MovementState", 3);
            legsAnimator.SetInteger("MovementState", 3);
            bodyAnimator.SetInteger("MovementState", 3);
        }

    }

    // Updates bend of torso and head to look at the characters target
    private void UpdateFacing()
    {
        const float HEAD_BEND_MAX_ANGLE = 30.0f;
        const float HEAD_BEND_STEP = 60.0f;
        const float TORSO_BEND_MAX_ANGLE = 10.0f;
        const float TORSO_BEND_STEP = 20.0f;

        float angleFraction = Vector2.SignedAngle(new Vector2(facingVector.x, 0.0f), facingVector) / 90.0f;
        angleFraction *= Mathf.Sign(facingVector.x);
        RotateJoint(head.transform, HEAD_BEND_MAX_ANGLE * angleFraction, HEAD_BEND_STEP);
        RotateJoint(torso.transform, TORSO_BEND_MAX_ANGLE * angleFraction, TORSO_BEND_STEP);
    }

    // Updates arm to point towards characters target when wielding weapon
    private void UpdateArmBend()
    {
        const float ARM_UP_R_BEND_MAX_ANGLE = 60.0f;
        const float ARM_UP_R_BEND_OFFSET_ANGLE = 30.0f;
        const float ARM_UP_R_BEND_STEP = 170.0f;
        const float ARM_LOW_R_BEND_MAX_ANGLE = 30.0f;
        const float ARM_LOW_R_BEND_OFFSET_ANGLE = -30.0f;
        const float ARM_LOW_R_BEND_STEP = 200.0f;

        float angleFraction = Vector2.SignedAngle(new Vector2(facingVector.x, 0.0f), facingVector) / 90.0f;
        angleFraction *= Mathf.Sign(facingVector.x);
        RotateJoint(arm_up_r.transform, (ARM_UP_R_BEND_MAX_ANGLE * angleFraction) + ARM_UP_R_BEND_OFFSET_ANGLE, ARM_UP_R_BEND_STEP);
        RotateJoint(arm_low_r.transform, (ARM_LOW_R_BEND_MAX_ANGLE * angleFraction) + ARM_LOW_R_BEND_OFFSET_ANGLE, ARM_LOW_R_BEND_STEP);
    }

    // When using weapons, the hand that holds the weapon must point towards the target

    // TODO - there is a bug if mouse pointer is over the weapon where the hand twists too far
    private void UpdateHandBend()
    {
        const float HAND_R_BEND_STEP = 300.0f;

        float targetAngle = Mathf.Atan2(aimingVector.y, aimingVector.x) * Mathf.Rad2Deg;
        targetAngle += hand_r.transform.lossyScale.x > 0 ? 0.0f : -180.0f;
        RotateJoint(hand_r.transform, targetAngle, HAND_R_BEND_STEP, false);
    }

}
