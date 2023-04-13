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
    private static string inventoryPrefabPath = "Prefabs/UI/Inventory";
    private static GameObject menu = null;
    private static GameObject dialog = null;
    private static GameObject inventory = null;

    public void Update()
    {
        if (PlayerInput.esc)
        {
            if (inventory)
            {
                destroyInventory();
                return;
            }
            if (!menu) showMenu();
            else destroyMenu();
        }
        if (PlayerInput.tab)
        {
            if (!inventory) showInventory();
            else destroyInventory();
        }
    }

    public static void showMenu()
    {
        GlobalControl.PauseGame();
        menu = Instantiate(Resources.Load<GameObject>(menuPrefabPath));
    }

    public static void destroyMenu()
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

    public static void showInventory()
    {
        GlobalControl.PauseGame();
        inventory = Instantiate(Resources.Load<GameObject>(inventoryPrefabPath));
        inventory.GetComponent<InventoryControl>().LoadInventoryPanel(GlobalControl.GetPlayer().GetComponent<PlayerBehaviour>());
    }

    public static void destroyInventory()
    {
        Destroy(inventory);
        inventory = null;
        GlobalControl.UnpauseGame();
    }
}
