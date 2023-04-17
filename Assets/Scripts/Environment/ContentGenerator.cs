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

    public int spawnCountMax;
    public int spawnCountMin;           // Min and max of how many enemies can spawn
    public CreatureTier enemyTier;      // Tier of enemies and their weapons
    public LootTier lootTier;           // Tier of loot that will be dropped

    public GameObject spawnLocation;    // Location of enemy spawn
    public float spawnRadius;           // How far away from center enemies can be spawned

    public void Trigger()
    {
        // Randomize how many enemies to spawn
        int spawnCount = Random.Range(spawnCountMin, spawnCountMax + 1);
        for (int i = 0; i < spawnCount; i++)
        {
            List<string> creatureAssets;
            CreatureLibrary.tierDictionary.TryGetValue(enemyTier, out creatureAssets);
            if (creatureAssets != null)
            {
                // Spawn enemy. If couldnt spawn enemy, go to next iteration
                Vector3 position = spawnLocation.transform.position;
                GameObject enemy = SpawnEnemy(enemyTier, position, spawnRadius);
                if (!enemy) continue;
                List<ItemData> loot = new List<ItemData>();
                // If enemy is of humanoid type, give it a weapon and make it lootable
                bool humanoid = enemyTier == CreatureTier.enemyEasy || enemyTier == CreatureTier.enemyMedium || enemyTier == CreatureTier.enemyHard;
                if (enemyTier == CreatureTier.enemyEasy)
                {
                    WeaponData weapon = GetRandomWeapon(new List<ItemTier>() { ItemTier.weak });
                    loot.Add(HelpFunc.DeepCopy(weapon));
                    GiveWeapon(weapon, enemy);
                }
                else if (enemyTier == CreatureTier.enemyMedium)
                {
                    WeaponData weapon = GetRandomWeapon(new List<ItemTier>() { ItemTier.weak, ItemTier.medium });
                    loot.Add(HelpFunc.DeepCopy(weapon));
                    GiveWeapon(weapon, enemy);
                }
                else if (enemyTier == CreatureTier.enemyHard)
                {
                    WeaponData weapon = GetRandomWeapon(new List<ItemTier>() { ItemTier.medium, ItemTier.strong });
                    loot.Add(HelpFunc.DeepCopy(weapon));
                    GiveWeapon(weapon, enemy);
                }
                // Generate loot. Humanoid enemies can give weapons but cant give armor
                loot.Concat(GetRandomLoot(lootTier, humanoid, !humanoid));
                // Give loot to enemy
                enemy.GetComponent<CreatureBehaviour>().loot = loot;
            }
        }
    }

    private GameObject SpawnEnemy(CreatureTier tier, Vector3 position, float radius)
    {
        // Get enemies at this tier
        List<string> creatureAssets;
        CreatureLibrary.tierDictionary.TryGetValue(tier, out creatureAssets);
        // If no creatures at this tier, return
        if (creatureAssets != null)
        {
            // Randomize enemy from given tier
            int i = Random.Range(0, creatureAssets.Count - 1);
            // Randomize location
            position.x += radius * ((Random.value * 2) - 1);
            position.y += radius * ((Random.value * 2) - 1);
            GameObject enemy = Instantiate(Resources.Load<GameObject>(CreatureLibrary.CREATURES_PREFAB_PATH + creatureAssets[i]), position, Quaternion.identity);
            return enemy;

        }
        return null;
    }

    private WeaponData GetRandomWeapon(List<ItemTier> tiersPossible)
    {
        // Collect list of all possible weapons
        List<string> weapons = new List<string>();
        foreach (ItemTier tier in tiersPossible)
        {
            // Get weapon assets at this tier
            List<string> tierWeapons;
            ItemLibrary.weaponTierDictionary.TryGetValue(tier, out tierWeapons);
            if (tierWeapons != null) weapons.Concat(tierWeapons);
        }
        // Select random weapon if there are any on this tier
        if (weapons.Count > 0)
        {
            // Randomize weapon
            int i = Random.Range(0, weapons.Count - 1);
            GameObject weapon = Resources.Load<GameObject>(weapons[i]);
            return weapon.GetComponent<WeaponBehaviour>().Save();
        }
        return null;
    }

    private void GiveWeapon(WeaponData weapon, GameObject receiver)
    {
        weapon.unlimitedAmmo = true;
        receiver.GetComponent<HumanoidBehaviour>().SetItemActive(weapon);
    }

    private List<ItemData> GetRandomLoot(LootTier tier, bool weaponsEnabled, bool armorsEnabled)
    {
        List<ItemData> loot = new List<ItemData>();
        switch (tier)
        {
            case LootTier.small:
                {
                    // Moderate chance for lowest quality item + small amount of scrap
                    if (Random.value < 0.7) loot.Add(GetRandomItem(ItemTier.weak, false, false, true, 7, 16));
                    if (weaponsEnabled && Random.value < 0.45) loot.Add(GetRandomItem(ItemTier.weak, true, false, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.3) loot.Add(GetRandomItem(ItemTier.weak, false, true, false, 1, 1));
                    loot.Add(GetScrapData(Random.Range(12, 26)));
                    break;
                }
            case LootTier.medium:
                {
                    // High chance for low quality item or small chance for medium item + reasonable scrap
                    if (Random.value < 0.95) loot.Add(GetRandomItem(ItemTier.weak, false, false, true, 7, 16));
                    if (Random.value < 0.35) loot.Add(GetRandomItem(ItemTier.medium, false, false, true, 4, 7));
                    if (weaponsEnabled && Random.value < 0.8) loot.Add(GetRandomItem(ItemTier.weak, true, false, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.6) loot.Add(GetRandomItem(ItemTier.weak, false, true, false, 1, 1));
                    loot.Add(GetScrapData(Random.Range(28, 82)));
                    break;
                }
            case LootTier.large:
                {
                    // Small chance for epic item, good chance for medium item or multiple small items + good scrap
                    if (Random.value < 0.95) loot.Add(GetRandomItem(ItemTier.weak, false, false, true, 15, 32));
                    if (Random.value < 0.85) loot.Add(GetRandomItem(ItemTier.medium, false, false, true, 5, 12));
                    if (Random.value < 0.45) loot.Add(GetRandomItem(ItemTier.strong, false, false, true, 2, 4));
                    if (weaponsEnabled && Random.value < 0.9) loot.Add(GetRandomItem(ItemTier.weak, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.4) loot.Add(GetRandomItem(ItemTier.weak, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.5) loot.Add(GetRandomItem(ItemTier.medium, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.25) loot.Add(GetRandomItem(ItemTier.strong, true, false, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.6) loot.Add(GetRandomItem(ItemTier.medium, false, true, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.2) loot.Add(GetRandomItem(ItemTier.strong, false, true, false, 1, 1));
                    loot.Add(GetScrapData(Random.Range(125, 362)));
                    break;
                }
            case LootTier.massive:
                {
                    // Tons of scrap + multiple epic and medium items
                    if (Random.value < 0.95) loot.Add(GetRandomItem(ItemTier.medium, false, false, true, 5, 12));
                    if (Random.value < 0.85) loot.Add(GetRandomItem(ItemTier.strong, false, false, true, 6, 9));
                    if (Random.value < 0.35) loot.Add(GetRandomItem(ItemTier.strong, false, false, true, 6, 9));
                    if (weaponsEnabled && Random.value < 0.9) loot.Add(GetRandomItem(ItemTier.medium, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.5) loot.Add(GetRandomItem(ItemTier.medium, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.25) loot.Add(GetRandomItem(ItemTier.medium, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.9) loot.Add(GetRandomItem(ItemTier.strong, true, false, false, 1, 1));
                    if (weaponsEnabled && Random.value < 0.4) loot.Add(GetRandomItem(ItemTier.strong, true, false, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.8) loot.Add(GetRandomItem(ItemTier.strong, false, true, false, 1, 1));
                    if (armorsEnabled && Random.value < 0.4) loot.Add(GetRandomItem(ItemTier.strong, false, true, false, 1, 1));
                    loot.Add(GetScrapData(Random.Range(425, 1204)));
                    break;
                }
        }
        return loot;
    }

    private ItemData GetRandomItem(ItemTier tier, bool weapons, bool armors, bool items, int minQuantity, int maxQuantity)
    {
        // Collect list of all possible loot items
        List<string> lootPossible = new List<string>();

        // Get weapons at this tier
        if (weapons)
        {
            List<string> tierWeapons;
            ItemLibrary.weaponTierDictionary.TryGetValue(tier, out tierWeapons);
            if (tierWeapons != null) lootPossible.Concat(tierWeapons);
        }

        // Get armors at this tier
        if (armors)
        {
            List<string> tierArmors;
            ItemLibrary.armorTierDictionary.TryGetValue(tier, out tierArmors);
            if (tierArmors != null) lootPossible.Concat(tierArmors);
        }

        // Get items at this tier
        if(items)
        {
            List<string> tierItems;
            ItemLibrary.itemTierDictionary.TryGetValue(tier, out tierItems);
            if (tierItems != null) lootPossible.Concat(tierItems);
        }

        // Get random loot from possible items
        if (lootPossible.Count == 0) return null;
        int i = Random.Range(0, lootPossible.Count - 1);
        GameObject loot = Resources.Load<GameObject>(lootPossible[i]);

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

    private ItemData GetScrapData(int value)
    {
        GameObject scrap = Resources.Load<GameObject>(ItemLibrary.SCRAP_PATH);
        ItemData data = scrap.GetComponent<ItemBehaviour>().Save();
        data.value = value;
        return data;
    }

}