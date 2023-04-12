using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static CreatureBehaviour;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.GraphicsBuffer;

/* This class controls information for characters and enemies in the game
 */
public class CreatureBehaviour : EntityBehaviour, Saveable<CreatureData>, Spawnable<CreatureData>
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
    public GameObject healthbar = null;
    public FactionAllegiance faction = FactionAllegiance.neutral;
    public CreatureAI aiControl = null;

    public float moveSpeed = 0.0f;
    [SerializeField] protected GameObject weaponAttachmentBone = null;
    [SerializeField] protected GameObject groundReferenceObject = null;
    [SerializeField] private float maxHealth = 100.0f;
    [SerializeField] private float health = 100.0f;
    [SerializeField] private bool alive = true;

    [HideInInspector] public CircleCollider2D visionCollider;

    [HideInInspector] public InventoryManager inventoryManager;

    private HealthbarBehaviour healthbarBehaviour = null;
    private GameObject lastDealer = null;

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
        if (alive) GetAnimations().UpdateRotations();
    }

    protected new void Awake()
    {
        base.Awake();
        // Record healthbar
        if (healthbar) healthbarBehaviour = healthbar.GetComponent<HealthbarBehaviour>();
        AddVisionCollider();
    }

    // Handle collision with projectiles
    public void OnTriggerEnter2D(Collider2D other)
    {
        // Do nothing if its detection collision or projectile wall collision
        if (other.gameObject.layer == 8 || other.gameObject.layer == 10) return;
        ProjectileTrigger t = other.gameObject.GetComponent<ProjectileTrigger>();
        if (!t) return;
        ProjectileBehaviour b = t.behaviour;
        // Do nothing if hit yourself
        if (b.ownerID == ID) return;
        // Do nothing if from same faction
        if (b.ownerFaction == faction && faction != FactionAllegiance.berserk) return;
        // Do nothing if this was already registered
        if (other.gameObject == lastDealer) return;
        // Apply changes
        lastDealer = other.gameObject;
        if (aiControl) aiControl.NotifyTakingDamage();
        DealDamage(b.damage);
        Destroy(b.gameObject);
        if (GetAlive()) GetAnimations().PlayFlinch();
    }

    public float GetHealth() { return health; }

    public float GetMaxHealth() { return maxHealth; }

    public void DealDamage(float amount)
    {
        health -= amount;
        if (healthbarBehaviour) healthbarBehaviour.UpdateHealthbar(health, maxHealth);
        if (health <= 0) SetAlive(false);
    }

    public void SetAlive(bool alive)
    {
        this.alive = alive;
        GetAnimations().SetAlive(alive);
        if (!alive) HelpFunc.DisableColliders(transform);
        if (!alive) base.SetSpeed(0.0f);
        if (!alive && healthbarBehaviour) healthbarBehaviour.Enable(false);
    }

    public bool GetAlive() { return alive; }

    public new void SetSpeed(float speed)
    {
        if (!alive) return;
        base.SetSpeed(speed);
        GetAnimations().SetSpeed(speed);
    }

    public new void SetMoveVector(Vector2 moveVector)
    {
        if (!alive) return;
        base.SetMoveVector(moveVector);
        GetAnimations().SetMovementVector(moveVector.normalized);
    }

    public void SetFacingVector(Vector2 facingVector)
    {
        if (!alive) return;
        GetAnimations().SetFacingVector(facingVector);
    }

    public void SetAimingLocation(Vector2 aimingLocation)
    {
        if (!alive) return;
        GetAnimations().SetAimingLocation(aimingLocation);
    }

    public void SetAttackTarget(Vector2 location)
    {
        if (!alive) return;
        foreach (WeaponBehaviour w in GetWeapons())
        {
            w.target = location;
        }
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
            w.Use();
        }
    }

    public void NotifyDetectedEntity(Collider2D other)
    {
        if (aiControl) aiControl.AddEntityDetected(other.gameObject);
    }

    public void NotifyDetectedEntityLeft(Collider2D other)
    {
        if (aiControl) aiControl.RemoveEntityDetected(other.gameObject);
    }

    protected virtual CreatureAnimations GetAnimations() { return null; }

    protected virtual List<WeaponBehaviour> GetWeapons() { return null; }

    private void AddVisionCollider()
    {
        GameObject vision = new GameObject("VisionCollider");
        vision.transform.parent = transform;
        vision.transform.localPosition = Vector3.zero;
        vision.layer = 9;
        visionCollider = vision.AddComponent<CircleCollider2D>();
        visionCollider.offset = gameObject.GetComponent<Collider2D>().offset;
        visionCollider.radius = 0.1f;
    }

    public new CreatureData Save()
    {
        CreatureData data = new CreatureData(base.Save());
        data.faction = faction;
        data.aiData = aiControl ? aiControl.Save() : null;
        data.moveSpeed = moveSpeed;
        data.alive = alive;
        data.maxHealth = maxHealth;
        data.health = health;
        return data;
    }

    public void Load(CreatureData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        faction = data.faction;
        if (data.aiData != null) aiControl.Load(data.aiData);
        moveSpeed = data.moveSpeed;
        SetAlive(data.alive);
        maxHealth = data.maxHealth;
        health = data.health;
        if (healthbarBehaviour && alive) healthbarBehaviour.UpdateHealthbar(health, maxHealth);
    }

    public static GameObject Spawn(CreatureData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = EntityBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<CreatureBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(CreatureData data, Transform parent = null)
    {
        GameObject obj = EntityBehaviour.Spawn(data, parent);
        obj.GetComponent<CreatureBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class CreatureData : EntityData
{
    public CreatureData() { }

    public CreatureData(EntityData data)
    {
        this.ID = data.ID;
        this.prefabPath = data.prefabPath;
        this.location = data.location;
        this.rotation = data.rotation;
        this.scale = data.scale;
        this.velocity = data.velocity;
        this.speed = data.speed;
    }

    public FactionAllegiance faction;
    public CreatureAIData aiData;
    public float moveSpeed;
    public float maxHealth;
    public bool alive;
    public float health;
}