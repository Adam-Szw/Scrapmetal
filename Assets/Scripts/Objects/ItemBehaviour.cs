using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using static UnityEditor.Progress;
using static WeaponBehaviour;

public class ItemBehaviour : EntityBehaviour, Saveable<ItemData>, Spawnable<ItemData>
{
    public CreatureBehaviour.FactionAllegiance ownerFaction = CreatureBehaviour.FactionAllegiance.neutral;
    public string inventoryIconLink = "Icons/Icon_Test";
    public ulong descriptionTextLinkID = 0;
    public int value = 0;
    public bool pickable = true;
    public bool removeOnPick = false;

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
        // Actions for normal items
        if (!removeOnPick)
        {
            // Give the item to the user and destroy entity
            pickable = false;   // Make sure that item is no longer interactible while in inventory
            MethodInfo saveMethod = this.GetType().GetMethod("Save");
            user.inventory.Add((ItemData)saveMethod.Invoke(this, null));
            Destroy(gameObject);
            // Spawn pickup text
            user.SpawnFloatingText(Color.green, "Item picked up", 0.5f);
        }
        // Actions for scrap/currency or other things that get liquidated instantly
        else
        {
            // Add value to the player's currency count if picked up by player
            if (user is PlayerBehaviour) ((PlayerBehaviour)user).currencyCount += value;
            // Destroy item
            Destroy(gameObject);
            // Spawn text
            user.SpawnFloatingText(Color.green, "Scrap +" + value, 0.5f);
        }
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
        hText.transform.position = interactAttachment.transform.position + new Vector3(0f, 0.7f, 0f);
        yield return new WaitForSeconds(time);
        Destroy(hText);
        hText = null;
    }

    /* Get data for a new item with given parameters. It replicates spawning proces without
     * actually creating any entity, as such it will for example allocate a new ID correctly.
     */
    public static ItemData Produce(string prefabPath, ulong descriptionLink, string iconLink, int value, bool pickable)
    {
        ItemData data = new ItemData();
        data.active = true;
        data.ID = ++GlobalControl.nextID;
        data.prefabPath = prefabPath;
        data.location = HelpFunc.VectorToArray(Vector3.zero);
        data.rotation = HelpFunc.QuaternionToArray(Quaternion.identity);
        data.scale = HelpFunc.VectorToArray(Vector3.zero);
        data.velocity = HelpFunc.VectorToArray(Vector2.zero);
        data.speed = 0f;
        data.ownerID = 0;
        data.ownerFaction = CreatureBehaviour.FactionAllegiance.neutral;
        data.descriptionTextLinkID = descriptionLink;
        data.inventoryIconLink = iconLink;
        data.value = value;
        data.pickable = pickable;
        return data;
    }

    public new ItemData Save()
    {
        ItemData data = new ItemData(base.Save());
        data.prefabPath = prefabPath;
        data.ownerID = ownerID;
        data.ownerFaction = ownerFaction;
        data.descriptionTextLinkID = descriptionTextLinkID;
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
        descriptionTextLinkID = data.descriptionTextLinkID;
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

    // This spawn method will check if data is actually of one of its children and call them instead
    public static GameObject FlexibleSpawn(ItemData data, Transform parent = null)
    {
        GameObject obj;
        if (data is WeaponData) obj = WeaponBehaviour.Spawn((WeaponData)data, parent);
        else if (data is ArmorData) obj = ArmorBehaviour.Spawn((ArmorData)data, parent);
        else obj = Spawn(data, parent);
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
    public string inventoryIconLink;
    public ulong descriptionTextLinkID;
    public int value;
    public bool pickable;
}