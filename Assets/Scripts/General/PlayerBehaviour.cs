using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Everything that is unique to the player and not other humanoid NPCs goes here
 */
public class PlayerBehaviour : HumanoidBehaviour
{

    public static new string PREFAB_PATH = "Prefabs/Creatures/Player";

    [SerializeField] private float playerSpeed;
    [SerializeField] private float playerSpeedBackward;

    new void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;

        // Aiming needs to be done every frame
        animations.SetAimingVector(PlayerInput.mousePos);

        // Trigger update of animations, rigidbody settings etc. if new input provided
        if (PlayerInput.InputChanged()) UpdateState();

        // Placeholder for inventory system
        if (PlayerInput.two)
        {
            //item rework
            GameObject gun = EntityBehaviour.Spawn("Prefabs/Items/Weapons/Rivetgun", transform.position, transform.rotation);
            WeaponData data = gun.GetComponent<WeaponBehaviour>().Save();
            Destroy(gun);
            SetItemActive(data);
            animations.SetStateHands(HumanoidAnimations.handsState.pistol);
        }

        if (PlayerInput.leftclick)
        {
            ShootActiveWeaponOnce(PlayerInput.mousePos);
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
        // Update facing
        animations.SetFacingVector(GetFacingVector());

        // Calculate target movement direction
        Vector2 moveVector = Vector2.zero;
        if (GetAlive())
        {
            if (PlayerInput.up) moveVector += new Vector2(0.0f, 1.0f);
            if (PlayerInput.down) moveVector += new Vector2(0.0f, -1.0f);
            if (PlayerInput.left) moveVector += new Vector2(-1.0f, 0.0f);
            if (PlayerInput.right) moveVector += new Vector2(1.0f, 0.0f);
            moveVector.Normalize();

        }

        // Make player character move based on inputs
        SetMoveVector(moveVector);
        if (moveVector.magnitude > 0) SetSpeed(animations.IsMovingAgainstFacing() ? playerSpeedBackward : playerSpeed);
        else SetSpeed(0.0f);
    }

    // Returns normalized vector that points from position of character to the pointer location
    private Vector2 GetFacingVector()
    {
        Vector2 targetVector = PlayerInput.mousePos - new Vector2(transform.position.x, transform.position.y);
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
        this.itemActive = data.itemActive;
        this.bodypartData = data.bodypartData;
        this.animationData = data.animationData;
    }

}
