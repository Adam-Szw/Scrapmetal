using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D.Animation;
using static CreatureLibrary;
using static HumanoidAnimations;
using ColorUtility = UnityEngine.ColorUtility;
using Random = UnityEngine.Random;

// Base class for humanoid-looking robots in the game
public class HumanoidBehaviour : CreatureBehaviour, Saveable<HumanoidData>, Spawnable<HumanoidData>
{
    [SerializeField] private int itemActiveSortLayer;       // Which layer active (equipped) item should be. This should be set to slid between character's bodypart layers
    [SerializeField] private bool randomizeParts = false;   // If true, the character's bodyparts will be randomized on spawn and saved (colour and sprite)

    public float backwardSpeedMultiplier = 0.5f;            // Humanoid characters are slowed down when moving away from facing

    [HideInInspector] public HumanoidAnimations animations;

    // Bone names
    public static string[] BODYPARTS = new string[] { "Face", "Head", "Torso", "Pelvis", "Arm_Up_R", "Arm_Low_R",
    "Hand_R", "Arm_Up_L", "Arm_Low_L", "Hand_L", "Leg_Up_R", "Leg_Low_R", "Foot_R", "Leg_Up_L", "Leg_Low_L", "Foot_L", };
    // Bone indices in sprite library
    public static string[] BODYPART_LABELS_INDEX = new string[] { "_F", "_0", "_2", "_5", "_1", "_4",
        "_7", "_3", "_6", "_8", "_10", "_9", "_13", "_11", "_12", "_14" };
    public static int BODYPART_NPC_INDEX_START = 2; // Not all bodyparts should be used in random generation
    public static int BODYPART_NPC_INDEX_END = 9;
    public static int FACE_NPC_INDEX_START = 2;     // Same but faces
    public static int FACE_NPC_INDEX_END = 6;

    // Humanoids can hold an item in their hands. This variable is a direct link to control it
    protected ItemBehaviour activeItem = null;

    private bool bodypartsGenerated = false;

    new protected void Awake()
    {
        base.Awake();
        // Find animators and setup animations object
        Animator bodyAnimator = HelpFunc.RecursiveFindChild(gameObject, "Body").GetComponent<Animator>();
        Animator armsAnimator = HelpFunc.RecursiveFindChild(gameObject, "Arms").GetComponent<Animator>();
        Animator legsAnimator = HelpFunc.RecursiveFindChild(gameObject, "Legs").GetComponent<Animator>();
        animations = new HumanoidAnimations(transform, new List<Animator>() { bodyAnimator, armsAnimator, legsAnimator }, BODYPARTS, weaponAttachmentBones[0]);
    }

