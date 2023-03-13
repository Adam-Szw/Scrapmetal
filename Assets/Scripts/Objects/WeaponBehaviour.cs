using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class WeaponBehaviour : MonoBehaviour
{
    [SerializeField] private ProjectileType projectileType;
    public float damage;
    public float delay;
    public float cooldown;
    public string projectileLink;              // prefab for the projectile
    public GameObject projectileAttachment;    // attachment bone of weapon where the bullet will be spawned
    public int projectileSortLayer;            // sorting layer index that will be given to new projectile
    public Animator animator;
    public string shootStateName;              // name of animation clip that will be played when weapon is fired
    public int maxAmmo;
    public int currAmmo;

    public GameObject owner;
    public Vector2 target = Vector2.zero;
    public bool firing = false;

    private float cooldownCurrent = 0.0f;

    enum ProjectileType
    {
        melee, bullet, missile
    }

    void Update()
    {
        if (GlobalControl.paused) return;

        // Update the animator
        animator.SetBool("Firing", firing);

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
        GameObject proj = Instantiate(Resources.Load<GameObject>(projectileLink), projectileAttachment.transform.position, rotation);
        proj.GetComponent<SpriteRenderer>().sortingOrder = projectileSortLayer;

        // Apply speed and direction based on type of projectile
        if(projectileType == ProjectileType.bullet)
        {
            proj.GetComponent<BulletBehaviour>().ownerID = this.gameObject.GetInstanceID();
            proj.GetComponent<BulletBehaviour>().SetVelocityVector(direction);
            proj.GetComponent<BulletBehaviour>().SetSpeed(proj.GetComponent<BulletBehaviour>().speedInitial);
            // Transfer weapon damage to the ammo
            proj.GetComponent<BulletBehaviour>().damage = damage;
        }

        // Ammo, cooldown and animation
        currAmmo--;
        cooldownCurrent = cooldown;
        animator.Play(shootStateName);

    }

    public void Reload()
    {
        currAmmo = maxAmmo;
    }

    public WeaponData Save()
    {
        WeaponData data = new WeaponData();
        return data;
    }

    public void Load(WeaponData data)
    {

    }
}
