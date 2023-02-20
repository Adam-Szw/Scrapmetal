using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuControl : MonoBehaviour
{

    [SerializeField] private GameObject inGameMenuPrefab;

    private GameObject inGameMenu = null;

    void Update()
    {
        if(PlayerInput.esc)
        {
            if (inGameMenu == null) showInGameMenu();
            else destroyInGameMenu();
        }
    }

    private void showInGameMenu()
    {
        GlobalControl.PauseGame();
        inGameMenu = Instantiate(inGameMenuPrefab);
    }

    private void destroyInGameMenu()
    {
        Destroy(inGameMenu);
        inGameMenu = null;
        GlobalControl.UnpauseGame();
    }

}
