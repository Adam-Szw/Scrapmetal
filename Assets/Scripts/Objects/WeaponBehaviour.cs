using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static WeaponBehaviour;
using Random = UnityEngine.Random;

public class WeaponBehaviour : ObjectBehaviour, Saveable<WeaponData>, Spawnable<WeaponData>
{
    public Vector2 target = Vector2.zero;   // Targeting location
    public GameObject projectilePrefab;

    public int maxAmmo;
    public int currAmmo;
    public float cooldown;
    public float spread = 0;
    public float snapMaxAngle = 0;

    [HideInInspector] public ulong guidanceTargetID = 0;
    [HideInInspector] public GameObject guidanceTarget = null;

    private float cooldownCurrent = 0.0f;
    private Animator animator;
    private GameObject projectileAttachment;

    protected new void Awake()
    {
        base.Awake();
        projectileAttachment = HelpFunc.RecursiveFindChild(this.gameObject, "Attachpoint");
        animator = GetComponent<Animator>();
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
        AcquireTargetLocation();

        // Ammo and cooldown checks
        if (currAmmo <= 0) return;
        if (cooldownCurrent > 0.0f) return;

        // Spawn projectile
        float shootAngle = GetSnapAngle();
        GameObject proj = Instantiate(projectilePrefab, projectileAttachment.transform.position, Quaternion.Euler(0f, 0f, shootAngle));
        proj.GetComponent<SpriteRenderer>().sortingOrder = GlobalControl.projectileSortLayer;

        // Transfer properties
        ProjectileBehaviour projBehaviour = proj.GetComponent<ProjectileBehaviour>();
        projBehaviour.ownerID = ownerID;
        projBehaviour.guidanceTargetID = guidanceTargetID;
        projBehaviour.guidanceTarget = guidanceTarget;

        // Ammo, cooldown and animation
        currAmmo--;
        cooldownCurrent = cooldown;
        if (animator) animator.Play("Shoot");

    }

    public void Reload()
    {
        currAmmo = maxAmmo;
    }

    public bool IsOnTarget()
    {
        float angle = GetSnapAngle();
        Vector2 targetVec = target - (Vector2)projectileAttachment.transform.position;
        float desiredAngle = HelpFunc.Vec2ToAngle(targetVec);
        return Mathf.Round(angle * 10.0f) / 10.0f == Mathf.Round(desiredAngle * 10.0f) / 10.0f;
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
        if (guidanceTargetID != 0) guidanceTarget = HelpFunc.FindGameObjectByBehaviourID(guidanceTargetID);
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

    public new WeaponData Save()
    {
        WeaponData data = new WeaponData(base.Save());
        data.cooldown = this.cooldown;
        data.maxAmmo = this.maxAmmo;
        data.target = HelpFunc.VectorToArray(this.target);
        data.currAmmo = this.currAmmo;
        data.cooldownCurrent = this.cooldownCurrent;
        return data;
    }

    public void Load(WeaponData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        this.cooldown = data.cooldown;
        this.maxAmmo = data.maxAmmo;
        this.target = HelpFunc.DataToVec2(data.target);
        this.currAmmo = data.currAmmo;
        this.cooldownCurrent = data.cooldownCurrent;
    }

    public static GameObject Spawn(WeaponData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = ObjectBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<WeaponBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(WeaponData data, Transform parent = null)
    {
        GameObject obj = ObjectBehaviour.Spawn(data, parent);
        obj.GetComponent<WeaponBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class WeaponData : ObjectData
{
    public WeaponData() { }

    public WeaponData(ObjectData data) : base(data)
    {
        this.prefabPath = data.prefabPath;
        this.ownerID = data.ownerID;
    }

    public float damage;
    public float delay;
    public float cooldown;
    public int maxAmmo;
    public float[] target;
    public bool firing;
    public int currAmmo;
    public float cooldownCurrent = 0.0f;
}