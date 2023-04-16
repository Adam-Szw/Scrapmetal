using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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

    public static void LoadItemLocalization(string filename)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Localization/" + filename);
        ItemLoadList itemLoadList = JsonUtility.FromJson<ItemLoadList>(textAsset.text);
        foreach (ItemLocalData load in itemLoadList.itemLocalData) itemLocalization.Add(load.id, load.text);
    }

    // Tells the game to load item resources
    public static void LoadItems()
    {
        for (int i = 0; i < ITEM_RESOURCES.Length; i++) Resources.Load(ITEM_PREFABS_PATH + ITEM_RESOURCES[i]);
    }

}
