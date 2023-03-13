using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Everything that is unique to the player and not other humanoid NPCs goes here
 */
public class PlayerBehaviour : MonoBehaviour
{
    [SerializeField] private float playerSpeed;
    [SerializeField] private float playerSpeedBackward;

    // temporary - to be replaced by inventory system
    public GameObject weaponSlot1;

    private HumanoidBehaviour humanoidBehaviour;

    // Values on last frame
    private bool upPressed = false;
    private bool downPressed = false;
    private bool leftPressed = false;
    private bool rightPressed = false;

    // Current movement direction vector deducted from inputs
    Vector2 moveVector = Vector3.zero;

    void Awake()
    {
        humanoidBehaviour = this.gameObject.GetComponent<HumanoidBehaviour>();
    }

    void Update()
    {
        if (GlobalControl.paused) return;

        // Trigger update of animations, rigidbody settings etc. if new input provided
        if (KeyStateChanged()) UpdateState();

        // Aiming needs to be done every frame
        humanoidBehaviour.animations.SetVectors(moveVector, GetFacingVector(), CameraControl.GetPointerWorldSpace());

        // Placeholder for inventory system
        if (PlayerInput.two)
        {
            humanoidBehaviour.SetWeaponActive(weaponSlot1);
            humanoidBehaviour.animations.SetStateHands(HumanoidAnimations.handsState.pistol);
        }

        if (PlayerInput.leftclick)
        {
            humanoidBehaviour.ShootActiveWeaponOnce(CameraControl.GetPointerWorldSpace());
        }
    }

    // Returns data containing all information about player in-game object
    public Tuple<PlayerData, HumanoidData> Save()
    {
        PlayerData dataP = new PlayerData();
        HumanoidData dataH = humanoidBehaviour.Save();
        return new Tuple<PlayerData, HumanoidData>(dataP, dataH);
    }

    // Load player using saved data
    public void Load(Tuple<PlayerData, HumanoidData> data)
    {
        humanoidBehaviour.Load(data.Item2);
        // todo: data
    }

    // Updates behaviour and animations of humanoid that are part of state machine
    private void UpdateState()
    {
        // Calculate target movement direction
        moveVector = Vector2.zero;
        if (humanoidBehaviour.GetAlive())
        {
            if (PlayerInput.up) moveVector += new Vector2(0.0f, 1.0f);
            if (PlayerInput.down) moveVector += new Vector2(0.0f, -1.0f);
            if (PlayerInput.left) moveVector += new Vector2(-1.0f, 0.0f);
            if (PlayerInput.right) moveVector += new Vector2(1.0f, 0.0f);
            moveVector.Normalize();
        }

        // Make player character move based on inputs
        humanoidBehaviour.SetVelocityVector(moveVector);
        humanoidBehaviour.SetSpeed(0.0f);
        if (humanoidBehaviour.GetAlive()) humanoidBehaviour.SetSpeed(humanoidBehaviour.animations.IsMovingAgainstFacing() ? playerSpeedBackward : playerSpeed);

        // Update animations based on inputs
        if (moveVector.magnitude > 0.0f) humanoidBehaviour.animations.SetStateMovement(
                humanoidBehaviour.animations.IsMovingAgainstFacing() ? HumanoidAnimations.movementState.walk : HumanoidAnimations.movementState.run);
        else humanoidBehaviour.animations.SetStateMovement(HumanoidAnimations.movementState.idle);
    }

    // Detects if state of keyboard has changed since last call
    private bool KeyStateChanged()
    {
        bool changed = false;
        if (PlayerInput.up != upPressed) changed = true;
        if (PlayerInput.down != downPressed) changed = true;
        if (PlayerInput.left != leftPressed) changed = true;
        if (PlayerInput.right != rightPressed) changed = true;
        upPressed = PlayerInput.up;
        downPressed = PlayerInput.down;
        leftPressed = PlayerInput.left;
        rightPressed = PlayerInput.right;
        return changed;
    }

    // Returns normalized vector that points from position of character to the pointer location
    private Vector2 GetFacingVector()
    {
        Vector2 targetVector = CameraControl.GetPointerWorldSpace() - new Vector2(transform.position.x, transform.position.y);
        targetVector.Normalize();
        return targetVector;
    }

}
