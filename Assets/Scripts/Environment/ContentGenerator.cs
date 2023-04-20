using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class ContentGenerator : MonoBehaviour
{
    public enum CreatureTier
    {
        hostileEasy,    // Easy hostile bots
        hostileMedium,  // Medium hostile bots
        hostileHard,    // Hard hostile bots
        enemyEasy,      // Enemy NPCs with low health and weak weapons
        enemyMedium,    // Enemy NPCs with low-mid tier weapons
        enemyHard,      // Enemy NPCs with mid-high tier weapons and health
        limited         // Will never be randomly spawned
    }

    public enum ItemTier
    {
        weak,
        medium,
        strong,
        limited     // Limited items will not appear in loot tables and can only be acquired
    }

    public enum LootTier
    {
        small,  // Only some enemies will drop low tier items
        medium, // Most enemies will drop low tier items, some will drop medium tier
        large,  // All types of items, with strong items being rare
        massive // Lots of cash and some most powerful items
    }

    public PolygonCollider2D spawnCollider;    // Enemies will be spawned only inside this collider

    [Serializable]
    public class SpawnData
    {
        public int minSpawn;
        public int maxSpawn;
        public CreatureTier creatureTier;
        public LootTier lootTier;
    }

    /* Arguments: Min and max of how many enemies can spawn, Tier of enemies and their weapons, Tier of loot that will be dropped
     */
    public List<SpawnData> spawns = new List<SpawnData>();

    public void Trigger()
    {
        foreach (SpawnData spawn in spawns)
        {
            int spawnCountMin = spawn.minSpawn;
            int spawnCountMax = spawn.maxSpawn;
            CreatureTier enemyTier = spawn.creatureTier;
            LootTier lootTier = spawn.lootTier;
            // Randomize how many enemies to spawn
            int spawnCount = Random.Range(spawnCountMin, spawnCountMax + 1);
            for (int i = 0; i < spawnCount; i++)
            {
                List<string> creatureAssets;
                CreatureLibrary.tierDictionary.TryGetValue(enemyTier, out creatureAssets);
                if (creatureAssets != null)
                {
                    // Spawn enemy. If couldnt spawn enemy, go to next iteration
                    // Randomize point inside collider, with more central bias
                    Vector3 position = HelpFunc.GetRandomPointInPolygonCollider(spawnCollider);
                    GameObject enemy = SpawnEnemy(enemyTier, position);
                    if (!enemy) continue;
                    List<ItemData> loot = new List<ItemData>();
                    // If enemy is of humanoid type, give it a weapon and make it lootable
                    bool humanoid = enemyTier == CreatureTier.enemyEasy || enemyTier == CreatureTier.enemyMedium || enemyTier == CreatureTier.enemyHard;
                    if (enemyTier == CreatureTier.enemyEasy)
                    {
                        WeaponData weapon = GetRandomWeapon(new List<ItemTier>() { ItemTier.weak });
                        if (weapon != null)
                        {
                            loot.Add(FabricateWeapon(weapon));
                            GiveWeapon(weapon, enemy);
                        }
                    }
                    else if (enemyTier == CreatureTier.enemyMedium)
                    {
                        WeaponData weapon = GetRandomWeapon(new List<ItemTier>() { ItemTier.weak, ItemTier.medium });
                        if (weapon != null)
                        {
                            loot.Add(FabricateWeapon(weapon));
                            GiveWeapon(weapon, enemy);
                        }
                    }
                    else if (enemyTier == CreatureTier.enemyHard)
                    {
                        WeaponData weapon = GetRandomWeapon(new List<ItemTier>() { ItemTier.medium, ItemTier.strong });
                        if (weapon != null)
                        {
                            loot.Add(FabricateWeapon(weapon));
                            GiveWeapon(weapon, enemy);
                        }
                    }
                    // See if player has extra modifiers
                    float lootModifier = 1f;
                    float scrapModifier = 1f;
                    GameObject player = GlobalControl.GetPlayer();
                    if (player)
                    {
                        lootModifier = player.GetComponent<PlayerBehaviour>().hasLootGeneration ? 1.3f : 1f;
                        scrapModifier = player.GetComponent<PlayerBehaviour>().hasScrapGeneration ? 1.8f : 1f;
                    }
                    // Generate loot. Humanoid enemies can give weapons but cant give armor
                    foreach (ItemData extraLoot in GetRandomLoot(lootTier, !humanoid, humanoid, scrapModifier, lootModifier)) loot.Add(extraLoot);
                    // Give loot to enemy
                    enemy.GetComponent<CreatureBehaviour>().loot = loot;
                }
            }
        }
    }

    public static WeaponData FabricateWeapon(WeaponData weapon)
    {
        GameObject obj = WeaponBehaviour.Spawn(weapon);
        WeaponData data = obj.GetComponent<WeaponBehaviour>().Save();
        Destroy(obj);
        return data;
    }

    private static GameObject SpawnEnemy(CreatureTier tier, Vector3 position)
    {
        // Get enemies at this tier
        List<string> creatureAssets;
        CreatureLibrary.tierDictionary.TryGetValue(tier, out creatureAssets);
        // If no creatures at this tier, return
        if (creatureAssets != null)
        {
            // Randomize enemy from given tier
            int i = Random.Range(0, creatureAssets.Count - 1);
            GameObject enemy = Instantiate(Resources.Load<GameObject>(CreatureLibrary.CREATURES_PREFAB_PATH + creatureAssets[i]), position, Quaternion.identity);
            return enemy;

        }
        return null;
    }

    public static WeaponData GetRandomWeapon(List<ItemTier> tiersPossible)
    {
        // Collect list of all possible weapons
        List<string> weapons = new List<string>();
        foreach (ItemTier tier in tiersPossible)
        {
            // Get weapon assets at this tier
            List<string> tierWeapons;
            ItemLibrary.weaponTierDictionary.TryGetValue(tier, out tierWeapons);
            if (tierWeapons != null) foreach (string weapon in tierWeapons) weapons.Add(weapon);
        }
        // Select random weapon if there are any on this tier
        if (weapons.Count > 0)
        {
            // Randomize weapon
            int i = Random.Range(0, weapons.Count - 1);
            GameObject weapon = Resources.Load<GameObject>(ItemLibrary.ITEM_PREFABS_PATH + weapons[i]);
            return weapon.GetComponent<WeaponBehaviour>().Save();
        }
        return null;
    }

    private void GiveWeapon(WeaponData weapon, GameObject receiver)
    {
        weapon.unlimitedAmmo = true;
        weapon.pickable = false;
        receiver.GetComponent<HumanoidBehaviour>().SetItemActive(weapon);
    }

    public static List<ItemData> GetRandomLoot(LootTier tier, bool weaponsEnabled, bool armorsEnabled, float scrapMultiplier = 1f, float chanceMultiplier = 1f)
    {
        List<ItemData> loot = new List<ItemData>();
        switch (tier)
        {
            case LootTier.small:
                {
                    // Moderate chance for lowest quality item + small amount of scrap
                    if (Random.value < 0.7 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.weak, false, false, true, 7, 16));
                    if (weaponsEnabled && Random.value < 0.45 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.weak, true, false, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.3 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.weak, false, true, false, 1, 1));
                    loot.Add(GetScrapData((int)Random.Range(12 * scrapMultiplier, 26 * scrapMultiplier)));
                    break;
                }
            case LootTier.medium:
                {
                    // High chance for low quality item or small chance for medium item + reasonable scrap
                    if (Random.value < 0.95 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.weak, false, false, true, 7, 16));
                    if (Random.value < 0.35 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.medium, false, false, true, 4, 7));
                    if (weaponsEnabled && Random.value < 0.8 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.weak, true, false, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.6 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.weak, false, true, false, 1, 1));
                    loot.Add(GetScrapData((int)Random.Range(28 * scrapMultiplier, 82 * scrapMultiplier)));
                    break;
                }
            case LootTier.large:
                {
                    // Small chance for epic item, good chance for medium item or multiple small items + good scrap
                    if (Random.value < 0.95 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.weak, false, false, true, 15, 32));
                    if (Random.value < 0.85 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.medium, false, false, true, 5, 12));
                    if (Random.value < 0.45 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.strong, false, false, true, 2, 4));
                    if (weaponsEnabled && Random.value < 0.9 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.weak, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.4 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.weak, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.5 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.medium, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.25 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.strong, true, false, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.6 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.medium, false, true, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.2 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.strong, false, true, false, 1, 1));
                    loot.Add(GetScrapData((int)Random.Range(125 * scrapMultiplier, 362 * scrapMultiplier)));
                    break;
                }
            case LootTier.massive:
                {
                    // Tons of scrap + multiple epic and medium items
                    if (Random.value < 0.95 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.medium, false, false, true, 5, 12));
                    if (Random.value < 0.85 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.strong, false, false, true, 6, 9));
                    if (Random.value < 0.35 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.strong, false, false, true, 6, 9));
                    if (weaponsEnabled && Random.value < 0.9 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.medium, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.5 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.medium, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.25 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.medium, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.9 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.strong, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.4 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.strong, true, false, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.8 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.strong, false, true, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.4 * chanceMultiplier) loot.Add(GetRandomItem(ItemTier.strong, false, true, false, 1, 1));
                    loot.Add(GetScrapData((int)Random.Range(425 * scrapMultiplier, 1204 * scrapMultiplier)));
                    break;
                }
        }
        // Sometimes we will fail to obtain items for loot table. Clear all nulls from the list
        List<ItemData> toRemove = new List<ItemData>();
        foreach (ItemData item in loot) if (item == null) toRemove.Add(item);
        foreach (ItemData item in toRemove) loot.Remove(item);
        return loot;
    }

    private static ItemData GetRandomItem(ItemTier tier, bool weapons, bool armors, bool items, int minQuantity, int maxQuantity)
    {
        // Collect list of all possible loot items
        List<string> lootPossible = new List<string>();

        // Get weapons at this tier
        if (weapons)
        {
            List<string> tierWeapons;
            ItemLibrary.weaponTierDictionary.TryGetValue(tier, out tierWeapons);
            if (tierWeapons != null) foreach (string weapon in tierWeapons) lootPossible.Add(weapon);
        }

        // Get armors at this tier
        if (armors)
        {
            List<string> tierArmors;
            ItemLibrary.armorTierDictionary.TryGetValue(tier, out tierArmors);
            if (tierArmors != null) foreach (string armor in tierArmors) lootPossible.Add(armor);
        }

        // Get items at this tier
        if (items)
        {
            List<string> tierItems;
            ItemLibrary.itemTierDictionary.TryGetValue(tier, out tierItems);
            if (tierItems != null) foreach (string item in tierItems) lootPossible.Add(item);
        }

        // Get random loot from possible items
        if (lootPossible.Count == 0) return null;
        int i = Random.Range(0, lootPossible.Count - 1);
        GameObject loot = Resources.Load<GameObject>(ItemLibrary.ITEM_PREFABS_PATH + lootPossible[i]);

        // Save correct data
        ItemBehaviour b = loot.GetComponent<ItemBehaviour>();
        MethodInfo saveMethod = b.GetType().GetMethod("Save");
        ItemData data = (ItemData)saveMethod.Invoke(b, null);
        
        // If its ammo - randomize quantity
        if (data is AmmoData)
        {
            int quantity = Random.Range(minQuantity, maxQuantity);
            ((AmmoData)data).quantity = quantity;
        }
        return data;
    }

    private static ItemData GetScrapData(int value)
    {
        GameObject scrap = Resources.Load<GameObject>(ItemLibrary.SCRAP_PATH);
        ItemData data = scrap.GetComponent<ItemBehaviour>().Save();
        data.value = value;
        return data;
    }

}