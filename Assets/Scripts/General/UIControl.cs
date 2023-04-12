using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class is responsible for opening/closing/interacting with main menu
 */
public class UIControl : MonoBehaviour
{

    private static string menuPrefabPath = "Prefabs/UI/Menu";
    private static string dialogPrefabPath = "Prefabs/UI/Dialog";
    private static GameObject menu = null;
    private static GameObject dialog = null;

    public void Update()
    {
        // Open or close menu when ESC bind used
        if(PlayerInput.esc)
        {
            if (menu == null) showInGameMenu();
            else destroyInGameMenu();
        }
    }

    public static void showInGameMenu()
    {
        GlobalControl.PauseGame();
        menu = Instantiate(Resources.Load<GameObject>(menuPrefabPath));
    }

    public static void destroyInGameMenu()
    {
        Destroy(menu);
        menu = null;
        GlobalControl.UnpauseGame();
    }

    public static void showDialog(DialogOption initialOption, string dialogRespondentName)
    {
        GlobalControl.PauseGame();
        dialog = Instantiate(Resources.Load<GameObject>(dialogPrefabPath));
        dialog.GetComponent<DialogControl>().Initialize(initialOption, dialogRespondentName);
    }

    public static void destroyDialog()
    {
        Destroy(dialog);
        dialog = null;
        GlobalControl.UnpauseGame();
    }


}
