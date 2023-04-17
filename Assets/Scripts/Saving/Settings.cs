using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[Serializable]
public class Settings
{
    public static string SETTINGS_FILE_PATH = "/SessionSettings";

    [Serializable]
    public struct SaveInfo
    {
        public string date;
        public string location;

        public SaveInfo(string date, string location)
        {
            this.date = date;
            this.location = location;
        }
    }

    public Dictionary<int, SaveInfo> saves = new Dictionary<int, SaveInfo>();


    public static Settings GetSettings()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + SETTINGS_FILE_PATH;
        try
        {
            FileStream fstream = new FileStream(path, FileMode.Open);
            Settings save = formatter.Deserialize(fstream) as Settings;
            fstream.Close();
            return save;
        }
        catch { return new Settings(); }
    }

    public static void SaveSettings(Settings settings)
    {
        // Save to the folder
        BinaryFormatter formatter = new BinaryFormatter();
        string path = Application.persistentDataPath + SETTINGS_FILE_PATH;
        FileStream fstream = new FileStream(path, FileMode.Create);
        formatter.Serialize(fstream, settings);
        fstream.Close();
    }


}
