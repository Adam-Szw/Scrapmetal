using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static AmmoBehaviour;
using static HumanoidAnimations;
using static WeaponBehaviour;
using Random = UnityEngine.Random;

public class WeaponBehaviour : ItemBehaviour, Saveable<WeaponData>, Spawnable<WeaponData>
{
    public GameObject projectilePrefab;

    public int maxAmmo = 0;
    public int currAmmo = 0;
    public float cooldown = 0;
    public float reloadCooldown = 0;
    public float spread = 0;
    public float snapMaxAngle = 0;
    public handsState animationType = handsState.empty;    // Used only by humanoid users
    public AmmoLink ammoLink = AmmoLink.empty;

    [HideInInspector] public Vector2 target = Vector2.zero;   // Targeting location
    [HideInInspector] public ulong guidanceTargetID = 0;
    [HideInInspector] public GameObject guidanceTarget = null;
    [HideInInspector] public GameObject groundReferenceObject = null;

    private float cooldownCurrent = 0.0f;
    private Animator animator;
    private GameObject projectileAttachment;

    protected new void Awake()
    {
        base.Awake();
        projectileAttachment = HelpFunc.RecursiveFindChild(this.gameObject, "Attachpoint");
        animator = GetComponentInChildren<Animator>();
    }

    protected new void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;

        // Calculate cooldown
        cooldownCurrent = Mathf.Max(0.0f, cooldownCurrent - Time.deltaTime);
    }

    public override void Use()
    {
        print("use");
        AcquireTargetLocation();

        // Ammo and cooldown checks
        if (currAmmo <= 0) return;
        if (cooldownCurrent > 0.0f) return;

        // Spawn projectile
        float shootAngle = GetSnapAngle();
        GameObject proj = Instantiate(projectilePrefab, projectileAttachment.transform.position, Quaternion.Euler(Vector3.zero));

        // Transfer properties
        ProjectileBehaviour projBehaviour = proj.GetComponent<ProjectileBehaviour>();
        projBehaviour.ownerID = ownerID;
        projBehaviour.guidanceTargetID = guidanceTargetID;
        projBehaviour.guidanceTarget = guidanceTarget;
        projBehaviour.ownerFaction = ownerFaction;
        projBehaviour.CreateStructureCollider(groundReferenceObject);
        projBehaviour.RotateSprite(shootAngle);

        // Ammo, cooldown and animation
        currAmmo--;
        cooldownCurrent = cooldown;
        if (animator) animator.Play("Shoot");

    }

    public void Reload()
    {
        currAmmo = maxAmmo;
    }

    public bool IsOnTarget(float allowance)
    {
        float angle = GetSnapAngle();
        Vector2 targetVec = target - (Vector2)projectileAttachment.transform.position;
        float desiredAngle = HelpFunc.Vec2ToAngle(targetVec);
        desiredAngle = Mathf.Round(desiredAngle * 10.0f) / 10.0f;
        float finalAngle = Mathf.Round(angle * 10.0f) / 10.0f;
        return Mathf.Abs(finalAngle - desiredAngle) < allowance;
    }

    private void AcquireTargetLocation()
    {
        // Save target ID if target already provided
        if (guidanceTarget != null && guidanceTargetID == 0)
        {
            EntityBehaviour b = guidanceTarget.GetComponent<EntityBehaviour>();
            guidanceTargetID = b.ID;
        }
        // Find target if only ID provided
        if (guidanceTargetID != 0) guidanceTarget = HelpFunc.FindEntityByID(guidanceTargetID);
        else guidanceTarget = null;
        if (guidanceTarget != null) target = guidanceTarget.transform.position;
    }

    private float GetSnapAngle()
    {
        // Calculate snap
        AcquireTargetLocation();
        Vector2 targetVec = target - (Vector2)projectileAttachment.transform.position;
        Vector2 weaponVec = HelpFunc.EulerToVec2(transform.eulerAngles.z);
        float targetAngle = HelpFunc.Vec2ToAngle(targetVec);
        float weaponAngle = HelpFunc.Vec2ToAngle(weaponVec);
        weaponAngle += transform.lossyScale.x > 0 ? 0.0f : -180.0f;
        weaponAngle = HelpFunc.NormalizeAngle(weaponAngle);
        float snapAngleMin = weaponAngle - snapMaxAngle;
        float snapAngleMax = HelpFunc.NormalizeAngle(weaponAngle + snapMaxAngle - snapAngleMin);
        float tempTargetAngle = HelpFunc.NormalizeAngle(targetAngle - snapAngleMin);
        float finalAngle = HelpFunc.NormalizeAngle(Mathf.Clamp(tempTargetAngle, 0, snapAngleMax) + snapAngleMin);

        // Calculate spread
        float angleSpreadMin = finalAngle - spread;
        float angleSpreadMax = finalAngle + spread;
        finalAngle = angleSpreadMin + Random.value * (angleSpreadMax - angleSpreadMin);

        return finalAngle;
    }

    public static WeaponData Produce(string prefabPath, ulong descriptionLink, string iconLink, int value, bool pickable,
        bool removeOnPick, float cooldown, float reloadCooldown, int maxAmmo, handsState animationType, AmmoLink link)
    {
        WeaponData data = new WeaponData(ItemBehaviour.Produce(prefabPath, descriptionLink, iconLink, value, pickable, removeOnPick));
        data.cooldown = cooldown;
        data.reloadCooldown = reloadCooldown;
        data.maxAmmo = maxAmmo;
        data.target = HelpFunc.VectorToArray(Vector2.zero);
        data.currAmmo = maxAmmo;
        data.cooldownCurrent = 0f;
        data.animationType = animationType;
        data.ammoLink = link;
        return data;
    }

    public new WeaponData Save()
    {
        WeaponData data = new WeaponData(base.Save());
        data.cooldown = cooldown;
        data.reloadCooldown = reloadCooldown;
        data.maxAmmo = maxAmmo;
        data.target = HelpFunc.VectorToArray(target);
        data.currAmmo = currAmmo;
        data.cooldownCurrent = cooldownCurrent;
        data.animationType = animationType;
        data.ammoLink = ammoLink;
        return data;
    }

    public void Load(WeaponData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        cooldown = data.cooldown;
        reloadCooldown = data.reloadCooldown;
        maxAmmo = data.maxAmmo;
        target = HelpFunc.DataToVec2(data.target);
        currAmmo = data.currAmmo;
        cooldownCurrent = data.cooldownCurrent;
        animationType = data.animationType;
        ammoLink = data.ammoLink;
    }

    public static GameObject Spawn(WeaponData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<WeaponBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(WeaponData data, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, parent);
        obj.GetComponent<WeaponBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class WeaponData : ItemData
{
    public WeaponData() { }

    public WeaponData(ItemData data) : base(data)
    {
        ownerID = data.ownerID;
        ownerFaction = data.ownerFaction;
        descriptionTextLinkID = data.descriptionTextLinkID;
        inventoryIconLink = data.inventoryIconLink;
        value = data.value;
        pickable = data.pickable;
        removeOnPick = data.removeOnPick;
    }

    public float damage;
    public float delay;
    public float cooldown;
    public float reloadCooldown = 0;
    public int maxAmmo;
    public float[] target;
    public bool firing;
    public int currAmmo;
    public float cooldownCurrent = 0.0f;
    public handsState animationType;
    public AmmoLink ammoLink;
}
