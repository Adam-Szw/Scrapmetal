using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class controls information for characters and enemies in the game
 */
public class CreatureBehaviour : EntityBehaviour
{
    public enum FactionAllegiance
    {
        player,
        neutral,    // Uninteractive background creatures i.e. a rabbit, never fight back
        NPC,        // Non playable neutral characters (villagers etc.), fight hostile creatures
        NPCaggro,   // NPCs that were triggered to be aggressive to player
        hostile,    // Always hostile creatures (example: wild dog), attack everything on sight except each other
        berserk,    // Attacks everything including in own faction
        enemy       // Enemy faction, attack player only but not NPCs etc.
    }
    public FactionAllegiance faction = FactionAllegiance.neutral;
    public GameObject visionBlocker;

    public float moveSpeed = 0.0f;
    private float health = 100.0f;
    private bool alive = true;

    [HideInInspector] public InventoryManager inventoryManager;
    [HideInInspector] public bool stunned = false;

    public static string PREFAB_PATH;

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
        if (alive) GetAnimations().UpdateRotations();
    }

    // Handle collision with projectiles
    public void OnTriggerEnter2D(Collider2D other)
    {
        // If hit by a bullet
        if (!(other.gameObject.GetComponent<ProjectileBehaviour>() == null))
        {
            ProjectileBehaviour bulletBehaviour = other.gameObject.GetComponent<ProjectileBehaviour>();
            // Do nothing if hit yourself
            if (bulletBehaviour.ownerID == ID) return;
            DealDamage(bulletBehaviour.damage);
            Destroy(other.gameObject);
            if (GetAlive()) GetAnimations().PlayFlinch();
        }
    }

    public void DealDamage(float amount)
    {
        health -= amount;
        if (health <= 0) SetAlive(false);
    }

    public void SetAlive(bool alive)
    {
        this.alive = alive;
        if (!alive) DisableColliders(transform);
        if (!alive) base.SetSpeed(0.0f);
        if (!alive) GetAnimations().SetAlive(GetAlive());
    }

    public bool GetAlive() { return alive; }

    public new void SetSpeed(float speed)
    {
        if (!alive) return;
        if (!stunned) base.SetSpeed(speed);
        GetAnimations().SetSpeed(speed);
    }

    public new void SetMoveVector(Vector2 moveVector)
    {
        if (!alive) return;
        if (!stunned) base.SetMoveVector(moveVector);
        GetAnimations().SetMovementVector(moveVector.normalized);
    }

    public void SetFacingVector(Vector2 facingVector)
    {
        if (!alive) return;
        GetAnimations().SetFacingVector(facingVector);
    }

    public void SetAimingVector(Vector2 aimLocation)
    {
        if (!alive) return;
        GetAnimations().SetAimingVector(aimLocation);
    }

    public void SetAttackTarget(GameObject target)
    {
        if (!alive) return;
        foreach (WeaponBehaviour w in GetWeapons())
        {
            w.guidanceTarget = target;
            w.guidanceTargetID = 0;
            w.target = target.transform.position;
        }
    }

    public bool AnyWeaponOnTarget() 
    {
        if (!alive) return false;
        foreach (WeaponBehaviour w in GetWeapons())
        {
            if (w.IsOnTarget()) return true;
        }
        return false;
    }

    public void Attack() 
    {
        if (!alive) return;
        foreach (WeaponBehaviour w in GetWeapons())
        {
            if (w.IsOnTarget()) w.Use();
        }
    }

    protected virtual CreatureAnimations GetAnimations() { return null; }

    protected virtual List<WeaponBehaviour> GetWeapons() { return null; }

    protected new CreatureData Save()
    {
        CreatureData data = new CreatureData(base.Save());
        data.alive = this.alive;
        data.health = this.health;
        return data;
    }

    protected void Load(CreatureData data)
    {
        base.Load(data);
        alive = data.alive;
        health = data.health;
    }

}

[Serializable]
public class CreatureData : EntityData
{
    public CreatureData() { }

    public CreatureData(EntityData data)
    {
        this.ID = data.ID;
        this.location = data.location;
        this.rotation = data.rotation;
        this.scale = data.scale;
        this.velocity = data.velocity;
        this.speed = data.speed;
    }

    public bool alive;
    public float health;
}