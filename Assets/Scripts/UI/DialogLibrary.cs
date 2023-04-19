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

    [Serializable]
    public class DialogData
    {
        public int id;
        public string text;
        public string response;
    }

    [Serializable]
    public class DialogLoadList
    {
        public List<DialogData> dialogData;
    }

    public static Dictionary<int, DialogLocal> dialogLocalization = new Dictionary<int, DialogLocal>();
    public static List<DialogOption> options = new List<DialogOption>();

    public static void LoadDialogLocalization(string filename)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Localization/" + filename);
        DialogLoadList dialogLoadList = JsonUtility.FromJson<DialogLoadList>(textAsset.text);
        foreach (DialogData load in dialogLoadList.dialogData) dialogLocalization[load.id] = new DialogLocal(load.text, load.response);
    }

    public static DialogOption getDialogOptionByID(int ID)
	{
		foreach(DialogOption o in options) { if (o.ID == ID) return o; }
		return null;
	}

    public static string GetDialogOptionText(int link)
    {
        DialogLocal data;
        if (!dialogLocalization.TryGetValue(link, out data)) return "";
        else return data.text;
    }

    public static string GetDialogOptionResponse(int link)
    {
        DialogLocal data;
        if (!dialogLocalization.TryGetValue(link, out data)) return "";
        else return data.response;
    }

    // Decisions and such are checked here and alter what should be the opening dialog for a character
    public static int GetDialogConditionedID(int initialID)
    {
        Decisions dec = GlobalControl.decisions;
        if (initialID == 1 && dec.villageWelcomeDone) return 11;
        return initialID;
    }

    // All of dialog logic is contained here. This logic could also be moved to a file if I had more time.
    public static void LoadDialogOptions()
    {
        Decisions dec = GlobalControl.decisions;
        // C-320 Dialog
        DialogOption o;
        o = new DialogOption(1, new List<int>() { 2, 4 });
        options.Add(o);
        o = new DialogOption(2, new List<int>() { 4 });
        options.Add(o);
        o = new DialogOption(4, new List<int>() { 3, 5 });
        options.Add(o);
        o = new DialogOption(3, new List<int>() { 5 });
        options.Add(o);
        o = new DialogOption(5, new List<int>() { 6 });
        options.Add(o);
        o = new DialogOption(6, new List<int>() { 10 });
        options.Add(o);
        o = new DialogOption(10, new List<int>() { 13 });
        o.effects.Add(() => {
            // Set new welcome from C-320
            dec.villageWelcomeDone = true;
        });
        options.Add(o);
        o = new DialogOption(11, new List<int>() { 7, 8, 12 });
        options.Add(o);
        o = new DialogOption(7, new List<int>() { 8, 12 });
        options.Add(o);
        o = new DialogOption(8, new List<int>() { 7, 9, 12 });
        options.Add(o);
        o = new DialogOption(9, new List<int>() { 7, 12 });
        options.Add(o);
        o = new DialogOption(12, new List<int>() { 13 });
        options.Add(o);
        o = new DialogOption(13, new List<int>());
        o.effects.Add(() => {
            UIControl.DestroyDialog();
        });
        options.Add(o);

    }

}
