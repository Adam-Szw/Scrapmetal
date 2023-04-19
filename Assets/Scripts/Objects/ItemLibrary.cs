using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ContentGenerator;
using static ItemLibrary;
using static UnityEditor.Progress;

public static class ItemLibrary
{
    [Serializable]
    public class ItemLocalData
    {
        public int id;
        public string text;
    }

    [Serializable]
    public class ItemLoadList
    {
        public List<ItemLocalData> itemLocalData;
    }

    public static Dictionary<int, string> itemLocalization = new Dictionary<int, string>();
    public static string ITEM_PREFABS_PATH = "Prefabs/Items/";
    // to fill
    public static string[] ITEM_RESOURCES = new string[] { "FixKit", "Scrap", "Ammo/RivetAmmo", "Ammo/CapacitorCharge",
        "Ammo/GrenadeAmmo", "Ammo/RocketsAmmo", "Ammo/TranquilizerDartAmmo", "Weapons/Rivetgun", "Weapons/Laser",
        "Weapons/MissileLauncher", "Weapons/Taser", "Weapons/Tranquilizer", "Weapons/Tube", "Weapons/MissileLauncherSpiderbot",
        "Weapons/RifleTankbot", "Weapons/ZapperWeapon", "Armors/ArmorArmsArmored", "Armors/ArmorArmsActuator", "Armors/ArmorArmsSensoric",
        "Armors/ArmorLegsArmored", "Armors/ArmorLegsActuator", "Armors/ArmorLegsSensoric", "Armors/ArmorHeadArmored",
        "Armors/ArmorHeadActuator", "Armors/ArmorHeadSensoric", "Armors/ArmorBodyArmored", "Armors/ArmorBodyActuator",
        "Armors/ArmorBodySensoric" };

    public static Dictionary<ItemTier, List<string>> weaponTierDictionary = new Dictionary<ItemTier, List<string>>();
    public static Dictionary<ItemTier, List<string>> armorTierDictionary = new Dictionary<ItemTier, List<string>>();
    public static Dictionary<ItemTier, List<string>> itemTierDictionary = new Dictionary<ItemTier, List<string>>();
    public static Dictionary<int, List<ItemData>> shopInventories = new Dictionary<int, List<ItemData>>();
    public static string SCRAP_PATH = "Prefabs/Items/Scrap";

    public static void LoadItemLocalization(string filename)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Localization/" + filename);
        ItemLoadList itemLoadList = JsonUtility.FromJson<ItemLoadList>(textAsset.text);
        foreach (ItemLocalData load in itemLoadList.itemLocalData) itemLocalization[load.id] = load.text;
    }

    public static void LoadItems()
    {
        for (int i = 0; i < ITEM_RESOURCES.Length; i++)
        {
            GameObject obj = Resources.Load<GameObject>(ITEM_PREFABS_PATH + ITEM_RESOURCES[i]);
            ItemBehaviour b = obj.GetComponent<ItemBehaviour>();
            ItemTier tier = b.tier;
            if (b is WeaponBehaviour)
            {
                if (!weaponTierDictionary.ContainsKey(tier)) weaponTierDictionary[tier] = new List<string>();
                weaponTierDictionary[tier].Add(ITEM_RESOURCES[i]);
            }
            else if (b is ArmorBehaviour)
            {
                if (!armorTierDictionary.ContainsKey(tier)) armorTierDictionary[tier] = new List<string>();
                armorTierDictionary[tier].Add(ITEM_RESOURCES[i]);
            }
            else
            {
                if (!itemTierDictionary.ContainsKey(tier)) itemTierDictionary[tier] = new List<string>();
                itemTierDictionary[tier].Add(ITEM_RESOURCES[i]);
            }
        }
    }

    public static List<ItemData> GetDefaultShopGyro()
    {
        List<ItemData> items = new List<ItemData>();
        try
        {
            items.Add(GetItemDataFromStr("Weapons/Rivetgun"));
            items.Add(GetItemDataFromStr("Weapons/Laser"));
            items.Add(GetItemDataFromStr("Weapons/Taser"));
            items.Add(GetItemDataFromStr("Weapons/Tube"));
            items.Add(GetItemDataFromStr("Weapons/MissileLauncher"));
            items.Add(GetItemDataFromStr("Armors/ArmorArmsArmored"));
            items.Add(GetItemDataFromStr("Armors/ArmorArmsActuator"));
            items.Add(GetItemDataFromStr("Armors/ArmorLegsArmored"));
            items.Add(GetItemDataFromStr("Armors/ArmorLegsActuator"));
            items.Add(GetItemDataFromStr("Armors/ArmorHeadArmored"));
            items.Add(GetItemDataFromStr("Armors/ArmorHeadActuator"));
            items.Add(GetItemDataFromStr("Armors/ArmorBodyArmored"));
            items.Add(GetItemDataFromStr("Armors/ArmorBodyActuator"));
            items.Add(GetItemDataFromStr("Ammo/RivetAmmo"));
            items.Add(GetItemDataFromStr("Ammo/RivetAmmo"));
            items.Add(GetItemDataFromStr("Ammo/RivetAmmo"));
            items.Add(GetItemDataFromStr("Ammo/RivetAmmo"));
            items.Add(GetItemDataFromStr("Ammo/RivetAmmo"));
            items.Add(GetItemDataFromStr("Ammo/RivetAmmo"));
            items.Add(GetItemDataFromStr("Ammo/CapacitorCharge"));
            items.Add(GetItemDataFromStr("Ammo/CapacitorCharge"));
            items.Add(GetItemDataFromStr("Ammo/CapacitorCharge"));
            items.Add(GetItemDataFromStr("Ammo/CapacitorCharge"));
            items.Add(GetItemDataFromStr("Ammo/GrenadeAmmo"));
            items.Add(GetItemDataFromStr("Ammo/GrenadeAmmo"));
            items.Add(GetItemDataFromStr("Ammo/RocketsAmmo"));
            items.Add(GetItemDataFromStr("Ammo/RocketsAmmo"));
            items.Add(GetItemDataFromStr("Ammo/TranquilizerDartAmmo"));
        } catch
        {
            Debug.Log("Inventory failed to load for Gyro");
        }
        return items;
    }

    public static List<ItemData> GetDefaultShopJesse()
    {
        List<ItemData> items = new List<ItemData>();
        try
        {
            for (int i = 0; i < 30; i++) items.Add(GetItemDataFromStr("FixKit"));
        }
        catch
        {
            Debug.Log("Inventory failed to load for Jesse");
        }
        return items;
    }

    private static ItemData GetItemDataFromStr(string path)
    {
        try
        {
            GameObject obj = Resources.Load<GameObject>(ITEM_PREFABS_PATH + path);
            if (obj.GetComponent<WeaponBehaviour>() != null) return obj.GetComponent<WeaponBehaviour>().Save();
            if (obj.GetComponent<ArmorBehaviour>() != null) return obj.GetComponent<ArmorBehaviour>().Save();
            if (obj.GetComponent<AmmoBehaviour>() != null) return obj.GetComponent<AmmoBehaviour>().Save();
            if (obj.GetComponent<UsableBehaviour>() != null) return obj.GetComponent<UsableBehaviour>().Save();
            if (obj.GetComponent<ItemBehaviour>() != null) return obj.GetComponent<ItemBehaviour>().Save();
        }
        catch
        {
            Debug.Log("Inventory failed to load item: " + path);
        }
        return null;
    }
}