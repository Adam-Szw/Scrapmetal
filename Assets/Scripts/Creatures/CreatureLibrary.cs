using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ContentGenerator;

public class CreatureLibrary
{

    public static string CREATURES_PREFAB_PATH = "Prefabs/Creatures/";
    public static string[] CREATURE_RESOURCES = new string[] { "NPC", "Player", "Spiderbot", "Tankbot", "Zapper" };

    public static Dictionary<CreatureTier, List<string>> tierDictionary = new Dictionary<CreatureTier, List<string>>();

    public static void LoadCreatures()
    {
        for (int i = 0; i < CREATURE_RESOURCES.Length; i++)
        {
            GameObject obj = Resources.Load<GameObject>(CREATURES_PREFAB_PATH + CREATURE_RESOURCES[i]);
            CreatureTier tier = obj.GetComponent<CreatureBehaviour>().tier;
            if (!tierDictionary.ContainsKey(tier)) tierDictionary[tier] = new List<string>();
            tierDictionary[tier].Add(CREATURE_RESOURCES[i]);
        }
    }

}