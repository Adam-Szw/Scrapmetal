using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{

    [SerializeField] public EntityMovement movement;
    [SerializeField] public HumanoidAnimations animations;
    [SerializeField] public HumanoidBehaviour behaviour;

    [SerializeField] public float playerSpeed;
    [SerializeField] public float playerSpeedBackward;

    [SerializeField] public GameObject weaponSlot1;

    void Start()
    {
        
    }

    void Update()
    {
        // Placeholder for inventory system
        if (PlayerInput.two)
        {
            behaviour.SetWeaponActive(weaponSlot1);
            animations.stateHands = HumanoidAnimations.handsState.pistol;
        }

        if (PlayerInput.leftclick)
        {
            behaviour.ShootActiveWeaponOnce(CameraMovement.GetPointerWorldSpace());
        }


        // Update direction
        Vector2 moveVector = Vector3.zero;
        if(behaviour.alive)
        {
            if (PlayerInput.up) moveVector += new Vector2(0.0f, 1.0f);
            if (PlayerInput.down) moveVector += new Vector2(0.0f, -1.0f);
            if (PlayerInput.left) moveVector += new Vector2(-1.0f, 0.0f);
            if (PlayerInput.right) moveVector += new Vector2(1.0f, 0.0f);
            moveVector.Normalize();
        }

        // Update animations
        animations.SetVectors(moveVector, GetFacingVector(), CameraMovement.GetPointerWorldSpace());
        if (moveVector.magnitude > 0.0f) animations.stateMovement = 
                animations.IsMovingAgainstFacing() ? HumanoidAnimations.movementState.walk : HumanoidAnimations.movementState.run;
        else animations.stateMovement = HumanoidAnimations.movementState.idle;

        // Set speed
        movement.velocityVector = moveVector;
        movement.speed = 0.0f;
        if (behaviour.alive) movement.speed = animations.IsMovingAgainstFacing() ? playerSpeedBackward : playerSpeed;
    }

    //returns normalized vector that points from position of character to the pointer location
    private Vector2 GetFacingVector()
    {
        Vector2 targetVector = CameraMovement.GetPointerWorldSpace() - new Vector2(transform.position.x, transform.position.y);
        targetVector.Normalize();
        return targetVector;
    }

}
