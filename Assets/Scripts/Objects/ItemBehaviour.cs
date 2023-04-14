using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using static WeaponBehaviour;

public class ItemBehaviour : EntityBehaviour, Saveable<ItemData>, Spawnable<ItemData>
{
    public CreatureBehaviour.FactionAllegiance ownerFaction = CreatureBehaviour.FactionAllegiance.berserk;
    public string descriptionText = "No description";
    public string inventoryIconLink = "Icons/Icon_Test";
    public int value = 0;
    public bool pickable = true;

    [HideInInspector] public ulong ownerID = 0;

    private GameObject aura = null;
    private GameObject hText = null;

    protected void Start()
    {
        if (pickable)
        {
            AddInteractionCollider();
            interactionEnterEffect = (CreatureBehaviour user) => { InteractionEnter(user); };
            interactionUseEffect = (CreatureBehaviour user) => { InteractionUse(user); };
        }
    }

    public virtual void Use() { }

    private void InteractionUse(CreatureBehaviour user)
    {
        // Do nothing if we cant pick up item due to inventory being full
        if (user.inventory.Count >= CreatureBehaviour.inventoryLimit) return;
        if (aura) Destroy(aura);
        aura = null;
        if (hText) Destroy(hText);
        hText = null;
        // Give the item to the user and destroy entity
        pickable = false;   // Make sure that item is no longer interactible while in inventory
        MethodInfo saveMethod = this.GetType().GetMethod("Save");
        user.inventory.Add((ItemData)saveMethod.Invoke(this, null));
        Destroy(this.gameObject);
    }

    private void InteractionEnter(CreatureBehaviour user)
    {
        // Do nothing if the user is not player
        if (user is not PlayerBehaviour) return;
        StartCoroutine(HighlightItem(PlayerBehaviour.interactibleInteravalTime));
        StartCoroutine(SpawnPickupText(PlayerBehaviour.interactibleInteravalTime));
    }

    // Envelop item in aura for given time
    private IEnumerator HighlightItem(float time)
    {
        aura = Instantiate(Resources.Load<GameObject>("Prefabs/UI/HighlightAura"));
        aura.transform.parent = transform;
        aura.transform.localPosition = Vector3.zero;
        yield return new WaitForSeconds(time);
        Destroy(aura);
        aura = null;
    }

    // Spawn a pickup text above item for given time
    private IEnumerator SpawnPickupText(float time)
    {
        hText = Instantiate(Resources.Load<GameObject>("Prefabs/UI/TextObject"));
        hText.GetComponentInChildren<TextMeshProUGUI>().text = "Press (E) to pick up item";
        hText.transform.position = (Vector3)interactTextOffset + transform.position;
        yield return new WaitForSeconds(time);
        Destroy(hText);
        hText = null;
    }

    public new ItemData Save()
    {
        ItemData data = new ItemData(base.Save());
        data.prefabPath = prefabPath;
        data.ownerID = ownerID;
        data.ownerFaction = ownerFaction;
        data.descriptionText = descriptionText;
        data.inventoryIconLink = inventoryIconLink;
        data.value = value;
        data.pickable = pickable;
        return data;
    }

    public void Load(ItemData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        ownerID = data.ownerID;
        ownerFaction = data.ownerFaction;
        descriptionText = data.descriptionText;
        inventoryIconLink = data.inventoryIconLink;
        value = data.value;
        pickable = data.pickable;
    }

    public static GameObject Spawn(ItemData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = EntityBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<ItemBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(ItemData data, Transform parent = null)
    {
        GameObject obj = EntityBehaviour.Spawn(data, parent);
        obj.GetComponent<ItemBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class ItemData : EntityData
{
    public ItemData() { }

    public ItemData(EntityData data)
    {
        prefabPath = data.prefabPath;
        ID = data.ID;
        location = data.location;
        rotation = data.rotation;
        scale = data.scale;
        velocity = data.velocity;
        speed = data.speed;
        active = data.active;
    }

    public ulong ownerID;
    public CreatureBehaviour.FactionAllegiance ownerFaction;
    public string descriptionText;
    public string inventoryIconLink;
    public int value;
    public bool pickable;
}