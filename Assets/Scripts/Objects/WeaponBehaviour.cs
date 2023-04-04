using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static WeaponBehaviour;

public class WeaponBehaviour : ObjectBehaviour
{
    public Vector2 target = Vector2.zero;   // Targeting location
    public GameObject projectilePrefab;

    public int maxAmmo;
    public int currAmmo;
    public float cooldown;

    public ulong guidanceTargetID = 0;
    public GameObject guidanceTarget = null;

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
        if (currAmmo <= 0) return;
        if (cooldownCurrent > 0.0f) return;
        // Acquire guidance target
        if (guidanceTargetID != 0) guidanceTarget = HelpFunc.FindGameObjectByBehaviourID(guidanceTargetID);
        // Get world-space direction for the bullet
        //Vector2 direction = transform.lossyScale.x * transform.right;
        Vector2 direction = target - (Vector2)transform.position;
        direction.Normalize();
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        GameObject proj = Instantiate(projectilePrefab, projectileAttachment.transform.position, rotation);
        proj.GetComponent<SpriteRenderer>().sortingOrder = GlobalControl.projectileSortLayer;
        // Transfer properties
        proj.GetComponent<ProjectileBehaviour>().ownerID = ownerID;
        proj.GetComponent<ProjectileBehaviour>().guidanceTargetID = guidanceTargetID;
        proj.GetComponent<ProjectileBehaviour>().guidanceTarget = guidanceTarget;
        // Apply speed and direction
        proj.GetComponent<ProjectileBehaviour>().SetVelocity(direction);
        proj.GetComponent<ProjectileBehaviour>().SetSpeed(proj.GetComponent<ProjectileBehaviour>().speedInitial);

        // Ammo, cooldown and animation
        currAmmo--;
        cooldownCurrent = cooldown;
        if (animator) animator.Play("Shoot");

    }

    public void Reload()
    {
        currAmmo = maxAmmo;
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