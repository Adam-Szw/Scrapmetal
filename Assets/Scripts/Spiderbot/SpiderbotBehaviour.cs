using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class SpiderbotBehaviour : CreatureBehaviour, Saveable<SpiderbotData>, Spawnable<SpiderbotData>
{
    public GameObject target;

    public SpiderbotAnimations animations;

    public static string[] BODYPARTS = new string[] { "Sensor", "Turret" };

    private WeaponBehaviour weaponBehaviour;

    new protected void Awake()
    {
        base.Awake();
        Animator bodyAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Body").GetComponent<Animator>();
        GameObject aimBone = HelpFunc.RecursiveFindChild(this.gameObject, "Turret_Parent");
        animations = new SpiderbotAnimations(transform, new List<Animator>() { bodyAnimator }, BODYPARTS, aimBone);
        animations.movementDeterminesFlip = true;
        Vector3 position = weaponAttachmentBone.transform.position;
        Quaternion rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        GameObject weapon = WeaponBehaviour.Spawn("Prefabs/Items/Weapons/MissileLauncherSpiderbot", position, rotation, weaponAttachmentBone.transform);
        weaponBehaviour = weapon.GetComponent<WeaponBehaviour>();
        weaponBehaviour.ownerID = ID;
        weaponBehaviour.groundReferenceObject = groundReferenceObject;
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
        return new List<WeaponBehaviour>(){ weaponBehaviour };
    }

    new public SpiderbotData Save()
    {
        SpiderbotData data = new SpiderbotData(base.Save());
        data.animationData = animations.Save();
        data.weaponData = weaponBehaviour.Save();
        return data;
    }

    public void Load(SpiderbotData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        SetAlive(GetAlive());
        animations.Load(data.animationData);
        weaponBehaviour.Load(data.weaponData);
    }

    public static GameObject Spawn(SpiderbotData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = CreatureBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<SpiderbotBehaviour>().Load(data, false);
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
public class SpiderbotData : CreatureData
{
    public SpiderbotData() { }

    public SpiderbotData(CreatureData data) : base(data)
    {
        this.faction = data.faction;
        this.aiData = data.aiData;
        this.moveSpeed = data.moveSpeed;
        this.alive = data.alive;
        this.maxHealth = data.maxHealth;
        this.health = data.health;
    }

    public SpiderbotAnimationData animationData;
    public WeaponData weaponData;

}