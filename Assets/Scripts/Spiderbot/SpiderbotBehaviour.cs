using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class SpiderbotBehaviour : CreatureBehaviour
{

    public static new string PREFAB_PATH = "Prefabs/Creatures/Spiderbot";

    [SerializeField] private GameObject weaponAttachmentBone;

    public GameObject target;

    public SpiderbotAnimations animations;

    public static string[] BODYPARTS = new string[] { "Sensor", "Turret" };

    private WeaponBehaviour weaponBehaviour;

    new protected void Awake()
    {
        base.Awake();
        Animator bodyAnimator = HelpFunc.RecursiveFindChild(this.gameObject, "Body").GetComponent<Animator>();
        animations = new SpiderbotAnimations(transform, new List<Animator>() { bodyAnimator }, new List<string>(BODYPARTS));
        Vector3 position = weaponAttachmentBone.transform.position;
        Quaternion rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        GameObject weapon = WeaponBehaviour.Spawn("Prefabs/Items/Weapons/MissileLauncherSpiderbot", position, rotation, weaponAttachmentBone.transform);
        weaponBehaviour = weapon.GetComponent<WeaponBehaviour>();
        weaponBehaviour.ownerID = ID;
        weaponBehaviour.guidanceTargetID = 2;
    }

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
        weaponBehaviour.Use();
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

    new public SpiderbotData Save()
    {
        SpiderbotData data = new SpiderbotData(base.Save());
        data.animationData = animations.Save();
        return data;
    }

    public void Load(SpiderbotData data)
    {
        base.Load(data);
        SetAlive(GetAlive());
        animations.Load(data.animationData);
    }

    public static void SpawnEntity(HumanoidData data)
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>(SpiderbotBehaviour.PREFAB_PATH));
        obj.GetComponent<SpiderbotBehaviour>().Load(data);
    }

}

[Serializable]
public class SpiderbotData : CreatureData
{
    public SpiderbotData() { }

    public SpiderbotData(CreatureData data) : base(data)
    {
        this.alive = data.alive;
        this.health = data.health;
    }

    public SpiderbotAnimationData animationData;
}