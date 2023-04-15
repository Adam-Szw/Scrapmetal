using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEditor;
using UnityEditor.U2D.Path.GUIFramework;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.U2D.Animation;
using static CreatureAnimations;
using static UnityEditor.Progress;

/* Handles both dumb bullets and guided missiles
 */
public class ProjectileBehaviour : ItemBehaviour, Saveable<ProjectileData>, Spawnable<ProjectileData>
{
    public delegate void Effect(ProjectileBehaviour projectile, CreatureBehaviour targetHit);

    public GameObject spriteObject;

    public float speedInitial;
    public float acceleration;
    public float lifespan;
    public float lifeRemaining;
    public float damage;
    public bool piercing = false;
    // Special effects can be caused here that are triggered on projectile destroy
    [HideInInspector] public Effect effect;

    // How many degrees per second the missile is allowed to turn to guide
    public float guidanceStep = 0;
    [HideInInspector] public ulong guidanceTargetID = 0;
    [HideInInspector] public GameObject guidanceTarget = null;

    // These values are used to setup special effects
    public bool sendTargetBerserk = false;
    public float explosionRadius = 0f;
    public bool guideOnPointer = false;

    new protected void Awake()
    {
        base.Awake();
        SetSpeed(speedInitial);
        SetupSpecialEffect();
    }

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;
        // If guidance used, turn missile accordingly
        if (guideOnPointer)
        {
            Vector2 targetPos = PlayerInput.mousePos;
            TurnMissile(targetPos);
        }
        else
        {
            if (guidanceTarget != null && guidanceStep > 0)
            {
                Vector2 targetPos = guidanceTarget.transform.position;
                TurnMissile(targetPos);
            }
            // Acquire target if ID is given but target not found
            else if (guidanceTargetID != 0) guidanceTarget = HelpFunc.FindEntityByID(guidanceTargetID);
        }
        // Update speed
        SetSpeed(Mathf.Max(GetSpeed() + acceleration * Time.deltaTime, 0.0f));
        // Update lifetime
        lifeRemaining -= Time.deltaTime;
        if (lifeRemaining < 0.0f) RunEffect(this, null);
    }

    public void RunEffect(ProjectileBehaviour projectile, CreatureBehaviour targetHit)
    {
        effect?.Invoke(projectile, targetHit);
    }

    public static GameObject Spawn(ProjectileData data)
    {
        GameObject obj = Instantiate(Resources.Load<GameObject>(data.prefabPath));
        obj.GetComponent<ProjectileBehaviour>().Load(data);
        return obj;
    }

    public void CreateStructureCollider(GameObject groundReference)
    {
        float referenceHeight = groundReference.transform.position.y;
        float myHeight = transform.position.y;
        GameObject structureCollider = new GameObject("Structure_Collider");
        structureCollider.transform.parent = transform;
        structureCollider.transform.localPosition = new Vector3(0f, referenceHeight - myHeight, 0f);
        structureCollider.layer = 10;
        CircleCollider2D collider = structureCollider.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.1f;
        ProjectileTrigger trigger = structureCollider.AddComponent<ProjectileTrigger>();
        trigger.behaviour = this;
    }

    public void RotateSprite(float angle)
    {
        spriteObject.transform.Rotate(0.0f, 0.0f, angle);
        // Update velocity vector to match rotation
        SetMoveVector(HelpFunc.EulerToVec2(spriteObject.transform.rotation.eulerAngles.z));
    }

    private void TurnMissile(Vector2 targetPos)
    {
        Vector2 missilePos = transform.position;
        Vector2 targetVec = targetPos - missilePos;
        float targetAngle = HelpFunc.Vec2ToAngle(targetVec);
        float currAngle = spriteObject.transform.rotation.eulerAngles.z;
        currAngle = HelpFunc.NormalizeAngle(currAngle);
        float angleDiff = -HelpFunc.SmallestAngle(targetAngle, currAngle);
        float maxStep = guidanceStep * Time.deltaTime;
        float step = Mathf.Clamp(angleDiff, -maxStep, maxStep);
        RotateSprite(step);
    }

    private void SetupSpecialEffect()
    {
        effect = (ProjectileBehaviour projectile, CreatureBehaviour target) => {
            if (target)
            {
                if (target.aiControl) target.aiControl.NotifyTakingDamage(projectile.ownerFaction);
                if (projectile.explosionRadius <= 0) target.DealDamage(projectile.damage);
            }
            if (projectile.explosionRadius > 0)
            {
                SpawnExplosion(projectile.transform.position, explosionRadius * 0.1f);
                List<CreatureBehaviour> inRange = HelpFunc.GetCreaturesInRadius(projectile.transform.position, explosionRadius);
                foreach (CreatureBehaviour creature in inRange)
                {
                    if (creature.aiControl) creature.aiControl.NotifyTakingDamage(projectile.ownerFaction);
                    creature.DealDamage(projectile.damage);
                }
            }
            if (target && projectile.sendTargetBerserk) target.faction = CreatureBehaviour.FactionAllegiance.berserk;
            if (!projectile.piercing || (projectile.lifeRemaining <= 0f)) Destroy(projectile.gameObject);
        };
    }

    private void SpawnExplosion(Vector2 location, float scale)
    {
        GameObject expl = Instantiate(Resources.Load<GameObject>("Prefabs/Effects/Explosion"));
        expl.transform.localScale = new Vector3(scale, scale, 1f);
        expl.transform.position = location;
    }

    new public ProjectileData Save()
    {
        ProjectileData data = new ProjectileData(base.Save());
        data.speedInitial = speedInitial;
        data.acceleration = acceleration;
        data.lifespan = lifespan;
        data.damage = damage;
        data.piercing = piercing;
        data.lifeRemaining = lifeRemaining;
        data.projectileRotation = spriteObject.transform.localEulerAngles.z;
        data.sendTargetBerserk = sendTargetBerserk;
        data.explosionRadius = explosionRadius;
        data.guideOnPointer = guideOnPointer;
        return data;
    }

    public void Load(ProjectileData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        speedInitial = data.speedInitial;
        acceleration = data.acceleration;
        lifespan = data.lifespan;
        damage = data.damage;
        piercing = data.piercing;
        ownerID = data.ownerID;
        lifeRemaining = data.lifeRemaining;
        spriteObject.transform.localEulerAngles = new Vector3(0f, 0f, data.projectileRotation);
        sendTargetBerserk = data.sendTargetBerserk;
        explosionRadius = data.explosionRadius;
        guideOnPointer = data.guideOnPointer;
    }

    public static GameObject Spawn(ProjectileData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<ProjectileBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(ProjectileData data, Transform parent = null)
    {
        GameObject obj = ItemBehaviour.Spawn(data, parent);
        obj.GetComponent<ProjectileBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class ProjectileData : ItemData
{
    public ProjectileData() { }

    public ProjectileData(ItemData data) : base(data)
    {
        prefabPath = data.prefabPath;
        ownerID = data.ownerID;
        descriptionTextLinkID = data.descriptionTextLinkID;
        inventoryIconLink = data.inventoryIconLink;
        value = data.value;
        pickable = data.pickable;
        removeOnPick = data.removeOnPick;
    }

    public float speedInitial;
    public float acceleration;
    public float lifespan;
    public float damage;
    public float lifeRemaining;
    public float projectileRotation;
    public bool sendTargetBerserk;
    public float explosionRadius;
    public bool guideOnPointer;
    public bool piercing;
}