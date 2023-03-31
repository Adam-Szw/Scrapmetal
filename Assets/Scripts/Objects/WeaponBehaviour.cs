using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static WeaponBehaviour;

public class WeaponBehaviour : ItemBehaviour
{
    public static new string PREFAB_PATH = "Prefabs/Weapon";

    public ProjectileType projectileType;
    public Vector2 target = Vector2.zero;   // Targeting location
    public GameObject targetLockOn;         // Used only in missiles

    public int maxAmmo;
    public int currAmmo;
    public float cooldown;

    private bool firing = false;
    private float cooldownCurrent = 0.0f;
    private Animator animator;
    private GameObject projectilePrefab;
    private GameObject projectileAttachment;

    public enum ProjectileType
    {
        melee, bullet, missile
    }

    protected new void Awake()
    {
        base.Awake();
        projectileAttachment = HelpFunc.RecursiveFindChild(this.gameObject, "Attachpoint");
        animator = GetComponent<Animator>();
        if(projectileType == ProjectileType.bullet) projectilePrefab = Resources.Load<GameObject>(ProjectileBehaviour.PREFAB_PATH);
    }

    protected new void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;

        // Update the animator
        animator.SetBool("Firing", firing);

        // Calculate cooldown
        cooldownCurrent = Mathf.Max(0.0f, cooldownCurrent - Time.deltaTime);
    }

    public void ShootOnce()
    {
        if (currAmmo <= 0) return;
        if (cooldownCurrent > 0.0f) return;
        // Get world-space direction for the bullet
        //Vector2 direction = transform.lossyScale.x * transform.right;
        Vector2 direction = target - (Vector2) transform.position;
        direction.Normalize();
        Quaternion rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        GameObject proj = Instantiate(projectilePrefab, projectileAttachment.transform.position, rotation);
        proj.GetComponent<SpriteRenderer>().sortingOrder = GlobalControl.projectileSortLayer;

        // Apply speed and direction based on type of projectile
        if(projectileType == ProjectileType.bullet)
        {
            proj.GetComponent<ProjectileBehaviour>().ownerID = this.gameObject.GetInstanceID();
            proj.GetComponent<ProjectileBehaviour>().SetVelocity(direction);
            proj.GetComponent<ProjectileBehaviour>().SetSpeed(proj.GetComponent<ProjectileBehaviour>().speedInitial);
        }

        // Ammo, cooldown and animation
        currAmmo--;
        cooldownCurrent = cooldown;
        animator.Play("Shoot");

    }

    public void Reload()
    {
        currAmmo = maxAmmo;
    }

    public WeaponData Save()
    {
        WeaponData data = new WeaponData();
        data.projectileType = this.projectileType;
        data.damage = this.damage;
        data.delay = this.delay ;
        data.cooldown = this.cooldown;
        data.maxAmmo = this.maxAmmo;
        data.target = HelpFunc.VectorToArray(this.target);
        data.firing = this.firing;
        data.currAmmo = this.currAmmo;
        data.cooldownCurrent = this.cooldownCurrent;
        return data;
    }

    public void Load(WeaponData data)
    {
        this.projectileType = data.projectileType;
        this.damage = data.damage;
        this.delay = data.delay;
        this.cooldown = data.cooldown;
        this.maxAmmo = data.maxAmmo;
        this.target = HelpFunc.DataToVec2(data.target);
        this.firing = data.firing;
        this.currAmmo = data.currAmmo;
        this.cooldownCurrent = data.cooldownCurrent;
    }
}

[Serializable]
public class WeaponData
{
    public ProjectileType projectileType;
    public float damage;
    public float delay;
    public float cooldown;
    public int maxAmmo;
    public float[] target;
    public bool firing;
    public int currAmmo;
    public float cooldownCurrent = 0.0f;
}