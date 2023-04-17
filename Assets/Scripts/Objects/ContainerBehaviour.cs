using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using static ContentGenerator;

public class ContainerBehaviour : EntityBehaviour, Saveable<ContainerData>, Spawnable<ContainerData>
{

    public bool isAvailable = true;
    public bool requiresOpeningSkill = false;
    public bool randomizeContent = true;
    public LootTier lootTier = LootTier.small;

    [HideInInspector] public List<ItemData> loot = new List<ItemData>();

    private bool interacting = false;

    protected void Start()
    {
        AddInteractionCollider();
        interactionEnterEffect = (CreatureBehaviour user) => { InteractionEnter(user); };
        interactionUseEffect = (CreatureBehaviour user) => { InteractionUse(user); };
    }

    private void InteractionUse(CreatureBehaviour user)
    {
        // If we are already opening container, do nothing
        if (interacting) return;
        // If we cant open the container, play text
        if ((user is PlayerBehaviour) && requiresOpeningSkill && !((PlayerBehaviour)user).hasChestOpening)
        {
            user.SpawnFloatingText(Color.red, "Container is locked!", 0.5f);
            return;
        }
        if (aura) Destroy(aura);
        aura = null;
        if (hText) Destroy(hText);
        hText = null;
        // Do nothing if container was already opened
        if (!isAvailable) return;
        // See if player has extra modifiers
        float lootModifier = 1f;
        float scrapModifier = 1f;
        GameObject player = GlobalControl.GetPlayer();
        if (player)
        {
            lootModifier = player.GetComponent<PlayerBehaviour>().hasLootGeneration ? 1.3f : 1f;
            scrapModifier = player.GetComponent<PlayerBehaviour>().hasScrapGeneration ? 1.8f : 1f;
        }
        if (randomizeContent) loot = GetRandomLoot(lootTier, true, true, scrapModifier, lootModifier);
        interacting = true;
        StartCoroutine(LootingCoroutine(user));
    }

    private IEnumerator LootingCoroutine(CreatureBehaviour user)
    {
        List<ItemData> itemsLost = new List<ItemData>();
        foreach (ItemData item in loot)
        {
            // Do nothing if we cant pick up item due to inventory being full
            if (user.GetInventory().Count >= CreatureBehaviour.inventoryLimit && (!item.removeOnPick))
            {
                user.SpawnFloatingText(Color.red, "Inventory full!", 0.5f);
                continue;
            }
            // Add item to user inventory and remove from container
            item.pickable = false;
            ItemBehaviour.GiveItem(item, user);
            itemsLost.Add(item);
            yield return new WaitForSeconds(.25f);
        }
        foreach (ItemData itemLost in itemsLost) loot.Remove(itemLost);
        if (loot.Count <= 0) isAvailable = false;
        interacting = false;
    }

    private void InteractionEnter(CreatureBehaviour user)
    {
        // Do nothing if container was already opened
        if (!isAvailable) return;
        // Do nothing if the user is not player
        if (user is not PlayerBehaviour) return;
        StartCoroutine(HighlightEntityCoroutine(PlayerBehaviour.interactibleInteravalTime));
        StartCoroutine(SpawnInteractionTextCoroutine("Press (E) to loot container", PlayerBehaviour.interactibleInteravalTime, 0.7f));
    }

    public new ContainerData Save()
    {
        ContainerData data = new ContainerData(base.Save());
        data.isAvailable = isAvailable;
        data.requiresOpeningSkill = requiresOpeningSkill;
        return data;
    }

    public void Load(ContainerData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        isAvailable = data.isAvailable;
        requiresOpeningSkill = data.requiresOpeningSkill;
    }

    public static GameObject Spawn(ContainerData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = EntityBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<ContainerBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(ContainerData data, Transform parent = null)
    {
        GameObject obj = EntityBehaviour.Spawn(data, parent);
        obj.GetComponent<ContainerBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class ContainerData : EntityData
{
    public ContainerData() { }

    public ContainerData(EntityData data)
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

    public bool isAvailable;
    public bool requiresOpeningSkill;

}