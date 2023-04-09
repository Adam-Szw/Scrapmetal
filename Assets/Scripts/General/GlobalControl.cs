using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using Scene = UnityEngine.SceneManagement.Scene;

/* This class is in charge of global variables and functionality such as pausing the game
 */
public class GlobalControl : MonoBehaviour
{
    // Global variables
    public static int projectileSortLayer = 20; // sorting layer for all projectiles in scene

    public static Camera currentCamera;
    public static CameraControl cameraControl;

    [HideInInspector] public static ulong nextID = 1;

    private static GameObject player = null;

    public static bool paused { get; private set; }

    void Awake()
    {
        currentCamera = Camera.main;
        cameraControl = new CameraControl();
        cameraControl.currentCamera = currentCamera;
        player = HelpFunc.FindPlayerInScene();
    }

    void Start()
    {
        // These 2 collision layers refer to entity and wall colliders
        Physics2D.IgnoreLayerCollision(0, 6);
        paused = false;
    }

    void Update()
    {
        if (!player) player = player = HelpFunc.FindPlayerInScene();
        if (!paused) cameraControl.AdjustCameraToPlayer();
    }

    public static GameObject GetPlayer() { return player; }

    public static void PauseGame()
    {
        Time.timeScale = 0.0f;
        paused = true;
    }

    public static void UnpauseGame()
    {
        Time.timeScale = 1.0f;
        paused = false;
    }

    public static void Save()
    {
        PauseGame();
        // Save current scene
        string sceneCurr = SceneManager.GetActiveScene().name;
        List<SceneData> sceneData = new List<SceneData>();
        sceneData.Add(new SceneData(sceneCurr, cameraControl.Save(), SaveEntities(sceneCurr)));
        Save save = new Save(sceneCurr, nextID, sceneData);

        // Save to the folder
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/testingSave";
        FileStream fstream = new FileStream(path, FileMode.Create);
        formatter.Serialize(fstream, save);
        fstream.Close();
    }

    public static void Load()
    {
        PauseGame();
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + "/testingSave";
        FileStream fstream = new FileStream(path, FileMode.Open);
        Save save;
        save = formatter.Deserialize(fstream) as Save;
        fstream.Close();
        string sceneCurr = SceneManager.GetActiveScene().name;
        SceneData scene = null;
        foreach (SceneData s in save.scenes)
        {
            if (s.name == sceneCurr)
            {
                scene = s;
                break;
            }
        }
        if (scene != null)
        {
            // Destroy entities in current scene
            player = null;
            DestroyEntities(sceneCurr);
            // Load entities using save file
            LoadEntities(scene.entities);
            player = HelpFunc.FindPlayerInScene();
            cameraControl.Load(scene.cameraData);
        }
        nextID = save.nextID;
    }

    private static readonly Dictionary<Type, Action<EntityData>> SpawnMethods = new Dictionary<Type, Action<EntityData>>()
    {
        { typeof(PlayerData), d => PlayerBehaviour.Spawn((PlayerData)d) },
        { typeof(HumanoidData), d => HumanoidBehaviour.Spawn((HumanoidData)d) },
        { typeof(SpiderbotData), d => SpiderbotBehaviour.Spawn((SpiderbotData)d) },
        { typeof(CreatureData), d => CreatureBehaviour.Spawn((CreatureData)d) },
        { typeof(WeaponData), d => WeaponBehaviour.Spawn((WeaponData)d) },
        { typeof(ProjectileData), d => ProjectileBehaviour.Spawn((ProjectileData)d) },
        { typeof(ObjectData), d => ObjectBehaviour.Spawn((ObjectData)d) },
        { typeof(EntityData), d => EntityBehaviour.Spawn(d) }
    };

    private static List<EntityData> SaveEntities(string sceneName)
    {
        List<EntityData> entities = new List<EntityData>();
        Scene scene = SceneManager.GetSceneByName(sceneName);
        List<GameObject> objects = scene.GetRootGameObjects().ToList();
        foreach (GameObject obj in objects)
        {
            EntityBehaviour b = obj.GetComponent<EntityBehaviour>();
            if (b)
            {
                MethodInfo saveMethod = b.GetType().GetMethod("Save");
                entities.Add((EntityData)saveMethod.Invoke(b, null));
            }
        }
        return entities;
    }

    private static void LoadEntities(List<EntityData> data)
    {
        foreach (EntityData d in data)
        {
            if (SpawnMethods.TryGetValue(d.GetType(), out var spawnMethod)) spawnMethod(d);
        }
    }

    /* Destroy all entities in a given scene
     */
    private static void DestroyEntities(string scene)
    {
        List<GameObject> objects = SceneManager.GetSceneByName(scene).GetRootGameObjects().ToList();
        foreach (GameObject obj in objects)
        {
            EntityBehaviour eB = obj.GetComponent<EntityBehaviour>();
            if (eB) Destroy(obj);
        }
    }

}
