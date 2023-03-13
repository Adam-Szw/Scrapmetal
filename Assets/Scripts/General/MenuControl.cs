using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* This class is responsible for opening/closing/interacting with main menu
 */
public class MenuControl : MonoBehaviour
{

    private static string MENU_INGAME_PATH = "Prefabs/Menu_Ingame";
    private static GameObject inGameMenu = null;

    void Update()
    {
        // Open or close menu when ESC bind used
        if(PlayerInput.esc)
        {
            if (inGameMenu == null) showInGameMenu();
            else destroyInGameMenu();
        }
    }

    public static void showInGameMenu()
    {
        GlobalControl.PauseGame();
        inGameMenu = Instantiate(Resources.Load<GameObject>(MENU_INGAME_PATH));
    }

    public static void destroyInGameMenu()
    {
        Destroy(inGameMenu);
        inGameMenu = null;
        GlobalControl.UnpauseGame();
    }

}
