using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Everything that is unique to the player and not other humanoid NPCs goes here
 */
public class PlayerBehaviour : HumanoidBehaviour
{

    public static string PLAYER_PATH = "Prefabs/Player";

    [SerializeField] private float playerSpeed;
    [SerializeField] private float playerSpeedBackward;

    // temporary - to be replaced by inventory system
    public GameObject weaponSlot1;

    // Values on last frame
    private bool upPressed = false;
    private bool downPressed = false;
    private bool leftPressed = false;
    private bool rightPressed = false;

    // Current movement direction vector deducted from inputs
    Vector2 moveVector = Vector3.zero;

    new void Update()
    {
        base.Update();

        // Trigger update of animations, rigidbody settings etc. if new input provided
        if (KeyStateChanged()) UpdateState();

        // Aiming needs to be done every frame
        animations.SetVectors(moveVector, GetFacingVector(), CameraControl.GetPointerWorldSpace());

        // Placeholder for inventory system
        if (PlayerInput.two)
        {
            SetWeaponActive(weaponSlot1);
            animations.SetStateHands(HumanoidAnimations.handsState.pistol);
        }

        if (PlayerInput.leftclick)
        {
            ShootActiveWeaponOnce(CameraControl.GetPointerWorldSpace());
        }
    }

    // Returns data containing all information about player in-game object
    public new PlayerData Save()
    {
        PlayerData data = new PlayerData(base.Save());
        return data;
    }

    // Load player using saved data
    public void Load(PlayerData data)
    {
        base.Load(data);
    }

    // Updates behaviour and animations of humanoid that are part of state machine
    private void UpdateState()
    {
        // Calculate target movement direction
        moveVector = Vector2.zero;
        if (GetAlive())
        {
            if (PlayerInput.up) moveVector += new Vector2(0.0f, 1.0f);
            if (PlayerInput.down) moveVector += new Vector2(0.0f, -1.0f);
            if (PlayerInput.left) moveVector += new Vector2(-1.0f, 0.0f);
            if (PlayerInput.right) moveVector += new Vector2(1.0f, 0.0f);
            moveVector.Normalize();
        }

        // Make player character move based on inputs
        SetVelocity(moveVector);
        SetSpeed(0.0f);
        if (GetAlive()) SetSpeed(animations.IsMovingAgainstFacing() ? playerSpeedBackward : playerSpeed);

        // Update animations based on inputs
        if (moveVector.magnitude > 0.0f) animations.SetStateMovement(
                animations.IsMovingAgainstFacing() ? HumanoidAnimations.movementState.walk : HumanoidAnimations.movementState.run);
        else animations.SetStateMovement(HumanoidAnimations.movementState.idle);
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

[Serializable]
public class PlayerData : HumanoidData
{
    public PlayerData() { }

    public PlayerData(HumanoidData data) : base(data)
    {
        this.weaponActive = data.weaponActive;
        this.bodypartData = data.bodypartData;
        this.animationData = data.animationData;
    }

}
