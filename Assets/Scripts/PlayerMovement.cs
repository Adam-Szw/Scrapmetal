using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] public float speed;
    [SerializeField] public float speedBackwardMultiplier;
    [SerializeField] public Animator legsAnimator;
    [SerializeField] public Animator armsAnimator;

    [SerializeField] public float headBendMaxAngles;
    [SerializeField] public float headBendCompletionTime;
    [SerializeField] public float torsoBendMaxAngles;
    [SerializeField] public float torsoBendCompletionTime;

    [SerializeField] public GameObject torso;
    [SerializeField] public GameObject head;

    [SerializeField] public GameObject armUpRight;
    [SerializeField] public GameObject handBone;
    [SerializeField] public GameObject weapon;
    private GameObject spawnedWeapon;

    private Vector3 moveVector = new Vector3(0.0f, 0.0f, 0.0f);

    //direction of movement
    private Direction horizontalDirection = Direction.neutral;
    private Direction verticalDirection = Direction.neutral;

    //movement flags
    private bool movingAgainstPointer = false;
    private bool movingBackwards = false;

    //aiming stuff (temporary)
    private bool weaponEquipped = false;
    private bool aiming = false;

    private Transform torsoBoneTransform;
    private Transform headBoneTransform;
    private Transform armUpRightTransform;

    private enum Direction
    {
        neutral, left, right, up, down
    }

    void Start()
    {
        torsoBoneTransform = torso.transform;
        headBoneTransform = head.transform;
        armUpRightTransform = armUpRight.transform;
    }

    void Update()
    {
        //get direction
        moveVector = new Vector3(0.0f, 0.0f, 0.0f);
        if (PlayerInput.up) moveVector += new Vector3(0.0f, 1.0f, 0.0f);
        if (PlayerInput.down) moveVector += new Vector3(0.0f, -1.0f, 0.0f);
        if (PlayerInput.left) moveVector += new Vector3(-1.0f, 0.0f, 0.0f);
        if (PlayerInput.right) moveVector += new Vector3(1.0f, 0.0f, 0.0f);

        //apply transformations
        FlipSprite();
        BendBodypart(headBoneTransform, false, headBendMaxAngles, headBendCompletionTime, 180.0f, GetFacingVector());
        BendBodypart(torsoBoneTransform, false, torsoBendMaxAngles, torsoBendCompletionTime, 0.0f, GetFacingVector());

        //aiming stuff (temporary)
        if (PlayerInput.two) 
        {
            if (handBone.transform.childCount == 0) spawnedWeapon = Instantiate(weapon, handBone.transform);
            spawnedWeapon.GetComponent<SpriteRenderer>().sortingOrder = 13;
            weaponEquipped = true;
        }
        if (PlayerInput.one) 
        {
            if (handBone.transform.childCount > 0) Destroy(handBone.transform.GetChild(0).gameObject);
            weaponEquipped = false;
        }
        if (PlayerInput.rightclick && weaponEquipped) aiming = true;
        if (!PlayerInput.rightclick) aiming = false;
        armsAnimator.SetBool("Aiming", aiming);
        armsAnimator.SetBool("1-H-Weapon Equipped", weaponEquipped);
        if (aiming) BendBodypart(armUpRightTransform, false, 90, 0.1f, 180.0f, GetFacingVector());
        else
        {
            armUpRightTransform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);
        }

        //apply animation
        UpdateDirections(moveVector);
        UpdateMovementFlags();
        int movemode = moveVector.magnitude > 0.0f ? 2 : 0;
        if (movemode == 2 && movingAgainstPointer) movemode = 1;
        legsAnimator.SetInteger("Run_State", movemode);
        legsAnimator.SetBool("Backward", movingBackwards);
        armsAnimator.SetInteger("Run_State", movemode);
        armsAnimator.SetBool("Backward", movingBackwards);

        //apply movement
        Move();
    }

    private void Move()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        float speedMod = speed;
        if (movingAgainstPointer) speedMod *= speedBackwardMultiplier;
        rb.velocity = moveVector.normalized * speedMod;
    }

    //returns true if flip was needed
    private bool FlipSprite()
    {
        bool flipNeeded = false;

        Vector3 scale = this.transform.localScale;
        Vector3 newScale = scale;

        Vector2 pointerFacing = GetFacingVector();
        if (pointerFacing.x < 0.0f) newScale.x = -1.0f;
        else newScale.x = 1.0f;

        if (newScale != scale) flipNeeded = true;

        this.transform.localScale = newScale;

        return flipNeeded;
    }

    //updates directions based on current movement
    private void UpdateDirections(Vector3 velocity)
    {
        horizontalDirection = Direction.neutral;
        if (velocity.x > 0.0f) horizontalDirection = Direction.right;
        if (velocity.x < 0.0f) horizontalDirection = Direction.left;
        verticalDirection = Direction.neutral;
        if (velocity.y > 0.0f) verticalDirection = Direction.up;
        if (velocity.y < 0.0f) verticalDirection = Direction.down;
    }

    //deduce movement flags based on comparing pointer position and movement
    private void UpdateMovementFlags()
    {
        bool againstPointer = false;
        bool backward = false;

        //get pointer directions
        Vector2 pointerFacing = GetFacingVector();
        Direction pointerHorDir;
        Direction pointerVerDir;
        if (pointerFacing.x >= 0.0f) pointerHorDir = Direction.right;
        else pointerHorDir = Direction.left;
        if (pointerFacing.y >= 0.0f) pointerVerDir = Direction.up;
        else pointerVerDir = Direction.down;

        //compare to character directions
        if (verticalDirection != Direction.neutral &&
            verticalDirection != pointerVerDir) againstPointer = true;
        if (horizontalDirection != Direction.neutral &&
            horizontalDirection != pointerHorDir)
        {
            againstPointer = true;
            backward = true;
        }

        movingAgainstPointer = againstPointer;
        movingBackwards = backward;
    }

    //returns direction vector of cursor
    private Vector2 GetFacingVector()
    {
        Vector2 targetVector = CameraMovement.GetCameraWorldPoint() - new Vector2(transform.position.x, transform.position.y);
        return targetVector;
    }

    //applies rotation to bodypart based on parameters
    private void BendBodypart(Transform bodypartTransform, bool instant, float maxBend, float bendCompleteTime, float baseAngle, Vector2 facing)
    {
        float targetAngle = Mathf.Sign(facing.x) * Vector2.SignedAngle(new Vector2(facing.x, 0.0f), facing);
        targetAngle *= maxBend / 90.0f;
        targetAngle += baseAngle;
        Vector2 targetVector = new Vector2(Mathf.Cos(targetAngle * Mathf.Deg2Rad), Mathf.Sin(targetAngle * Mathf.Deg2Rad)).normalized;
        targetVector.x *= Mathf.Sign(facing.x);

        Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, targetVector);
        bodypartTransform.rotation = Quaternion.RotateTowards(bodypartTransform.rotation, targetRotation,
            instant ? 360.0f : maxBend * Time.deltaTime / bendCompleteTime);
    }
}