    protected void Start()
    {
        // Randomize body if not done and needs to be done
        if (!bodypartsGenerated && randomizeParts) RandomizeBodyparts();
    }

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
    }

    protected override CreatureAnimations GetAnimations() { return animations; }

    /* Spawns an item in selected bone for character using data. Null can be provided to indicate that no items should be selected.
     * Currently held object will be destroyed. Animations will be set appropriately to item selected.
     */
    public void SetItemActive(ItemData item)
    {
        if (activeItem != null)
        {
            // Destroy item object
            Destroy(activeItem.gameObject);
            activeItem = null;
        }
        animations.aimingReferenceBone = weaponAttachmentBones[0];
        animations.SetStateHands(handsState.empty);
        animations.ResetJoints();
        // Finish here if no new item provided
        if (item == null) return;

        // We have to specify extra details so spawning process is more manual than usual here
        // Spawn new item in hand/bone
        Vector3 position = weaponAttachmentBones[0].transform.position;
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

        // Setup extra variables to make sure the item cant be grabbed from NPC hand and doesnt clip through body
        obj.GetComponentInChildren<SortingGroup>().sortingOrder = itemActiveSortLayer;
        Rigidbody2D objRB = obj.GetComponentInChildren<Rigidbody2D>();
        Destroy(objRB);
        Collider2D objCollider = obj.GetComponentInChildren<Collider2D>();
        Destroy(objCollider);

        // Transfer properties to the item - ownership and faction, from the character
        ItemBehaviour itemB = obj.GetComponent<ItemBehaviour>();
        itemB.ownerID = ID;
        itemB.ownerFaction = faction;
        activeItem = itemB;

        // Make sure the active item is not saved itself. Active item is loaded using SetItemActive rather than saved itself
        itemB.dontSave = true;
        return;
    }

    // Return data for currently held item or null if none are held
    public ItemData SaveItemActive()
    {
        ItemData data = null;
        if (activeItem != null)
        {
            MethodInfo saveMethod = activeItem.GetType().GetMethod("Save");
            data = (ItemData)saveMethod.Invoke(activeItem, null);
        }
        return data;
    }

    protected override List<WeaponBehaviour> GetWeapons()
    {
        List<WeaponBehaviour> weapons = new List<WeaponBehaviour>();
        if (activeItem is WeaponBehaviour) weapons.Add((WeaponBehaviour)activeItem);
        return weapons;
    }

    // Setup bodypart to use label and index from sprite library. Colors declared as four RGBA values in scale 0...1 with 1 being max saturation.
    protected void SetBodypart(int labelIndex, int setChoice, float[] colorVals = null)
    {
        string partName = BODYPARTS[labelIndex];
        string category = BODYPARTS[labelIndex];
        string label = "Body" + setChoice + BODYPART_LABELS_INDEX[labelIndex];
        SpriteResolver spR = HelpFunc.RecursiveFindChild(gameObject, partName).GetComponent<SpriteResolver>();
        SpriteRenderer spRD = HelpFunc.RecursiveFindChild(gameObject, partName).GetComponent<SpriteRenderer>();
        spR.SetCategoryAndLabel(category, label);
        Color color = Color.white;
        if (colorVals != null) color = new Color(colorVals[0], colorVals[1], colorVals[2], colorVals[3]);
        spRD.color = color;
    }

    // Returns a list of strings that encode which sprites are selected for this humanoid
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

    // Parses bodypart data saved using SaveBodypartData and sets up the bodyparts using it
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

    // Sets bodyparts to have random sprite and color preset from those available to NPCs
    private void RandomizeBodyparts()
    {
        // Randomize color scheme using presets
        BodyColorPreset preset = HUMANOID_BODY_COLOR_PRESETS[Random.Range(0, HUMANOID_BODY_COLOR_PRESETS.Count)];
        for (int i = 1; i < BODYPARTS.Length; i++)
        {
            // Randomize part
            int bodyChoice = Random.Range(BODYPART_NPC_INDEX_START, BODYPART_NPC_INDEX_END + 1);
            if (i == 1) SetBodypart(i, bodyChoice, preset.headColor);
            if (i >= 2 && i <= 3) SetBodypart(i, bodyChoice, preset.bodyColor);
            if (i >= 4 && i <= 9) SetBodypart(i, bodyChoice, preset.armsColor);
            if (i >= 10) SetBodypart(i, bodyChoice, preset.legsColor);
        }
        // Randomize face
        int faceChoice = Random.Range(FACE_NPC_INDEX_START, FACE_NPC_INDEX_END + 1);
        SetBodypart(0, faceChoice);
        bodypartsGenerated = true;
    }

    new public HumanoidData Save()
    {
        HumanoidData data = new HumanoidData(base.Save());
        data.bodypartData = SaveBodypartData();
        data.animationData = animations.Save();
        data.itemActive = SaveItemActive();
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
        loot = data.loot;
        AIweaponsData = data.AIweaponsData;
    }

    public ItemData itemActive;
    public List<string> bodypartData;
    public HumanoidAnimationData animationData;
    public bool randomizeParts;
    public bool bodypartsGenerated;

}

