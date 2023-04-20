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
        if (initialID == 1 && dec.c320GreetingDone) return 11;
        if (initialID == 14 && dec.elderQuestCompleted) return 35;
        if (initialID == 14 && dec.elderQuestFulfilled && !dec.elderQuestCompleted) return 34;
        if (initialID == 14 && dec.causedVillageTrouble && !dec.elderQuestFulfilled) return 32;
        if (initialID == 14 && dec.elderGreetingDone && !dec.elderQuestAccepted && !dec.elderQuestFulfilled && !dec.elderQuestCompleted) return 31;
        if (initialID == 14 && dec.elderGreetingDone && dec.elderQuestAccepted && !dec.elderQuestFulfilled && !dec.elderQuestCompleted) return 29;
        if (initialID == 45 && dec.o25GreetingDone) return 52;
        return initialID;
    }

    // All of dialog logic is contained here. This logic could also be moved to a file if I had more time.
    public static void LoadDialogOptions()
    {
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
        // Set new welcome from C-320
        o = new DialogOption(10, new List<int>() { 13 });
        o.effects.Add(() => {
            GlobalControl.decisions.c320GreetingDone = true;
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
        // Elder dialog
        o = new DialogOption(14, new List<int>() { 15, 16, 17 });
        options.Add(o);
        o = new DialogOption(15, new List<int>() { 16, 18, 17 });
        options.Add(o);
        o = new DialogOption(16, new List<int>() { 15, 17 });
        options.Add(o);
        o = new DialogOption(17, new List<int>() { 13 });
        options.Add(o);
        // Introduction done
        o = new DialogOption(18, new List<int>() { 19, 17 });
        o.effects.Add(() => {
            GlobalControl.decisions.elderGreetingDone = true;
        });
        options.Add(o);
        o = new DialogOption(19, new List<int>() { 20, 21, 22 });
        options.Add(o);
        o = new DialogOption(22, new List<int>() { 15, 16, 19, 17 });
        options.Add(o);
        o = new DialogOption(20, new List<int>() { 23, 22 });
        options.Add(o);
        o = new DialogOption(21, new List<int>() { 23, 22 });
        options.Add(o);
        o = new DialogOption(23, new List<int>() { 24, 17 });
        options.Add(o);
        o = new DialogOption(24, new List<int>() { 25, 26, 17 });
        options.Add(o);
        o = new DialogOption(25, new List<int>() { 26, 17 });
        options.Add(o);
        o = new DialogOption(26, new List<int>() { 27, 28 });
        options.Add(o);
        // Start quest for the Elder
        o = new DialogOption(27, new List<int>() { 17 });
        o.effects.Add(() => {
            GlobalControl.decisions.elderGreetingDone = true;
            GlobalControl.decisions.elderQuestAccepted = true;
        });
        options.Add(o);
        o = new DialogOption(28, new List<int>() { 24, 25, 26, 19, 17 });
        options.Add(o);
        o = new DialogOption(29, new List<int>() { 30 });
        options.Add(o);
        o = new DialogOption(30, new List<int>() { 17 });
        options.Add(o);
        o = new DialogOption(31, new List<int>() { 18, 19, 17 });
        options.Add(o);
        o = new DialogOption(32, new List<int>() { 33, 18, 17 });
        options.Add(o);
        o = new DialogOption(33, new List<int>() { 13 });
        options.Add(o);
        // Complete Elder quest
        o = new DialogOption(34, new List<int>() { 13 });
        o.effects.Add(() => {
            GlobalControl.decisions.elderGreetingDone = true;
            GlobalControl.decisions.elderQuestFulfilled = true;
            GlobalControl.decisions.elderQuestCompleted = true;
            ArmorData reward = Resources.Load<GameObject>(ItemLibrary.ITEM_PREFABS_PATH + "Armors/ArmorArmsSensoric").GetComponent<ArmorBehaviour>().Save();
            reward.pickable = false;
            if (GlobalControl.GetPlayer()) GlobalControl.GetPlayer().GetComponent<PlayerBehaviour>().GiveItem(reward);
        });
        options.Add(o);
        o = new DialogOption(35, new List<int>() { 36, 37 });
        options.Add(o);
        o = new DialogOption(36, new List<int>() { 17 });
        options.Add(o);
        o = new DialogOption(37, new List<int>() { 17 });
        options.Add(o);
        // Dialog with Gyro
        o = new DialogOption(38, new List<int>() { 39, 40, 42, 41 });
        options.Add(o);
        o = new DialogOption(43, new List<int>() { 39, 40, 42, 41 });
        options.Add(o);
        o = new DialogOption(39, new List<int>() { 40, 42, 41 });
        options.Add(o);
        o = new DialogOption(40, new List<int>() { 39, 42, 41 });
        options.Add(o);
        o = new DialogOption(41, new List<int>() { 13 });
        o.effects.Add(() => {
            GlobalControl.decisions.gyroGreetingDone = true;
        });
        options.Add(o);
        o = new DialogOption(42, new List<int>() { 44 });
        options.Add(o);
        // Open trade menu
        o = new DialogOption(44, new List<int>());
        o.effects.Add(() => {
            NPCBehaviour trader = UIControl.dialog.GetComponent<DialogControl>().responder;
            if (!GlobalControl.decisions.gyroShopInventoryLoaded) trader.SetInventory(ItemLibrary.GetDefaultShopGyro());
            GlobalControl.decisions.gyroShopInventoryLoaded = true;
            UIControl.DestroyDialog();
            UIControl.ShowInventory(trader.GetInventory());
        });
        options.Add(o);
        // Dialog with O25
        o = new DialogOption(45, new List<int>() { 46, 47, 48 });
        options.Add(o);
        o = new DialogOption(46, new List<int>() { 49, 50, 51, 56 });
        options.Add(o);
        o = new DialogOption(47, new List<int>() { 49, 50, 51, 56 });
        options.Add(o);
        o = new DialogOption(48, new List<int>() { 49, 50, 51, 56 });
        options.Add(o);
        o = new DialogOption(49, new List<int>() { 50, 51, 56 });
        options.Add(o);
        o = new DialogOption(50, new List<int>() { 49, 51, 56 });
        options.Add(o);
        o = new DialogOption(51, new List<int>() { 53, 55, 56 });
        o.effects.Add(() => {
            GlobalControl.decisions.o25GreetingDone = true;
        });
        options.Add(o);
        o = new DialogOption(52, new List<int>() { 53, 55, 56 });
        options.Add(o);
        o = new DialogOption(53, new List<int>() { 54 });
        options.Add(o);
        // Trade
        o = new DialogOption(54, new List<int>() );
        o.effects.Add(() => {
            NPCBehaviour trader = UIControl.dialog.GetComponent<DialogControl>().responder;
            if (!GlobalControl.decisions.o25ShopInventoryLoaded) trader.SetInventory(ItemLibrary.GetDefaultShopO25());
            GlobalControl.decisions.o25ShopInventoryLoaded = true;
            UIControl.DestroyDialog();
            UIControl.ShowInventory(trader.GetInventory());
        });
        options.Add(o);
        o = new DialogOption(55, new List<int>() { 59, 56 });
        options.Add(o);
        o = new DialogOption(56, new List<int>() { 13 });
        options.Add(o);
        // Guards dialog
        o = new DialogOption(57, new List<int>() { 58 });
        options.Add(o);
        o = new DialogOption(58, new List<int>() );
        o.effects.Add(() => {
            UIControl.DestroyDialog();
        });
        options.Add(o);
        // Paint me
        o = new DialogOption(59, new List<int>());
        o.effects.Add(() => {
            if (GlobalControl.GetPlayer()) GlobalControl.GetPlayer().GetComponent<PlayerBehaviour>().PaintArmors();
            if (GlobalControl.GetPlayer()) GlobalControl.GetPlayer().GetComponent<PlayerBehaviour>().RefreshPlayerLimbs();
            UIControl.DestroyDialog();
        });
        options.Add(o);
    }

}
