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
    private static string combatUIPrefabPath = "Prefabs/UI/CombatUI";
    private static GameObject menu = null;
    private static GameObject dialog = null;
    private static GameObject inventory = null;
    private static GameObject combatUI = null;

    public void Update()
    {
        // React to keystrokes
        if (PlayerInput.esc)
        {
            // Inventory open - close it
            if (inventory)
            {
                DestroyInventory();
                ShowCombatUI();
                return;
            }
            // Open menu
            if (!menu)
            {
                DestroyCombatUI();
                ShowMenu();
            }
            // Close menu
            else
            {
                DestroyMenu();
                ShowCombatUI();
            }
        }
        if (PlayerInput.tab)
        {
            if (!inventory)
            {
                DestroyCombatUI();
                ShowInventory();
            }
            else
            {
                DestroyInventory();
                ShowCombatUI();
            }
        }
    }

    public static void ShowMenu()
    {
        GlobalControl.PauseGame();
        menu = Instantiate(Resources.Load<GameObject>(menuPrefabPath));
    }

    public static void DestroyMenu()
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

    public static void DestroyDialog()
    {
        Destroy(dialog);
        dialog = null;
        GlobalControl.UnpauseGame();
    }

    public static void ShowInventory()
    {
        GlobalControl.PauseGame();
        inventory = Instantiate(Resources.Load<GameObject>(inventoryPrefabPath));
        inventory.GetComponent<InventoryControl>().LoadInventoryPanel(GlobalControl.GetPlayer().GetComponent<PlayerBehaviour>());
    }

    public static void DestroyInventory()
    {
        Destroy(inventory);
        inventory = null;
        GlobalControl.UnpauseGame();
    }

    public static void ShowCombatUI()
    {
        combatUI = Instantiate(Resources.Load<GameObject>(combatUIPrefabPath));
        GlobalControl.GetPlayer().GetComponent<PlayerBehaviour>().SetHealthbar(combatUI.GetComponent<CombatUIControl>().healthbarBehaviour);
    }

    public static void DestroyCombatUI()
    {
        if (combatUI) Destroy(combatUI);
        combatUI = null;
    }
}
