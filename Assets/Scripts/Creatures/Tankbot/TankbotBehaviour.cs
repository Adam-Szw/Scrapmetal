using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

// This class is very similar to spiderbot - but separation is on purpose so in the future these 2 enemies can be more distinct
public class TankbotBehaviour : CreatureBehaviour, Saveable<TankbotData>, Spawnable<TankbotData>
{
    public TankbotAnimations animations;
    public GameObject weaponAttachmentSecondary;

    public static string[] BODYPARTS = new string[] { "Sensor", "Turret" };

    private List<WeaponBehaviour> weapons = new List<WeaponBehaviour>();

    new protected void Awake()
    {
        base.Awake();
        Animator bodyAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Body").GetComponent<Animator>();
        GameObject aimBone = HelpFunc.RecursiveFindChild(this.gameObject, "Turret_Parent");
        animations = new TankbotAnimations(transform, new List<Animator>() { bodyAnimator }, BODYPARTS, aimBone);
        animations.movementDeterminesFlip = true;
        Vector3 position = weaponAttachmentBone.transform.position;
        Quaternion rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        GameObject weapon = WeaponBehaviour.Spawn("Prefabs/Items/Weapons/RifleTankbot", position, rotation, weaponAttachmentBone.transform);
        WeaponBehaviour weaponBehaviour = weapon.GetComponent<WeaponBehaviour>();
        weaponBehaviour.ownerID = ID;
        weaponBehaviour.ownerFaction = faction;
        weaponBehaviour.groundReferenceObject = groundReferenceObject;
        weapons.Add(weaponBehaviour);
        position = weaponAttachmentSecondary.transform.position;
        weapon = WeaponBehaviour.Spawn("Prefabs/Items/Weapons/RifleTankbot", position, rotation, weaponAttachmentBone.transform);
        weaponBehaviour = weapon.GetComponent<WeaponBehaviour>();
        weaponBehaviour.ownerID = ID;
        weaponBehaviour.ownerFaction = faction;
        weaponBehaviour.groundReferenceObject = groundReferenceObject;
        weapons.Add(weaponBehaviour);
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

    protected override List<WeaponBehaviour> GetWeapons()
    {
        return weapons;
    }

    private List<WeaponData> SaveWeapons()
    {
        List<WeaponData> weapons = new List<WeaponData>();
        return weapons;
    }

    private void LoadWeapons(List<WeaponData> data)
    {
        for (int i = 0; i < weapons.Count; i++) weapons[i].Load(data[i]);
    }

    new public TankbotData Save()
    {
        TankbotData data = new TankbotData(base.Save());
        data.animationData = animations.Save();
        data.weaponsData = SaveWeapons();
        return data;
    }

    public void Load(TankbotData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        SetAlive(GetAlive());
        animations.Load(data.animationData);
        LoadWeapons(data.weaponsData);
    }

    public static GameObject Spawn(TankbotData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = CreatureBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<TankbotBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(SpiderbotData data, Transform parent = null)
    {
        GameObject obj = CreatureBehaviour.Spawn(data, parent);
        obj.GetComponent<SpiderbotBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class TankbotData : CreatureData
{
    public TankbotData() { }

    public TankbotData(CreatureData data) : base(data)
    {
        this.faction = data.faction;
        this.aiData = data.aiData;
        this.moveSpeed = data.moveSpeed;
        this.alive = data.alive;
        this.maxHealth = data.maxHealth;
        this.health = data.health;
        this.inventory = data.inventory;
    }

    public TankbotAnimationData animationData;
    public List<WeaponData> weaponsData;

}