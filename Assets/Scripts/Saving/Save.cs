using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/* This class contains total serialized data of a save. This includes state of all scenes
 * as well as playthrough information that we might want to track
 */
[Serializable]
public class Save
{
    public static string SAVE_PATH = "/Save";

    public string currentScene = "";
    public Decisions decisions = new Decisions();
    public Dictionary<string, SceneData> scenes = new Dictionary<string, SceneData>();

    public static void StoreSave(Save save, int index)
    {
        // Save to the folder
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + SAVE_PATH + index;
        FileStream fstream = new FileStream(path, FileMode.Create);
        formatter.Serialize(fstream, save);
        fstream.Close();
    }

    public static Save LoadSave(int index)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + SAVE_PATH + index;
        try
        {
            FileStream fstream = new FileStream(path, FileMode.Open);
            Save save = formatter.Deserialize(fstream) as Save;
            fstream.Close();
            return save;
        }
        catch { return null; }
    }
}
