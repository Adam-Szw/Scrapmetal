using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class PopupLibrary
{

    [Serializable]
    private class PopupData
    {
        public ulong id;
        public string text;
    }

    [Serializable]
    private class PopupLoadList
    {
        public List<PopupData> popupData;
    }

    public static Dictionary<ulong, string> popupLocalization = new Dictionary<ulong, string>();

    public static void LoadPopupLocalization(string filename)
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Localization/" + filename);
        PopupLoadList popupLoadList = JsonUtility.FromJson<PopupLoadList>(textAsset.text);
        foreach (PopupData load in popupLoadList.popupData)
        {
            popupLocalization[load.id] = load.text;
        }
    }

}
