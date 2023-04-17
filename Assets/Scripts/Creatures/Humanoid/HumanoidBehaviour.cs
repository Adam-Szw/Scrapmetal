using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.U2D.Animation;
using static CreatureLibrary;
using static HumanoidAnimations;
using ColorUtility = UnityEngine.ColorUtility;
using Random = UnityEngine.Random;

public class HumanoidBehaviour : CreatureBehaviour, Saveable<HumanoidData>, Spawnable<HumanoidData>
{
    [SerializeField] private Vector2 weaponAttachmentOffset;
    [SerializeField] private int itemActiveSortLayer;
    [SerializeField] private bool randomizeParts = false;

    [HideInInspector] public HumanoidAnimations animations;
    public float backwardSpeedMultiplier = 0.5f;

    public static string[] BODYPARTS = new string[] { "Face", "Head", "Torso", "Pelvis", "Arm_Up_R", "Arm_Low_R",
    "Hand_R", "Arm_Up_L", "Arm_Low_L", "Hand_L", "Leg_Up_R", "Leg_Low_R", "Foot_R", "Leg_Up_L", "Leg_Low_L", "Foot_L", };
    public static string[] BODYPART_LABELS_INDEX = new string[] { "_F", "_0", "_2", "_5", "_1", "_4",
        "_7", "_3", "_6", "_8", "_10", "_9", "_13", "_11", "_12", "_14" };
    public static int BODYPART_NPC_INDEX_START = 2;
    public static int BODYPART_NPC_INDEX_END = 9;
    public static int FACE_NPC_INDEX_START = 2;
    public static int FACE_NPC_INDEX_END = 6;

    protected ItemBehaviour activeItemBehaviour = null;

    private bool bodypartsGenerated = false;

    new protected void Awake()
    {
        base.Awake();
        Animator bodyAnimator = HelpFunc.RecursiveFindChild(gameObject, "Body").GetComponent<Animator>();
        Animator armsAnimator = HelpFunc.RecursiveFindChild(gameObject, "Arms").GetComponent<Animator>();
        Animator legsAnimator = HelpFunc.RecursiveFindChild(gameObject, "Legs").GetComponent<Animator>();
        animations = new HumanoidAnimations(transform, new List<Animator>() { bodyAnimator, armsAnimator, legsAnimator }, BODYPARTS, weaponAttachmentBones[0]);
    }

    protected void Start()
    {
        if (!bodypartsGenerated && randomizeParts) RandomizeBodyparts();
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
        animations.aimingReferenceBone = weaponAttachmentBones[0];
        animations.SetStateHands(handsState.empty);
        animations.ResetJoints();
        // Finish here if no new item provided
        if (item == null) return data;

        // Spawn new item in hand/bone
        // We have to specify extra details so spawning process is more manual than usual here
        Vector3 position = weaponAttachmentBones[0].transform.position + (Vector3)weaponAttachmentOffset;
        Quaternion rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        GameObject obj = ItemBehaviour.Spawn(item.prefabPath, position, rotation, weaponAttachmentBones[0].transform);
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

    protected void SetBodypart(int labelIndex, int setChoice, string colorRGBA = "", float[] colorVals = null)
    {
        string partName = BODYPARTS[labelIndex];
        string category = BODYPARTS[labelIndex];
        string label = "Body" + setChoice + BODYPART_LABELS_INDEX[labelIndex];
        SpriteResolver spR = HelpFunc.RecursiveFindChild(gameObject, partName).GetComponent<SpriteResolver>();
        SpriteRenderer spRD = HelpFunc.RecursiveFindChild(gameObject, partName).GetComponent<SpriteRenderer>();
        spR.SetCategoryAndLabel(category, label);
        Color color = Color.white;
        if (colorVals != null) color = new Color(colorVals[0], colorVals[1], colorVals[2], colorVals[3]);
        else if (colorRGBA != "") ColorUtility.TryParseHtmlString("#" + colorRGBA, out color);
        spRD.color = color;
    }

    private List<string> SaveBodypartData()
    {
        List<string> data = new List<string>();
        foreach (string bodypart in BODYPARTS)
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

    private void RandomizeBodyparts()
    {
        // Randomize color scheme
        BodyColorPreset preset = HUMANOID_BODY_COLOR_PRESETS[Random.Range(0, HUMANOID_BODY_COLOR_PRESETS.Count)];
        for (int i = 1; i < BODYPARTS.Length; i++)
        {
            // Randomize part
            int bodyChoice = Random.Range(BODYPART_NPC_INDEX_START, BODYPART_NPC_INDEX_END + 1);
            if (i == 1) SetBodypart(i, bodyChoice, "", preset.headColor);
            if (i >= 2 && i <= 3) SetBodypart(i, bodyChoice, "", preset.bodyColor);
            if (i >= 4 && i <= 9) SetBodypart(i, bodyChoice, "", preset.armsColor);
            if (i >= 10) SetBodypart(i, bodyChoice, "", preset.legsColor);
        }
        // Randomize face
        int faceChoice = Random.Range(FACE_NPC_INDEX_START, FACE_NPC_INDEX_END + 1);
        SetBodypart(0, faceChoice, "");
        bodypartsGenerated = true;
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
        data.randomizeParts = randomizeParts;
        data.bodypartsGenerated = bodypartsGenerated;
        return data;
    }

    public void Load(HumanoidData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        SetAlive(GetAlive());
        LoadBodyPartData(data.bodypartData);
        animations.Load(data.animationData);
        SetItemActive(data.itemActive);
        randomizeParts = data.randomizeParts;
        bodypartsGenerated = data.bodypartsGenerated;
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
        tier = data.tier;
        aiData = data.aiData;
        moveSpeed = data.moveSpeed;
        alive = data.alive;
        maxHealth = data.maxHealth;
        health = data.health;
        inventory = data.inventory;
        AIweaponsData = data.AIweaponsData;
    }

    public ItemData itemActive;
    public List<string> bodypartData;
    public HumanoidAnimationData animationData;
    public bool randomizeParts;
    public bool bodypartsGenerated;

}

