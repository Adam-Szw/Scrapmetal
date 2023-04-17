using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ContentGenerator;
using static ItemLibrary;

public static class ItemLibrary
{
    [System.Serializable]
    public class ItemLocalData
    {
        public ulong id;
        public string text;
    }

    [System.Serializable]
    public class ItemLoadList
    {
        public List<ItemLocalData> itemLocalData;
    }

    public static Dictionary<ulong, string> itemLocalization = new Dictionary<ulong, string>();
    private static string ITEM_PREFABS_PATH = "Prefabs/Items/";
    // to fill
    private static string[] ITEM_RESOURCES = new string[] { "FixKit", "Scrap", "Ammo/AmmoRivetgun" };

    public static Dictionary<ItemTier, List<string>> weaponTierDictionary = new Dictionary<ItemTier, List<string>>();
    public static Dictionary<ItemTier, List<string>> armorTierDictionary = new Dictionary<ItemTier, List<string>>();
    public static Dictionary<ItemTier, List<string>> itemTierDictionary = new Dictionary<ItemTier, List<string>>();
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

}
