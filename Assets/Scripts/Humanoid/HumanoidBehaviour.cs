using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class HumanoidBehaviour : MonoBehaviour
{
    [SerializeField] public HumanoidAnimations animations;
    [SerializeField] GameObject weaponAttachmentBone;
    [SerializeField] Vector2 weaponAttachmentOffset;
    [SerializeField] int weaponSortLayer;

    //HP, death, inventory etc will go to this class
    public bool alive = true;

    public float health = 100.0f;

    private GameObject weaponActive = null;

    private void Start()
    {
        // this can be moved to scene script
        Physics2D.IgnoreLayerCollision(0, 6);
    }

    void Update()
    {
        // todo - make this a state machine not update for performance
        animations.alive = alive;
    }

    // We are counting on projectile collision to delegate to this method
    public void OnTriggerEnter2D(Collider2D other)
    {
        // If hit by a bullet
        if (!(other.gameObject.GetComponent<BulletBehaviour>() == null))
        {
            BulletBehaviour bulletBehaviour = other.gameObject.GetComponent<BulletBehaviour>();
            // Do nothing if hit yourself
            if (bulletBehaviour.owner == gameObject) return;
            health -= bulletBehaviour.damage;
            if (health <= 0) alive = false;
            Destroy(other.gameObject);
            if (alive) animations.PlayFlinch();
            if (!alive) DisableColliders(transform);
        }
    }

    public void SetWeaponActive(GameObject weapon)
    {
        if(weaponActive != null)
        {
            Destroy(weaponActive);
            weaponActive = null;
        }
        weaponActive = Instantiate(weapon, weaponAttachmentBone.transform.position + (Vector3) weaponAttachmentOffset,
            new Quaternion(0.0f, 0.0f, 0.0f, 0.0f), weaponAttachmentBone.transform);
        weaponActive.GetComponent<SpriteRenderer>().sortingOrder = weaponSortLayer;
        weaponActive.GetComponent<WeaponBehaviour>().owner = gameObject;

    }

    public void ShootActiveWeaponOnce(Vector2 target)
    {
        weaponActive.GetComponent<WeaponBehaviour>().target = target;
        weaponActive.GetComponent<WeaponBehaviour>().ShootOnce();
    }

    public void ShootActiveWeaponOn(Vector2 target)
    {
        weaponActive.GetComponent<WeaponBehaviour>().target = target;
        weaponActive.GetComponent<WeaponBehaviour>().firing = true;
    }

    public void ShootActiveWeaponOff(Vector2 target)
    {
        weaponActive.GetComponent<WeaponBehaviour>().target = target;
        weaponActive.GetComponent<WeaponBehaviour>().firing = false;
    }

    // Recursively disables all colliders in the object
    private void DisableColliders(Transform parent)
    {
        CapsuleCollider2D capsuleCollider = parent.GetComponent<CapsuleCollider2D>();
        if (capsuleCollider != null) capsuleCollider.enabled = false;
        BoxCollider2D boxCollider = parent.GetComponent<BoxCollider2D>();
        if (boxCollider != null) boxCollider.enabled = false;
        foreach (Transform child in parent)
        {
            capsuleCollider = child.GetComponent<CapsuleCollider2D>();
            if (capsuleCollider != null) capsuleCollider.enabled = false;
            boxCollider = child.GetComponent<BoxCollider2D>();
            if (boxCollider != null) boxCollider.enabled = false;
            DisableColliders(child);
        }
    }

}
