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

    public static new string PREFAB_PATH = "Prefabs/Creatures/Humanoid";

    [SerializeField] private GameObject weaponAttachmentBone;
    [SerializeField] private Vector2 weaponAttachmentOffset;
    [SerializeField] private int itemActiveSortLayer;

    [HideInInspector] public HumanoidAnimations animations;

    public static string[] BODYPARTS = new string[] { "Pelvis", "Torso", "Head", "Arm_Up_R", "Arm_Low_R",
    "Hand_R", "Arm_Up_L", "Arm_Low_L", "Hand_L", "Leg_Up_R", "Leg_Low_R", "Foot_R", "Leg_Up_L", "Leg_Low_L", "Foot_L" };

    private GameObject itemActive = null;
    private bool hasWeapon = false;

    new protected void Awake()
    {
        base.Awake();
        Animator bodyAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Body").GetComponent<Animator>();
        Animator armsAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Arms").GetComponent<Animator>();
        Animator legsAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Legs").GetComponent<Animator>();
        animations = new HumanoidAnimations(transform, new List<Animator>() { bodyAnimator, armsAnimator, legsAnimator }, BODYPARTS, "Hand_R_Parent");
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

    // Spawns an item in selected bone for character. Null can be provided to indicate that no items should be selected.
    public void SetItemActive(ObjectData item)
    {
        // Save to inventory before destroying
        Destroy(itemActive);
        hasWeapon = false;
        itemActive = null;
        if (item == null) return;
        Vector3 position = weaponAttachmentBone.transform.position + (Vector3)weaponAttachmentOffset;
        Quaternion rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        // Spawn the item in hand/bone
        GameObject obj = ObjectBehaviour.Spawn(item.prefabPath, position, rotation, weaponAttachmentBone.transform);
        // Load item's details but not its transform
        if (item.GetType() == typeof(WeaponData))
        {
            obj.GetComponent<WeaponBehaviour>().Load((WeaponData)item, true);
            hasWeapon = true;
        }
        else obj.GetComponent<ObjectBehaviour>().Load(item);
        obj.GetComponent<SpriteRenderer>().sortingOrder = itemActiveSortLayer;
        obj.GetComponent<ObjectBehaviour>().ownerID = ID;
        itemActive = obj;

    }

    public void ShootActiveWeaponOnce(Vector2 target)
    {

        if (!hasWeapon || !itemActive) return;
        itemActive.GetComponent<WeaponBehaviour>().target = target;
        itemActive.GetComponent<WeaponBehaviour>().Use();
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

    public static GameObject Spawn(HumanoidData data)
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>(PREFAB_PATH));
        obj.GetComponent<HumanoidBehaviour>().Load(data);
        return obj;
    }

    new public HumanoidData Save()
    {
        HumanoidData data = new HumanoidData(base.Save());
        data.bodypartData = SaveBodypartData();
        data.animationData = animations.Save();
        if (itemActive != null)
        {
            if (hasWeapon) data.itemActive = itemActive.GetComponent<WeaponBehaviour>().Save();
            else data.itemActive = itemActive.GetComponent<ObjectBehaviour>().Save();
        }
        else data.itemActive = null;
        return data;
    }

    public void Load(HumanoidData data)
    {
        base.Load(data);
        SetAlive(GetAlive());
        LoadBodyPartData(data.bodypartData);
        animations.Load(data.animationData);
        SetItemActive(data.itemActive);
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

    public ObjectData itemActive;
    public List<string> bodypartData;
    public HumanoidAnimationData animationData;
}

