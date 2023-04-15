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
    public static List<ItemData> items = new List<ItemData>();

    public static void LoadItemLocalization(string filename)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Localization/" + filename);
        ItemLoadList itemLoadList = JsonUtility.FromJson<ItemLoadList>(textAsset.text);
        foreach (ItemLocalData load in itemLoadList.itemLocalData) itemLocalization.Add(load.id, load.text);
    }

    public static void LoadItems()
    {

    }

}
