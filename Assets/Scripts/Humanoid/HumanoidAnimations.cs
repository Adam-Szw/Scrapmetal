using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements.Experimental;
using static UnityEngine.GraphicsBuffer;

public class HumanoidAnimations : MonoBehaviour
{

    [SerializeField] private Animator legsAnimator;
    [SerializeField] private Animator armsAnimator;
    [SerializeField] private Animator bodyAnimator;

    // Joints of a humanoid to be controlled by scripts
    [SerializeField] public GameObject pelvis;
    [SerializeField] public GameObject torso;
    [SerializeField] public GameObject head;
    [SerializeField] public GameObject arm_up_r;
    [SerializeField] public GameObject arm_low_r;
    [SerializeField] public GameObject hand_r;
    [SerializeField] public GameObject arm_up_l;
    [SerializeField] public GameObject arm_low_l;
    [SerializeField] public GameObject hand_l;
    [SerializeField] public GameObject leg_up_r;
    [SerializeField] public GameObject leg_low_r;
    [SerializeField] public GameObject foot_r;
    [SerializeField] public GameObject leg_up_l;
    [SerializeField] public GameObject leg_low_l;
    [SerializeField] public GameObject foot_l;

    public enum handsState
    {
        empty, pistol
    }

    public enum movementState
    {
        idle, walk, run
    }

    // Important
    // Animations are controlled through these flags
    public bool alive = true;
    public handsState stateHands = handsState.empty;
    public movementState stateMovement = movementState.idle;

    private enum Direction
    {
        neutral, left, right, up, down
    }
    private Direction horizontalDirection = Direction.neutral;
    private Direction verticalDirection = Direction.neutral;
    private Direction facingHorizontalDirection = Direction.neutral;
    private Direction facingVerticalDirection = Direction.neutral;
    private Vector2 facingVector = Vector2.zero;
    private Vector2 movementVector = Vector2.zero;
    private bool movingBackward = false;        // these 2 are not the same. backwards refers only to horizontal
    private bool movingAgainstFacing = false;   // misalignment of running and facing direction
    private Vector2 aimingLocation = Vector2.zero;

    void Update()
    {
        UpdateAnimators();
        if (alive) UpdateFacing();
        if (alive && stateHands == handsState.pistol)
        {
            UpdateArmBend();
            UpdateHandBend();
        }
    }

    public bool IsMovingBackwards() { return movingBackward; }

    public bool IsMovingAgainstFacing() { return movingAgainstFacing; }

    // This will trigger animations to adjust to the new movement and facing vectors for the character
    public void SetVectors(Vector2 movementVector, Vector2 facingVector, Vector2 aimingLocation)
    {
        if (!alive) return;
        this.movementVector = movementVector;
        this.facingVector = facingVector;
        this.aimingLocation = aimingLocation;

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
        pelvis.transform.localEulerAngles = Vector3.zero;
        torso.transform.localEulerAngles = Vector3.zero;
        head.transform.localEulerAngles = Vector3.zero;
        arm_up_r.transform.localEulerAngles = Vector3.zero;
        arm_low_r.transform.localEulerAngles = Vector3.zero;
        hand_r.transform.localEulerAngles = Vector3.zero;
        arm_up_l.transform.localEulerAngles = Vector3.zero;
        arm_low_l.transform.localEulerAngles = Vector3.zero;
        hand_l.transform.localEulerAngles = Vector3.zero;
        leg_up_r.transform.localEulerAngles = Vector3.zero;
        leg_low_r.transform.localEulerAngles = Vector3.zero;
        foot_r.transform.localEulerAngles = Vector3.zero;
        leg_up_l.transform.localEulerAngles = Vector3.zero;
        leg_low_l.transform.localEulerAngles = Vector3.zero;
        foot_l.transform.localEulerAngles = Vector3.zero;
    }

    public void PlayFlinch()
    {
        bodyAnimator.Play("Body_Flinch");
        if (stateMovement == movementState.idle) legsAnimator.Play("Legs_Flinch");
        if (stateHands == handsState.empty) armsAnimator.Play("Arms_Flinch");
    }

    // Flips a sprite to a set direction
    public void FlipSprite(bool right)
    {
        Vector3 scale = this.transform.localScale;
        Vector3 newScale = scale;
        newScale.x = Mathf.Abs(newScale.x) * (right ? 1.0f : -1.0f);
        this.transform.localScale = newScale;
    }

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

    // This is where animators are set based on whether or not the character holds weapons, is running etc.
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

        Vector2 targetVector = (aimingLocation - (Vector2) hand_r.transform.position).normalized;
        float targetAngle = Mathf.Atan2(targetVector.y, targetVector.x) * Mathf.Rad2Deg;
        targetAngle += hand_r.transform.lossyScale.x > 0 ? 0.0f : -180.0f;
        RotateJoint(hand_r.transform, targetAngle, HAND_R_BEND_STEP, false);

    }

}
