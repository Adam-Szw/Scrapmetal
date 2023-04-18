using UnityEngine;
using UnityEditor;

public class AddSuffixToChildrenEditor : EditorWindow
{
    string suffix = "";

    [MenuItem("Custom/Add Suffix To Children")]
    static void Init()
    {
        AddSuffixToChildrenEditor window = (AddSuffixToChildrenEditor)EditorWindow.GetWindow(typeof(AddSuffixToChildrenEditor));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Add Suffix To Children", EditorStyles.boldLabel);

        suffix = EditorGUILayout.TextField("Suffix:", suffix);

        if (GUILayout.Button("Add Suffix"))
        {
            GameObject[] selectedObjects = Selection.gameObjects;

            foreach (GameObject selectedObject in selectedObjects)
            {
                foreach (Transform child in selectedObject.transform)
                {
                    child.gameObject.name += suffix;
                }
            }
        }
    }
}
