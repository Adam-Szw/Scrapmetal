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
    [HideInInspector] public static GameObject menu = null;
    [HideInInspector] public static GameObject dialog = null;
    [HideInInspector] public static GameObject inventory = null;
    [HideInInspector] public static GameObject combatUI = null;

    public void Update()
    {
        // React to keystrokes
        if (PlayerInput.esc)
        {
            // Dialog open - close it
            if (dialog)
            {
                DestroyDialog();
                ShowCombatUI();
                return;
            }

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

    public static void showDialog(ulong initialOptionID, string dialogRespondentName)
    {
        GlobalControl.PauseGame();
        DialogOption initialOption = DialogLibrary.getDialogOptionByID(initialOptionID);
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
        GlobalControl.GetPlayer().GetComponent<PlayerBehaviour>().UIRefresh();
    }

    public static void DestroyCombatUI()
    {
        if (combatUI) Destroy(combatUI);
        combatUI = null;
    }
}
