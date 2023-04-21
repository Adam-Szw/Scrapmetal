using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ContentGenerator;

/* Stores loaded assets about the creatures. Also used by random content generation
 */
public class CreatureLibrary
{
    public static string CREATURES_PREFAB_PATH = "Prefabs/Creatures/";
    public static string[] CREATURE_RESOURCES = new string[] { "NPC", "NPC_Enemy_Easy", "NPC_Enemy_Medium", "NPC_Enemy_Hard", "Player", "Spiderbot", "Tankbot", "Zapper" };

    public static Dictionary<CreatureTier, List<string>> tierDictionary = new Dictionary<CreatureTier, List<string>>();

    public struct BodyColorPreset
    {
        public float[] bodyColor;
        public float[] headColor;
        public float[] armsColor;
        public float[] legsColor;

        public BodyColorPreset(float[] torsoColor, float[] headColor, float[] armsColor, float[] legsColor)
        {
            this.bodyColor = torsoColor;
            this.headColor = headColor;
            this.armsColor = armsColor;
            this.legsColor = legsColor;
        }
    }

    public static List<BodyColorPreset> HUMANOID_BODY_COLOR_PRESETS = new List<BodyColorPreset>();

    public static void LoadCreatures()
    {
        // Load resources
        for (int i = 0; i < CREATURE_RESOURCES.Length; i++)
        {
            GameObject obj = Resources.Load<GameObject>(CREATURES_PREFAB_PATH + CREATURE_RESOURCES[i]);
            CreatureTier tier = obj.GetComponent<CreatureBehaviour>().tier;
            if (!tierDictionary.ContainsKey(tier)) tierDictionary[tier] = new List<string>();
            tierDictionary[tier].Add(CREATURE_RESOURCES[i]);
        }

        // Load color presets
        // White/grey
        HUMANOID_BODY_COLOR_PRESETS.Add(new BodyColorPreset(
            new float[] { 1f, 1f, 1f, 1f }, new float[] { 1f, 1f, 1f, 1f }, new float[] { 1f, 1f, 1f, 1f }, new float[] { 1f, 1f, 1f, 1f }));
        // Black
        HUMANOID_BODY_COLOR_PRESETS.Add(new BodyColorPreset(
            new float[] { 0.52f, 0.52f, 0.52f, 1f }, new float[] { 0.63f, 0.63f, 0.63f, 1f }, new float[] { 0.59f, 0.59f, 0.59f, 1f }, new float[] { 0.48f, 0.48f, 0.48f, 1f }));
        // Green
        HUMANOID_BODY_COLOR_PRESETS.Add(new BodyColorPreset(
            new float[] { 0.35f, 0.52f, 0.4f, 1f }, new float[] { 0.33f, 0.58f, 0.4f, 1f }, new float[] { 0.27f, 0.45f, 0.33f, 1f }, new float[] { 0.3f, 0.47f, 0.33f, 1f }));
        // Yellow
        HUMANOID_BODY_COLOR_PRESETS.Add(new BodyColorPreset(
            new float[] { 0.61f, 0.61f, 0.29f, 1f }, new float[] { 0.74f, 0.73f, 0.41f, 1f }, new float[] { 0.72f, 0.69f, 0.35f, 1f }, new float[] { 0.62f, 0.6f, 0.28f, 1f }));
        // Blue
        HUMANOID_BODY_COLOR_PRESETS.Add(new BodyColorPreset(
            new float[] { 0.28f, 0.45f, 0.63f, 1f }, new float[] { 0.38f, 0.51f, 0.79f, 1f }, new float[] { 0.39f, 0.49f, 0.73f, 1f }, new float[] { 0.29f, 0.39f, 0.65f, 1f }));
        // Red
        HUMANOID_BODY_COLOR_PRESETS.Add(new BodyColorPreset(
            new float[] { 0.5f, 0.19f, 0.24f, 1f }, new float[] { 0.49f, 0.2f, 0.27f, 1f }, new float[] { 0.51f, 0.29f, 0.33f, 1f }, new float[] { 0.52f, 0.2f, 0.22f, 1f }));
        // Grey B
        HUMANOID_BODY_COLOR_PRESETS.Add(new BodyColorPreset(
            new float[] { 0.66f, 0.65f, 0.65f, 1f }, new float[] { 0.64f, 0.65f, 0.67f, 1f }, new float[] { 0.75f, 0.73f, 0.74f, 1f }, new float[] { 0.64f, 0.65f, 0.67f, 1f }));
        // Ocean
        HUMANOID_BODY_COLOR_PRESETS.Add(
            new BodyColorPreset(new float[] { 0.46f, 0.73f, 0.85f, 1f }, new float[] { 0.42f, 0.65f, 0.75f, 1f }, new float[] { 0.42f, 0.65f, 0.75f, 1f }, new float[] { 0.35f, 0.63f, 0.75f, 1f }));

    }

}