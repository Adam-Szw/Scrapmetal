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

    [HideInInspector] public static bool keyStrokesAccepted = false;


    public void Update()
    {
        if (!keyStrokesAccepted) return;
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
            // Nothing if in dialog or menu
            if (menu) return;
            if (dialog) return;
            // Popup open - close it
            if (popup) DestroyPopup();
            // Open inventory
            else if (inventory)
            {
                DestroyInventory();
                ShowCombatUI();
            }
            // CLose inventory
            else if (!inventory)
            {
                DestroyCombatUI();
                ShowInventory();
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
        keyStrokesAccepted = true;
    }

    public static void ShowMenu()
    {
        GlobalControl.PauseGame();
        menu = Instantiate(Resources.Load<GameObject>(menuPrefabPath));
        menu.GetComponent<MainMenuControl>().OpenIngameMenu();
    }

    public static void DestroyMenu()
    {
        if (menu) Destroy(menu);
        menu = null;
        GlobalControl.UnpauseGame();
    }

    public static void showDialog(int initialOptionID, string dialogRespondentName, NPCBehaviour responder)
    {
        if (!GlobalControl.GetPlayer()) return;
        GlobalControl.PauseGame();
        DialogOption initialOption = DialogLibrary.getDialogOptionByID(initialOptionID);
        dialog = Instantiate(Resources.Load<GameObject>(dialogPrefabPath));
        dialog.GetComponent<DialogControl>().Initialize(initialOption, dialogRespondentName, responder);
    }

    public static void DestroyDialog()
    {
        if (dialog) Destroy(dialog);
        dialog = null;
        GlobalControl.UnpauseGame();
    }

    public static void ShowInventory(List<ItemData> shopItems = null)
    {
        if (!GlobalControl.GetPlayer()) return;
        GlobalControl.PauseGame();
        inventory = Instantiate(Resources.Load<GameObject>(inventoryPrefabPath));
        inventory.GetComponent<InventoryControl>().LoadInventoryPanel(GlobalControl.GetPlayer().GetComponent<PlayerBehaviour>(), shopItems);
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

    public static void ShowPopup(int ID, float fadeInTime, Effect effect = null)
    {
        GlobalControl.PauseGame();
        popup = Instantiate(Resources.Load<GameObject>(popupPrefabPath));
        popup.GetComponent<PopupControl>().Initialize(ID, fadeInTime, effect);
    }

    public static void DestroyPopup()
    {
        if (popup) popup.GetComponent<PopupControl>().SelfDestruct();
        popup = null;
        if (!dialog && !inventory && !menu) GlobalControl.UnpauseGame();
    }
}
