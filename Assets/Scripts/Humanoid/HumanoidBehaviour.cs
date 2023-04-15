using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.U2D.Animation;
using static HumanoidAnimations;
using ColorUtility = UnityEngine.ColorUtility;

public class HumanoidBehaviour : CreatureBehaviour, Saveable<HumanoidData>, Spawnable<HumanoidData>
{
    [SerializeField] private Vector2 weaponAttachmentOffset;
    [SerializeField] private int itemActiveSortLayer;

    [HideInInspector] public HumanoidAnimations animations;
    public float backwardSpeedMultiplier = 0.5f;

    public static string[] BODYPARTS = new string[] { "Face", "Head", "Torso", "Pelvis", "Arm_Up_R", "Arm_Low_R",
    "Hand_R", "Arm_Up_L", "Arm_Low_L", "Hand_L", "Leg_Up_R", "Leg_Low_R", "Foot_R", "Leg_Up_L", "Leg_Low_L", "Foot_L", };
    public static String[] BODYPART_LABELS_INDEX = new string[] { "", "_0", "_2", "_5", "_1", "_4",
        "_7", "_3", "_6", "_8", "_10", "_9", "_13", "_11", "_12", "_14" };

    protected ItemBehaviour activeItemBehaviour = null;

    new protected void Awake()
    {
        base.Awake();
        Animator bodyAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Body").GetComponent<Animator>();
        Animator armsAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Arms").GetComponent<Animator>();
        Animator legsAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Legs").GetComponent<Animator>();
        animations = new HumanoidAnimations(transform, new List<Animator>() { bodyAnimator, armsAnimator, legsAnimator }, BODYPARTS, weaponAttachmentBone);
    }

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
    }

    protected override CreatureAnimations GetAnimations()
    {
        return animations;
    }

    /* Spawns an item in selected bone for character. Null can be provided to indicate that no items should be selected.
     * Returns data of the item that is to be unequipped.
     */
    public ItemData SetItemActive(ItemData item)
    {
        // We have to destroy current item
        // Save item before destroying
        ItemData data = null;
        if (activeItemBehaviour != null)
        {
            MethodInfo saveMethod = activeItemBehaviour.GetType().GetMethod("Save");
            data = (ItemData)saveMethod.Invoke(activeItemBehaviour, null);
            // Destroy item object
            Destroy(activeItemBehaviour.gameObject);
            activeItemBehaviour = null;
        }
        animations.aimingReferenceBone = weaponAttachmentBone;
        animations.SetStateHands(handsState.empty);
        animations.ResetJoints();
        // Finish here if no new item provided
        if (item == null) return data;

        // Spawn new item in hand/bone
        // We have to specify extra details so spawning process is more manual than usual here
        Vector3 position = weaponAttachmentBone.transform.position + (Vector3)weaponAttachmentOffset;
        Quaternion rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        GameObject obj = ItemBehaviour.Spawn(item.prefabPath, position, rotation, weaponAttachmentBone.transform);
        // Load item's details but not its transform
        if (item.GetType() == typeof(WeaponData))
        {
            WeaponBehaviour behaviour = obj.GetComponent<WeaponBehaviour>();
            behaviour.Load((WeaponData)item, false);
            // Transfer references and set animation
            behaviour.groundReferenceObject = groundReferenceObject;
            animations.aimingReferenceBone = HelpFunc.RecursiveFindChild(obj, "Attachpoint");
            animations.SetStateHands(behaviour.animationType);
        }
        else obj.GetComponent<ItemBehaviour>().Load(item);
        obj.GetComponentInChildren<SpriteRenderer>().sortingOrder = itemActiveSortLayer;
        Rigidbody2D objRB = obj.GetComponentInChildren<Rigidbody2D>();
        Destroy(objRB);
        ItemBehaviour itemB = obj.GetComponent<ItemBehaviour>();
        itemB.ownerID = ID;
        itemB.ownerFaction = faction;
        activeItemBehaviour = itemB;

        return data;
    }

    protected override List<WeaponBehaviour> GetWeapons()
    {
        List<WeaponBehaviour> weapons = new List<WeaponBehaviour>();
        if (activeItemBehaviour is WeaponBehaviour) weapons.Add((WeaponBehaviour)activeItemBehaviour);
        return weapons;
    }

    protected void SetBodypart(int labelIndex, int setChoice, string colorRGBA)
    {
        string partName = BODYPARTS[labelIndex];
        string category = BODYPARTS[labelIndex];
        string label = "Body" + setChoice + BODYPART_LABELS_INDEX[labelIndex];
        SpriteResolver spR = HelpFunc.RecursiveFindChild(this.gameObject, partName).GetComponent<SpriteResolver>();
        SpriteRenderer spRD = HelpFunc.RecursiveFindChild(this.gameObject, partName).GetComponent<SpriteRenderer>();
        spR.SetCategoryAndLabel(category, label);
        Color color;
        ColorUtility.TryParseHtmlString("#" + colorRGBA, out color);
        spRD.color = color;
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

    new public HumanoidData Save()
    {
        HumanoidData data = new HumanoidData(base.Save());
        data.bodypartData = SaveBodypartData();
        data.animationData = animations.Save();
        if (activeItemBehaviour)
        {
            MethodInfo saveMethod = activeItemBehaviour.GetType().GetMethod("Save");
            data.itemActive = (ItemData)saveMethod.Invoke(activeItemBehaviour, null);
        }
        else data.itemActive = null;
        return data;
    }

    public void Load(HumanoidData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        SetAlive(GetAlive());
        LoadBodyPartData(data.bodypartData);
        animations.Load(data.animationData);
        SetItemActive(data.itemActive);
    }

    public static GameObject Spawn(HumanoidData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = CreatureBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<HumanoidBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(HumanoidData data, Transform parent = null)
    {
        GameObject obj = CreatureBehaviour.Spawn(data, parent);
        obj.GetComponent<HumanoidBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class HumanoidData : CreatureData
{
    public HumanoidData() { }

    public HumanoidData(CreatureData data) : base(data)
    {
        faction = data.faction;
        aiData = data.aiData;
        moveSpeed = data.moveSpeed;
        alive = data.alive;
        maxHealth = data.maxHealth;
        health = data.health;
        inventory = data.inventory;
    }

    public ItemData itemActive;
    public List<string> bodypartData;
    public HumanoidAnimationData animationData;
}

