using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PopupControl;

/* This class is responsible for opening/closing/interacting with main menu
 */
public class UIControl : MonoBehaviour
{

    private static string menuPrefabPath = "Prefabs/UI/Menu";
    private static string dialogPrefabPath = "Prefabs/UI/Dialog";
    private static string inventoryPrefabPath = "Prefabs/UI/Inventory";
    private static string combatUIPrefabPath = "Prefabs/UI/CombatUI";
    private static string popupPrefabPath = "Prefabs/UI/Popup";
    [HideInInspector] public static GameObject menu = null;
    [HideInInspector] public static GameObject dialog = null;
    [HideInInspector] public static GameObject inventory = null;
    [HideInInspector] public static GameObject combatUI = null;
    [HideInInspector] public static GameObject popup = null;

    public void Update()
    {
        // Go thorugh hierarchy of closing things - then open menu if everything closed
        if (PlayerInput.esc)
        {
            // Popup open - close it
            if (popup)
            {
                DestroyPopup();
            }
            // Dialog open - close it
            else if (dialog)
            {
                DestroyDialog();
                ShowCombatUI();
            }
            // Inventory open - close it
            else if (inventory)
            {
                DestroyInventory();
                ShowCombatUI();
            }
            // Open menu
            else if (!menu)
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
            // Popup open - close it
            if (popup) DestroyPopup();
            // Open inventory
            else if (!inventory)
            {
                DestroyCombatUI();
                ShowInventory();
            }
            // CLose inventory
            else
            {
                DestroyInventory();
                ShowCombatUI();
            }
        }
    }

    public static void DefaultUI()
    {
        DestroyCombatUI();
        DestroyDialog();
        DestroyPopup();
        DestroyMenu();
        ShowCombatUI();
    }

    public static void ShowMenu()
    {
        GlobalControl.PauseGame();
        menu = Instantiate(Resources.Load<GameObject>(menuPrefabPath));
    }

    public static void DestroyMenu()
    {
        if (menu) Destroy(menu);
        menu = null;
        GlobalControl.UnpauseGame();
    }

    public static void showDialog(ulong initialOptionID, string dialogRespondentName)
    {
        if (!GlobalControl.GetPlayer()) return;
        GlobalControl.PauseGame();
        DialogOption initialOption = DialogLibrary.getDialogOptionByID(initialOptionID);
        dialog = Instantiate(Resources.Load<GameObject>(dialogPrefabPath));
        dialog.GetComponent<DialogControl>().Initialize(initialOption, dialogRespondentName);
    }

    public static void DestroyDialog()
    {
        if (dialog) Destroy(dialog);
        dialog = null;
        GlobalControl.UnpauseGame();
    }

    public static void ShowInventory()
    {
        if (!GlobalControl.GetPlayer()) return;
        GlobalControl.PauseGame();
        inventory = Instantiate(Resources.Load<GameObject>(inventoryPrefabPath));
        inventory.GetComponent<InventoryControl>().LoadInventoryPanel(GlobalControl.GetPlayer().GetComponent<PlayerBehaviour>());
    }

    public static void DestroyInventory()
    {
        if (inventory) Destroy(inventory);
        inventory = null;
        GlobalControl.UnpauseGame();
    }

    public static void ShowCombatUI()
    {
        if (!GlobalControl.GetPlayer()) return;
        combatUI = Instantiate(Resources.Load<GameObject>(combatUIPrefabPath));
    }

    public static void DestroyCombatUI()
    {
        if (combatUI) Destroy(combatUI);
        combatUI = null;
    }

    public static void ShowPopup(string text, string imageLink, float fadeInTime, Effect effect)
    {
        popup = Instantiate(Resources.Load<GameObject>(popupPrefabPath));
        popup.GetComponent<PopupControl>().Initialize(text, imageLink, fadeInTime, effect);
    }

    public static void DestroyPopup()
    {
        if (popup) popup.GetComponent<PopupControl>().SelfDestruct();
        popup = null;
    }
}
