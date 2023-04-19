using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static ContentGenerator;
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
        NPCaggressive,   // NPCs that were triggered to be aggressive to player
        hostile,    // Always hostile creatures (example: wild dog), attack everything on sight except each other
        berserk,    // Attacks everything including in own faction
        enemy       // Enemy faction, attack player only but not NPCs etc.
    }
    public GameObject healthbar = null;
    public CreatureTier tier = CreatureTier.hostileEasy;
    public FactionAllegiance faction = FactionAllegiance.neutral;
    public CreatureAI aiControl = null;

    public float moveSpeed = 0.0f;
    public List<GameObject> weaponAttachmentBones = new List<GameObject>();
    public GameObject groundReferenceObject = null;
    [SerializeField] private float maxHealth = 100.0f;
    [SerializeField] private float health = 100.0f;
    private bool alive = true;
    private List<ItemData> inventory = new List<ItemData>();

    [HideInInspector] public CircleCollider2D visionCollider;
    [HideInInspector] public static int inventoryLimit = 40;
    [HideInInspector] public List<ItemData> loot;

    private HealthbarBehaviour healthbarBehaviour = null;
    private int lastHit = -1;
    private int lastFrameHit = -1;

    // Populate this list to have AI use it
    protected List<WeaponBehaviour> AIweapons = new List<WeaponBehaviour>();
    protected List<WeaponData> loadOnWeaponSpawn = null;

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
        if (alive) GetAnimations().UpdateRotations();
    }

    protected new void Awake()
    {
        base.Awake();
        // Get healthbar
        if (healthbar) healthbarBehaviour = healthbar.GetComponent<HealthbarBehaviour>();
        AddVisionCollider();
    }

    protected void OnEnable()
    {
        MethodInfo method = GetAnimations().GetType().GetMethod("UpdateAnimators");
        method.Invoke(GetAnimations(), null);
    }

    // Handle collision with projectiles
    public void OnTriggerEnter2D(Collider2D other)
    {
        // Do nothing if its a collision with something else than projectile
        if (other.gameObject.layer != 6) return;
        ProjectileTrigger t = other.gameObject.GetComponent<ProjectileTrigger>();
        if (!t) return;
        ProjectileBehaviour b = t.behaviour;
        // Do nothing if hit yourself
        if (b.ownerID == ID) return;
        // Do nothing if from same faction
        if (b.ownerFaction == faction && faction != FactionAllegiance.berserk) return;
        RunProjectileHitActions(b);
    }

    public float GetHealth() { return health; }

    public void SetHealth(float value)
    {
        health = value;
        if (healthbarBehaviour) healthbarBehaviour.UpdateHealthbar(GetHealth(), GetMaxHealth());
        if (health <= 0) SetAlive(false);
    }

    public float GetMaxHealth() { return maxHealth; }

    public void SetMaxHealth(float value)
    {
        maxHealth = value;
        if (healthbarBehaviour) healthbarBehaviour.UpdateHealthbar(GetHealth(), GetMaxHealth());
        if (GetHealth() <= 0) SetAlive(false);
    }

    public void SetHealthbar(HealthbarBehaviour behaviour)
    {
        healthbarBehaviour = behaviour;
        healthbarBehaviour.UpdateHealthbar(health, maxHealth);
    }

    public void DealDamage(float amount)
    {
        SetHealth(GetHealth() - amount);
        SpawnFloatingText(Color.red, "-" + amount, 0.35f);
        if (GetAlive()) GetAnimations().PlayFlinch();
    }

    public void Heal(float amount)
    {
        float newHealth = Mathf.Min(GetHealth() + amount, GetMaxHealth());
        SetHealth(newHealth);
        SpawnFloatingText(Color.green, "+" + amount, 0.35f);
    }

    public List<ItemData> GetInventory() { return inventory; }

    public void SetInventory(List<ItemData> items)
    {
        inventory = items;
    }

    public void GiveItem(ItemData item)
    {
        // Spawn pickup text
        SpawnFloatingText(Color.green, "Item picked up", 0.5f);
        // Check if item is stackable
        if (item is AmmoData)
        {
            int amountToStack = ((AmmoData)item).quantity;
            // Check if we can stack on any existing ammo items
            foreach (ItemData inv in inventory)
            {
                // If stacked all, finish
                if (amountToStack <= 0) break;
                // If not stackable continue
                if (inv is not AmmoData) continue;
                bool ammoSameType = ((AmmoData)item).link == ((AmmoData)inv).link;
                bool canStack = ((AmmoData)inv).maxStack > ((AmmoData)inv).quantity;
                if (ammoSameType && canStack)
                {
                    // Stack item
                    int amountStacked = Mathf.Min(((AmmoData)inv).maxStack - ((AmmoData)inv).quantity, amountToStack);
                    ((AmmoData)inv).quantity += amountStacked;
                    amountToStack -= amountStacked;
                }
            }
            // Set final quantity
            ((AmmoData)item).quantity = amountToStack;
            // Only add if item not empty
            if (((AmmoData)item).quantity > 0) inventory.Add(item);
        }
        else
        {
            // Add to creature's inventory by default
            inventory.Add(item);
        }
    }

    public void SetAlive(bool alive)
    {
        this.alive = alive;
        GetAnimations().SetAlive(alive);
        if (!alive) RunDeathActions();
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

    public bool AnyWeaponOnTarget(float allowance)
    {
        if (!alive) return false;
        foreach (WeaponBehaviour w in GetWeapons()) if (w.IsOnTarget(allowance)) return true;
        return false;
    }

    public bool Attack()
    {
        if (!alive) return false;
        foreach (WeaponBehaviour w in GetWeapons())
        {
            w.Use();
        }
        if (GetWeapons().Count > 0) return true;
        return false;
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

    protected virtual List<WeaponBehaviour> GetWeapons() { return AIweapons; }

    public void SpawnAIWeapon(GameObject bone, string prefabPath)
    {
        Vector3 position = bone.transform.position;
        Quaternion rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        GameObject weapon = WeaponBehaviour.Spawn(prefabPath, position, rotation, bone.transform);
        weapon.transform.localRotation = rotation;
        WeaponBehaviour weaponBehaviour = weapon.GetComponent<WeaponBehaviour>();
        weaponBehaviour.ownerID = ID;
        weaponBehaviour.ownerFaction = faction;
        weaponBehaviour.groundReferenceObject = groundReferenceObject;
        weaponBehaviour.dontSave = true;
        AIweapons.Add(weaponBehaviour);
    }

    private void RunProjectileHitActions(ProjectileBehaviour projectile)
    {
        // Do nothing if this was already registered
        if (lastFrameHit == Time.frameCount) return;
        lastFrameHit = Time.frameCount;
        if (projectile.gameObject.GetInstanceID() == lastHit) return;
        // Lock this projectile from triggering entity again
        lastHit = projectile.gameObject.GetInstanceID();
        projectile.RunEffect(projectile, this);
    }

    private void RunDeathActions()
    {
        base.SetSpeed(0.0f);
        StartCoroutine(DestroyInTime(5));
        foreach (ItemData item in loot)
        {
            item.pickable = true;
            GameObject obj = ItemBehaviour.FlexibleSpawn(item);
            obj.transform.position = groundReferenceObject.transform.position;
        }
    }

    private IEnumerator DestroyInTime(float time)
    {
        yield return new WaitForSeconds(time);
        if (this is not PlayerBehaviour) HelpFunc.DisableColliders(transform);
        Destroy(gameObject);
    }

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

    private List<WeaponData> SaveAIWeapons()
    {
        List<WeaponData> data = new List<WeaponData>();
        foreach (WeaponBehaviour weapon in AIweapons) data.Add(weapon.Save());
        return data;
    }

    protected void LoadAIWeapons(List<WeaponData> data)
    {
        for (int i = 0; i < data.Count; i++) AIweapons[i].Load(data[i], false);
    }

    public new CreatureData Save()
    {
        CreatureData data = new CreatureData(base.Save());
        data.tier = tier;
        data.faction = faction;
        data.aiData = aiControl ? aiControl.Save() : null;
        data.moveSpeed = moveSpeed;
        data.alive = GetAlive();
        data.maxHealth = GetMaxHealth();
        data.health = GetHealth();
        data.inventory = inventory;
        data.AIweaponsData = SaveAIWeapons();
        return data;
    }

    public void Load(CreatureData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        tier = data.tier;
        faction = data.faction;
        if (data.aiData != null) aiControl.Load(data.aiData);
        moveSpeed = data.moveSpeed;
        SetAlive(data.alive);
        SetMaxHealth(data.maxHealth);
        SetHealth(data.health);
        inventory = data.inventory;
        loadOnWeaponSpawn = data.AIweaponsData;
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
        ID = data.ID;
        prefabPath = data.prefabPath;
        location = data.location;
        rotation = data.rotation;
        scale = data.scale;
        velocity = data.velocity;
        speed = data.speed;
        active = data.active;
    }

    public FactionAllegiance faction;
    public CreatureTier tier;
    public CreatureAIData aiData;
    public float moveSpeed;
    public float maxHealth;
    public bool alive;
    public float health;
    public List<ItemData> inventory;
    public List<WeaponData> AIweaponsData;
}