using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D.Animation;
using static HumanoidAnimations;
using ColorUtility = UnityEngine.ColorUtility;

public class HumanoidBehaviour : EntityBehaviour
{
    [SerializeField] private GameObject weaponAttachmentBone;
    [SerializeField] private Vector2 weaponAttachmentOffset;
    [SerializeField] private int weaponSortLayer;

    private GameObject weaponActive = null;

    public HumanoidAnimations animations;

    public static string[] BODYPARTS = new string[] { "Pelvis", "Torso", "Head", "Arm_Up_R", "Arm_Low_R",
    "Hand_R", "Arm_Up_L", "Arm_Low_L", "Hand_L", "Leg_Up_R", "Leg_Low_R", "Foot_R", "Leg_Up_L", "Leg_Low_L", "Foot_L" };

    new protected void Awake()
    {
        base.Awake();
        Animator legsAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Legs").GetComponent<Animator>();
        Animator armsAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Arms").GetComponent<Animator>();
        Animator bodyAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Body").GetComponent<Animator>();
        animations = new HumanoidAnimations(this.gameObject, legsAnimator, armsAnimator, bodyAnimator, transform);
    }

    protected void Update()
    {
        if (GlobalControl.paused) return;
        if (alive) animations.UpdateRotations();
        UpdateRigidBody();
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        // If hit by a bullet
        if (!(other.gameObject.GetComponent<BulletBehaviour>() == null))
        {
            BulletBehaviour bulletBehaviour = other.gameObject.GetComponent<BulletBehaviour>();
            // Do nothing if hit yourself
            if (bulletBehaviour.ownerID == this.gameObject.GetInstanceID()) return;
            health -= bulletBehaviour.damage;
            if (health <= 0) SetAlive(false);
            Destroy(other.gameObject);
            if (alive) animations.PlayFlinch();
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
        if (!weaponActive) return;
        weaponActive.GetComponent<WeaponBehaviour>().target = target;
        weaponActive.GetComponent<WeaponBehaviour>().ShootOnce();
    }

    public void ShootActiveWeaponOn(Vector2 target)
    {
        if (!weaponActive) return;
        weaponActive.GetComponent<WeaponBehaviour>().target = target;
        weaponActive.GetComponent<WeaponBehaviour>().firing = true;
    }

    public void ShootActiveWeaponOff(Vector2 target)
    {
        if (!weaponActive) return;
        weaponActive.GetComponent<WeaponBehaviour>().target = target;
        weaponActive.GetComponent<WeaponBehaviour>().firing = false;
    }

    public void SetAlive(bool alive)
    {
        this.alive = alive;
        animations.SetAlive(alive);
        if (!alive) DisableColliders(transform);
        if (!alive) SetSpeed(0.0f);
    }

    public bool GetAlive() { return alive; }

    // Returns data from which this humanoid can be replicated
    new public HumanoidData Save()
    {
        HumanoidData data = new HumanoidData(base.Save());
        if (weaponActive) data.weaponActive = weaponActive.GetComponent<WeaponBehaviour>().Save();
        else data.weaponActive = null;
        data.bodypartData = SaveBodypartData();
        data.animationData = animations.Save();
        return data;
    }

    public void Load(HumanoidData data)
    {
        base.Load(data);
        // Refresh alive for humanoid script
        SetAlive(alive);

        // rework this
        if(data.weaponActive != null)
        {
            weaponActive = Instantiate(Resources.Load<GameObject>(GlobalControl.WEAPON_PATH),
                weaponAttachmentBone.transform.position + (Vector3)weaponAttachmentOffset,
                new Quaternion(0.0f, 0.0f, 0.0f, 0.0f), weaponAttachmentBone.transform);
            weaponActive.GetComponent<SpriteRenderer>().sortingOrder = weaponSortLayer;
            weaponActive.GetComponent<WeaponBehaviour>().owner = gameObject;
            weaponActive.GetComponent<WeaponBehaviour>().Load(data.weaponActive);
        }
        LoadBodyPartData(data.bodypartData);
        animations.Load(data.animationData);
    }

    // This stuff should be moved to its own class
    private List<string> SaveBodypartData()
    {
        List<string> data = new List<string>();
        foreach(string bodypart in BODYPARTS)
        {
            SpriteResolver spR = HelpFunc.RecursiveFindChild(this.gameObject, bodypart).GetComponent<SpriteResolver>();
            SpriteRenderer spRD = HelpFunc.RecursiveFindChild(this.gameObject, bodypart).GetComponent<SpriteRenderer>();
            data.Add(spR.GetCategory());
            data.Add(spR.GetLabel());
            data.Add(ColorUtility.ToHtmlStringRGBA(spRD.color));
        }
        return data;
    }

    private void LoadBodyPartData(List<string> data)
    {
        int i = 0;
        foreach (string bodypart in BODYPARTS)
        {
            SpriteResolver spR = HelpFunc.RecursiveFindChild(this.gameObject, bodypart).GetComponent<SpriteResolver>();
            SpriteRenderer spRD = HelpFunc.RecursiveFindChild(this.gameObject, bodypart).GetComponent<SpriteRenderer>();
            spR.SetCategoryAndLabel(data[i], data[i + 1]);
            Color color;
            ColorUtility.TryParseHtmlString("#" + data[i+2], out color);
            spRD.color = color;
            i += 3;
        }
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
