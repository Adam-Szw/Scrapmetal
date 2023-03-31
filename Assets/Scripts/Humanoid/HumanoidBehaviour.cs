using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D.Animation;
using UnityEngine.UIElements;
using static HumanoidAnimations;
using ColorUtility = UnityEngine.ColorUtility;

public class HumanoidBehaviour : CreatureBehaviour
{

    public static string PREFAB_PATH = "Prefabs/Humanoid";

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
        Animator bodyAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Body").GetComponent<Animator>();
        Animator armsAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Arms").GetComponent<Animator>();
        Animator legsAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Legs").GetComponent<Animator>();
        animations = new HumanoidAnimations(transform, new List<Animator>() { bodyAnimator, armsAnimator, legsAnimator }, new List<string>(BODYPARTS));
    }

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
    }

    protected override void FlinchFallback()
    {
        if (GetAlive()) animations.PlayFlinch();
    }

    protected override void DeathFallback()
    {
        animations.SetAlive(GetAlive());
    }

    protected override void AnimationUpdateFallback()
    {
        animations.UpdateRotations();
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
        weaponActive.GetComponent<WeaponBehaviour>().ownerID = this.gameObject.GetInstanceID();

    }

    public void ShootActiveWeaponOnce(Vector2 target)
    {
        if (!weaponActive) return;
        weaponActive.GetComponent<WeaponBehaviour>().target = target;
        weaponActive.GetComponent<WeaponBehaviour>().ShootOnce();
    }

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
        SetAlive(GetAlive());

        // rework this
        if (data.weaponActive != null)
        {
            weaponActive = Instantiate(Resources.Load<GameObject>(WeaponBehaviour.PREFAB_PATH),
                weaponAttachmentBone.transform.position + (Vector3)weaponAttachmentOffset,
                new Quaternion(0.0f, 0.0f, 0.0f, 0.0f), weaponAttachmentBone.transform);
            weaponActive.GetComponent<WeaponBehaviour>().Load(data.weaponActive);
            weaponActive.GetComponent<SpriteRenderer>().sortingOrder = weaponSortLayer;
        }
        LoadBodyPartData(data.bodypartData);
        animations.Load(data.animationData);
    }

    public static void SpawnEntity(HumanoidData data)
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>(HumanoidBehaviour.PREFAB_PATH));
        obj.GetComponent<HumanoidBehaviour>().Load(data);
    }

}

[Serializable]
public class HumanoidData : CreatureData
{
    public HumanoidData() { }

    public HumanoidData(CreatureData data) : base(data)
    {
        this.alive = data.alive;
        this.health = data.health;
    }

    public WeaponData weaponActive;
    public List<string> bodypartData;
    public HumanoidAnimationData animationData;
}

