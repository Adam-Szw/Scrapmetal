using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


    public GameObject player;
    public Camera currentCamera;

    public static CameraControl cameraControl;

    public static bool paused { get; private set; }

    void Awake()
    {
        cameraControl = new CameraControl();
        cameraControl.player = player;
        cameraControl.currentCamera = currentCamera;
    }

    void Start()
    {
        // These 2 collision layers refer to entity and wall colliders
        Physics2D.IgnoreLayerCollision(0, 6);
        paused = false;
    }

    void Update()
    {
        if (!paused) cameraControl.AdjustCameraToPlayer();
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
        // Build save file
        Save save = new Save();
        // Save current scene
        string sceneCurr = SceneManager.GetActiveScene().name;
        save.currentScene = sceneCurr;
        save.playerData = GetPlayerData(sceneCurr);
        save.scenes.Add(GetSceneData(sceneCurr));

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
            cameraControl.Load(scene.cameraData);
            // Destroy entities in current scene
            DestroyEntities(sceneCurr);
            // Load entities using save file
            LoadEntities(scene, save.playerData);
        }
        UnpauseGame();

    }

    private static PlayerData GetPlayerData(string sceneName)
    {
        PlayerData playerData = null;
        // Find the player object in top layer of the scene
        List<GameObject> objects = SceneManager.GetSceneByName(sceneName).GetRootGameObjects().ToList();
        foreach (GameObject obj in objects)
        {
            PlayerBehaviour pB = obj.GetComponent<PlayerBehaviour>();
            if (pB) playerData = pB.Save();
        }
        return playerData;
    }

    /* Builds save data for currently active scene
     */
    private static SceneData GetSceneData(string name)
    {
        SceneData sceneData = new SceneData();
        sceneData.name = name;
        sceneData.cameraData = cameraControl.Save();
        Scene scene = SceneManager.GetSceneByName(name);
        List<GameObject> objects = scene.GetRootGameObjects().ToList();
        // Go over all objects present in top layer of the scene, searching for various saveable entities
        foreach(GameObject obj in objects)
        {
            // Save all non-player humanoid characters
            HumanoidBehaviour hB = obj.GetComponent<HumanoidBehaviour>();
            PlayerBehaviour pB = obj.GetComponent<PlayerBehaviour>();
            if (hB && !pB) sceneData.humanoids.Add(hB.Save());
            // Save all bullets
            ProjectileBehaviour bB = obj.GetComponent<ProjectileBehaviour>();
            if (bB) sceneData.bullets.Add(bB.Save());

        }
        return sceneData;
    }

    /* Destroy all entities in a given scene
     */
    private static void DestroyEntities(string scene)
    {
        List<GameObject> objects = SceneManager.GetSceneByName(scene).GetRootGameObjects().ToList();
        foreach (GameObject obj in objects)
        {
            // Destroy humanoids
            HumanoidBehaviour hB = obj.GetComponent<HumanoidBehaviour>();
            if (hB) Destroy(obj);
            // Destroy bullets
            ObjectBehaviour bB = obj.GetComponent<ObjectBehaviour>();
            if (bB) Destroy(obj);
        }
    }

    /* Load entities in current scene state using save file
     */
    private static void LoadEntities(SceneData data, PlayerData playerData)
    {
        GameObject player = Instantiate(Resources.Load<GameObject>(PlayerBehaviour.PREFAB_PATH));
        player.GetComponent<PlayerBehaviour>().Load(playerData);
        cameraControl.player = player;
        foreach (HumanoidData hD in data.humanoids)
        {
            HumanoidBehaviour.Spawn(hD);
        }
        foreach (ProjectileData bB in data.bullets)
        {
            ProjectileBehaviour.Spawn(bB);
        }
    }
}
