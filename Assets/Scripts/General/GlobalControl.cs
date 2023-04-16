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

    private static readonly Dictionary<Type, Action<EntityData>> SpawnMethods = new Dictionary<Type, Action<EntityData>>()
    {
        { typeof(PlayerData), d => PlayerBehaviour.Spawn((PlayerData)d) },
        { typeof(NPCData), d => NPCBehaviour.Spawn((NPCData)d) },
        { typeof(HumanoidData), d => HumanoidBehaviour.Spawn((HumanoidData)d) },
        { typeof(SpiderbotData), d => SpiderbotBehaviour.Spawn((SpiderbotData)d) },
        { typeof(TankbotData), d => TankbotBehaviour.Spawn((TankbotData)d) },
        { typeof(ZapperData), d => ZapperBehaviour.Spawn((ZapperData)d) },
        { typeof(CreatureData), d => CreatureBehaviour.Spawn((CreatureData)d) },
        { typeof(ArmorData), d => ArmorBehaviour.Spawn((ArmorData)d) },
        { typeof(WeaponData), d => WeaponBehaviour.Spawn((WeaponData)d) },
        { typeof(ProjectileData), d => ProjectileBehaviour.Spawn((ProjectileData)d) },
        { typeof(UsableData), d => UsableBehaviour.Spawn((UsableData)d) },
        { typeof(AmmoData), d => AmmoBehaviour.Spawn((AmmoData)d) },
        { typeof(ItemData), d => ItemBehaviour.Spawn((ItemData)d) },
        { typeof(ContainerData), d => ContainerBehaviour.Spawn((ContainerData)d) },
        { typeof(EntityData), d => EntityBehaviour.Spawn(d) }
    };

    // Global variables
    public static Camera currentCamera;
    public static CameraControl cameraControl;

    [HideInInspector] public static ulong nextID = 1;

    private static Transform playerTransform;
    private static GameObject player;

    public static bool paused { get; private set; }
    public static bool trackingPlayer = true;

    void Awake()
    {
        // Setup main camera
        currentCamera = Camera.main;
        cameraControl = new CameraControl();
        cameraControl.currentCamera = currentCamera;
        PlayerInput.currCamera = currentCamera;
        // Load dialog localization
        DialogLibrary.LoadDialogLocalization("DialogText_EN");
        DialogLibrary.LoadDialogOptions();
        ItemLibrary.LoadItemLocalization("ItemDescriptions_EN");
    }

    void Start()
    {
        paused = false;
        SetPlayer(HelpFunc.FindPlayerInScene());
        // Open standard UI
        UIControl.ShowCombatUI();
    }

    void Update()
    {
        if (trackingPlayer)
        {
            if (!playerTransform) SetPlayer(HelpFunc.FindPlayerInScene());
            if (!paused) cameraControl.AdjustCameraToPlayer();
        }
    }

    public static Transform GetPlayerTransform() { return playerTransform; }

    public static GameObject GetPlayer() { return player; }

    private static void SetPlayer(GameObject player)
    {
        GlobalControl.player = player;
        if (!player)
        {
            playerTransform = null;
            cameraControl.playerTransform = null;
            PlayerDeadActions();
            return;
        }
        playerTransform = player.transform;
        cameraControl.playerTransform = player.transform;
    }

    public static void PlayerDeadActions()
    {
        if (trackingPlayer)
        {
            trackingPlayer = false;
            UIControl.ShowMenu();
            UIControl.ShowPopup("Game Over!", "", 0f);
        }
    }

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
            playerTransform = null;
            DestroyEntities(sceneCurr);
            // Load entities using save file
            LoadEntities(scene.entities);
            SetPlayer(HelpFunc.FindPlayerInScene());
            cameraControl.Load(scene.cameraData);
        }
        UIControl.ShowCombatUI();
        nextID = save.nextID;
    }

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
            TextMarker tm = obj.GetComponent<TextMarker>();
            if (tm) Destroy(obj);
        }
    }

}
