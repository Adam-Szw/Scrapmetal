using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class controls information for characters and enemies in the game
 */
public class CreatureBehaviour : EntityBehaviour
{
    private float health = 100.0f;
    private bool alive = true;

    [HideInInspector] public InventoryManager inventoryManager;

    public static string PREFAB_PATH;

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
        if (alive) AnimationUpdateFallback();
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
            FlinchFallback();
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
        if (!alive) SetSpeed(0.0f);
        if (!alive) DeathFallback();
    }

    public bool GetAlive() { return alive; }

    // Creatures inheriting should update animations on death
    protected virtual void DeathFallback() { }

    // Creatures inheriting from this should play flinch animation upon taking damage
    protected virtual void FlinchFallback() { }

    // Inherited class should call joint animation update per frame
    protected virtual void AnimationUpdateFallback() { }

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