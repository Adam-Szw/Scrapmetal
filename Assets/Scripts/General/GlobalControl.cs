using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static PopupControl;
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
    [HideInInspector] public static Camera currentCamera = null;
    [HideInInspector] public static ulong nextID = 1;
    [HideInInspector] public static bool paused { get; private set; } = true;
    [HideInInspector] public static bool gameLoopOn = false;            // This should be false while in menu scene

    private static GameObject player = null;
    private static Transform playerTransform = null;
    private static PlayerBehaviour playerBehaviour = null;
    private static CameraControl cameraControl = null;

    // Inter-scene data
    public static Decisions decisions = new Decisions();    // Playthrough decisions
    public static bool resourcesLoaded = false;             // Set to true to prevent resources being repeatedly loaded on scene load
    public static int saveIndex = -1;                       // Index of save file that opened scenes will look for data in

    // Local data
    public bool loadOnLaunch = true;        // True if game should attempt to load the scene and start gameplay loop automatically
    public bool showStartingPopup = false;  // If true - this scene will show welcome popup at start

    private void Awake()
    {
        // Setup camera
        currentCamera = Camera.main;
        cameraControl = new CameraControl();
        cameraControl.currentCamera = currentCamera;
        // Load resources
        if (!resourcesLoaded)
        {
            DialogLibrary.LoadDialogLocalization("DialogText_EN");
            DialogLibrary.LoadDialogOptions();
            ItemLibrary.LoadItemLocalization("ItemDescriptions_EN");
            ItemLibrary.LoadItems();
            PopupLibrary.LoadPopupLocalization("PopupText_EN");
            CreatureLibrary.LoadCreatures();
        }
        // Load active scene
        if (loadOnLaunch) LoadGame(saveIndex);
        if (showStartingPopup) UIControl.ShowPopup(2, 0.2f);
    }

    private void Update()
    {
        if (gameLoopOn)
        {
            if (!player) SetPlayer(HelpFunc.FindPlayerInScene());
            if (!paused) cameraControl.AdjustCameraToPlayer();
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

    public static GameObject GetPlayer() { return player; }

    public static Transform GetPlayerTransform() { return playerTransform; }

    public static PlayerBehaviour GetPlayerBehaviour() { return playerBehaviour; }

    private static void SetPlayer(GameObject player)
    {
        GlobalControl.player = player;
        if (!player)
        {
            playerTransform = null;
            playerBehaviour = null;
            PlayerDeadActions();
            return;
        }
        playerBehaviour = player.GetComponent<PlayerBehaviour>();
        playerTransform = player.transform;
    }

    public static void PlayerDeadActions()
    {
        gameLoopOn = false;
        UIControl.DestroyCombatUI();
        UIControl.DestroyDialog();
        UIControl.DestroyPopup();
        UIControl.DestroyMenu();
        // make effect change scene to menu
        Effect effect = () => { ReturnToTitle(); };
        UIControl.ShowPopup(1, 0.05f, effect);
    }

    public static void ReturnToTitle()
    {
        saveIndex = -1;
        gameLoopOn = false;
        SwitchScene("Menu");
    }

    public static void SaveGame(int index)
    {
        PauseGame();
        Save save = new Save();

        // Get current save in slot
        Save saveCurr = Save.LoadSave(index);
        if (saveCurr != null) save = saveCurr;

        // Update save globals
        string sceneCurr = SceneManager.GetActiveScene().name;
        save.currentScene = sceneCurr;
        save.decisions = decisions;

        // Save current scene
        SceneData data = new SceneData(sceneCurr, cameraControl.Save(), SaveEntities(sceneCurr), SaveCells(sceneCurr), SaveTriggers(sceneCurr));
        save.scenes[sceneCurr] = data;

        // Record info in settings
        Settings settings = Settings.GetSettings();
        settings.saves[index] = new Settings.SaveInfo(DateTime.Now.ToString(), sceneCurr);
        Settings.SaveSettings(settings);

        // Save on disk
        Save.StoreSave(save, index);
    }

    public static void SwitchScene(string newScene)
    {
        SceneManager.LoadScene(newScene, LoadSceneMode.Single);
    }

    public static void LoadGame(int index)
    {
        PauseGame();
        // Load the save
        saveIndex = index;
        Save save = Save.LoadSave(index);

        // See if we got the save file
        if (save == null)
        {
            StartGameLoop();
            return;
        }

        // Load decisions
        decisions = save.decisions;

        // Check if we should switch to a different scene
        string sceneCurr = SceneManager.GetActiveScene().name;
        string sceneSave = save.currentScene;
        if (sceneCurr != sceneSave)
        {
            gameLoopOn = false;
            SwitchScene(sceneSave);
            return;
        }

        // See if this scene is in save file
        SceneData scene = null;
        foreach (KeyValuePair<string, SceneData> pair in save.scenes) if (pair.Key == sceneCurr) { scene = pair.Value; break; }

        // If there is data for this scene in save, load scene using it
        if (scene != null)
        {
            // Destroy entities in current scene
            DestroyEntities(sceneCurr);

            // Load scene data
            LoadEntities(scene.entities);
            LoadCells(scene.cells);
            LoadTriggers(scene.triggers);
            cameraControl.Load(scene.cameraData);

            // Setup game environment
            player = null;
            playerTransform = null;
            playerBehaviour = null;
            gameLoopOn = true;
            SetPlayer(HelpFunc.FindPlayerInScene());

            // Load save globals

        }

        StartGameLoop();
    }

    private static void StartGameLoop()
    {
        // Refresh cells
        Dictionary<int, CellBehaviour> cells = HelpFunc.GetCells();
        foreach (KeyValuePair<int, CellBehaviour> pair in cells) pair.Value.Initialize();

        // Refresh structures
        StructureBehaviour.UpdateStructures();

        // Start the game
        gameLoopOn = true;
        SetPlayer(HelpFunc.FindPlayerInScene());
        UIControl.DefaultUI();
        UnpauseGame();
    }

    private static List<EntityData> SaveEntities(string sceneName)
    {
        // First - tell cells to enable all objects so they can be saved
        Dictionary<int, CellBehaviour> cells = HelpFunc.GetCells();
        foreach (KeyValuePair<int, CellBehaviour> pair in cells)
        {
            pair.Value.ActivateEntitiesInCell();
            pair.Value.initialized = false;
        }

        // Get all entities in scene
        List<EntityData> entities = new List<EntityData>();
        Scene scene = SceneManager.GetSceneByName(sceneName);
        List<GameObject> objects = scene.GetRootGameObjects().ToList();
        foreach (GameObject obj in objects)
        {
            EntityBehaviour b = obj.GetComponent<EntityBehaviour>();
            if (b)
            {
                // Save each entity
                MethodInfo saveMethod = b.GetType().GetMethod("Save");
                entities.Add((EntityData)saveMethod.Invoke(b, null));
            }
        }

        // Refresh cells
        cells = HelpFunc.GetCells();
        foreach (KeyValuePair<int, CellBehaviour> pair in cells) pair.Value.Initialize();

        return entities;
    }

    private static List<CellData> SaveCells(string sceneName)
    {
        List<CellData> cells = new List<CellData>();
        Scene scene = SceneManager.GetSceneByName(sceneName);
        List<GameObject> objects = scene.GetRootGameObjects().ToList();
        foreach (GameObject obj in objects)
        {
            CellBehaviour b = obj.GetComponent<CellBehaviour>();
            if (b)
            {
                MethodInfo saveMethod = b.GetType().GetMethod("Save");
                cells.Add((CellData)saveMethod.Invoke(b, null));
            }
        }
        return cells;
    }

    private static List<TriggerData> SaveTriggers(string sceneName)
    {
        List<TriggerData> triggers = new List<TriggerData>();
        Scene scene = SceneManager.GetSceneByName(sceneName);
        List<GameObject> objects = scene.GetRootGameObjects().ToList();
        foreach (GameObject obj in objects)
        {
            TriggerBehaviour b = obj.GetComponent<TriggerBehaviour>();
            if (b)
            {
                MethodInfo saveMethod = b.GetType().GetMethod("Save");
                triggers.Add((TriggerData)saveMethod.Invoke(b, null));
            }
        }
        return triggers;
    }

    private static void LoadEntities(List<EntityData> data)
    {
        foreach (EntityData d in data)
        {
            if (SpawnMethods.TryGetValue(d.GetType(), out var spawnMethod)) spawnMethod(d);
        }
    }

    private static void LoadCells(List<CellData> data)
    {
        Dictionary<int, CellBehaviour> cells = HelpFunc.GetCells();
        foreach (CellData cellData in data)
        {
            CellBehaviour cell = null;
            cells.TryGetValue(cellData.id, out cell);
            if (cell) cell.Load(cellData);
        }
    }

    private static void LoadTriggers(List<TriggerData> data)
    {
        Dictionary<int, TriggerBehaviour> triggers = HelpFunc.GetTriggers();
        foreach (TriggerData trigger in data)
        {
            TriggerBehaviour triggerBehaviour = null;
            triggers.TryGetValue(trigger.id, out triggerBehaviour);
            if (triggerBehaviour) triggerBehaviour.Load(trigger);
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
            TextBehaviour tm = obj.GetComponent<TextBehaviour>();
            if (tm) Destroy(obj);
        }
    }

}
