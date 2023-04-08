using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static WeaponBehaviour;
using Random = UnityEngine.Random;

public class WeaponBehaviour : ObjectBehaviour
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

    private bool firing = false;
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

        // Update the animator
        if (animator) animator.SetBool("Firing", firing);

        // Calculate cooldown
        cooldownCurrent = Mathf.Max(0.0f, cooldownCurrent - Time.deltaTime);
    }

    public override void Use()
    {
        // Ammo and cooldown checks
        if (currAmmo <= 0) return;
        if (cooldownCurrent > 0.0f) return;

        // Acquire guidance target
        if (guidanceTargetID != 0) guidanceTarget = HelpFunc.FindGameObjectByBehaviourID(guidanceTargetID);
        else guidanceTarget = null;
        if (guidanceTarget) target = guidanceTarget.transform.position;

        // Spawn projectile
        GameObject proj = Instantiate(projectilePrefab, projectileAttachment.transform.position, GetFireAngle());
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
        float angle = GetFireAngle().eulerAngles.z;
        Vector2 targetVec = target - (Vector2)projectileAttachment.transform.position;
        float desiredAngle = HelpFunc.Vec2ToAngle(targetVec);
        return angle == desiredAngle;
    }

    private Quaternion GetFireAngle()
    {
        // Calculate snap
        Vector2 targetVec = target - (Vector2)projectileAttachment.transform.position;
        Vector2 weaponVec = HelpFunc.EulerToVec2(transform.eulerAngles.z);
        float targetAngle = HelpFunc.Vec2ToAngle(targetVec);
        float weaponAngle = HelpFunc.Vec2ToAngle(weaponVec);
        weaponAngle += transform.lossyScale.x > 0 ? 0.0f : -180.0f;
        weaponAngle = HelpFunc.NormalizeAngle(weaponAngle);
        float snapAngleMin = weaponAngle - snapMaxAngle;
        float snapAngleMax = weaponAngle + snapMaxAngle - snapAngleMin;
        float tempTargetAngle = targetAngle - snapAngleMin;
        float finalAngle = Mathf.Clamp(tempTargetAngle, 0, snapAngleMax) + snapAngleMin;

        // Calculate spread
        float angleSpreadMin = finalAngle - spread;
        float angleSpreadMax = finalAngle + spread;
        finalAngle = angleSpreadMin + Random.value * (angleSpreadMax - angleSpreadMin);

        return Quaternion.Euler(0f, 0f, finalAngle);
    }

    public new WeaponData Save()
    {
        WeaponData data = new WeaponData(base.Save());
        data.cooldown = this.cooldown;
        data.maxAmmo = this.maxAmmo;
        data.target = HelpFunc.VectorToArray(this.target);
        data.firing = this.firing;
        data.currAmmo = this.currAmmo;
        data.cooldownCurrent = this.cooldownCurrent;
        return data;
    }

    public void Load(WeaponData data, bool dontLoadBody = false)
    {
        base.Load(data, dontLoadBody);
        this.cooldown = data.cooldown;
        this.maxAmmo = data.maxAmmo;
        this.target = HelpFunc.DataToVec2(data.target);
        this.firing = data.firing;
        this.currAmmo = data.currAmmo;
        this.cooldownCurrent = data.cooldownCurrent;
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