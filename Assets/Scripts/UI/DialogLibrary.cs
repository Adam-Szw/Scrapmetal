using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class DialogLibrary
{
    public struct DialogLocal
    {
        public string text;
        public string response;

        public DialogLocal(string text, string response)
        {
            this.text = text;
            this.response = response;
        }
    }

    [System.Serializable]
    public class DialogData
    {
        public ulong id;
        public string text;
        public string response;
    }

    [System.Serializable]
    public class DialogLoadList
    {
        public List<DialogData> dialogData;
    }

    public static Dictionary<ulong, DialogLocal> dialogLocalization = new Dictionary<ulong, DialogLocal>();
    public static List<DialogOption> options = new List<DialogOption>();

    public static void LoadDialogLocalization(string filename)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Localization/" + filename);
        DialogLoadList dialogLoadList = JsonUtility.FromJson<DialogLoadList>(textAsset.text);
        foreach (DialogData load in dialogLoadList.dialogData) dialogLocalization.Add(load.id, new DialogLocal(load.text, load.response));
    }

    public static DialogOption getDialogOptionByID(float ID)
	{
		foreach(DialogOption o in options) { if (o.ID == ID) return o; }
		return null;
	}

    public static string GetDialogOptionText(ulong link)
    {
        DialogLocal data;
        if (!dialogLocalization.TryGetValue(link, out data)) return "";
        else return data.text;
    }

    public static string GetDialogOptionResponse(ulong link)
    {
        DialogLocal data;
        if (!dialogLocalization.TryGetValue(link, out data)) return "";
        else return data.response;
    }

    // All of dialog logic is contained here. This could potentially be moved to a JSON file if i had more time
    public static void LoadDialogOptions()
    {
        //dialog test
        DialogOption o = new DialogOption(1, new List<ulong>() { 2 });
        options.Add(o);
        o = new DialogOption(2, new List<ulong>());
        options.Add(o);
    }

}
