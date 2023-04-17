using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ArmorBehaviour;
using static ArmorBehaviour.ArmorSlot;
using static PlayerBehaviour;

public class InventoryControl : MonoBehaviour
{
    public GameObject inventoryPanel;
    public GameObject shopPanel;
    public Button[] actionButtons;
    public Button[] weaponSlots;
    public Button[] armorSlots;
    public Button cancelAction;
    public Button throwAction;
    public TextMeshProUGUI currencyText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI shopText;

    private List<Button> inventorySlots = new List<Button>();
    private int inventoryOccupied = 0;
    private List<Button> shopSlots = new List<Button>();
    private int shopOccupied = 0;
    private bool isShopOpen = false;
    private bool hoverLocked = false;

    private PlayerBehaviour playerBehaviour = null;
    private List<ItemData> shopItems = null;

    private enum ItemStatus
    {
        inventory, equipped, shop
    }

    private void Awake()
    {
        foreach (Transform child in inventoryPanel.transform)
        {
            Button button = child.GetComponent<Button>();
            if (button != null) inventorySlots.Add(button);
        }
        foreach (Transform child in shopPanel.transform)
        {
            Button button = child.GetComponent<Button>();
            if (button != null) shopSlots.Add(button);
        }
        cancelAction.onClick.AddListener(() =>
        {
            ClearActionPanel();
        });
    }

    // Clear all triggers and texts, and disable all buttons
    private void ClearPanel()
    {
        foreach (Button b in inventorySlots) EnableButton(false, b);
        foreach (Button b in inventorySlots) ClearButtonActions(b);
        foreach (Button b in inventorySlots) SetButtonText(b, "No text");
        foreach (Button b in weaponSlots) EnableButton(false, b);
        foreach (Button b in weaponSlots) ClearButtonActions(b);
        foreach (Button b in weaponSlots) SetButtonText(b, "No text");
        foreach (Button b in armorSlots) EnableButton(false, b);
        foreach (Button b in armorSlots) ClearButtonActions(b);
        foreach (Button b in armorSlots) SetButtonText(b, "No text");
        foreach (Button b in shopSlots) EnableButton(false, b);
        foreach (Button b in shopSlots) ClearButtonActions(b);
        foreach (Button b in shopSlots) SetButtonText(b, "No text");
        currencyText.text = "No text";
        EnableText(false, currencyText);
        descriptionText.text = "No text";
        EnableText(false, descriptionText);
        ClearActionPanel();
        inventoryOccupied = 0;
        shopOccupied = 0;
        shopPanel.SetActive(false);
        shopText.gameObject.SetActive(false);
        isShopOpen = false;
        hoverLocked = false;
    }

    private void ClearActionPanel()
    {
        foreach (Button b in actionButtons) EnableButton(false, b);
        foreach (Button b in actionButtons) ClearButtonActions(b);
        foreach (Button b in actionButtons) SetButtonText(b, "No text");
        EnableButton(false, cancelAction);
        EnableButton(false, throwAction);
        ClearButtonActions(throwAction);
        descriptionText.text = "No text";
        EnableText(false, descriptionText);
        hoverLocked = false;
    }

    private void EnableButton(bool enable, Button button)
    {
        button.image.enabled = enable;
        button.interactable = enable;
        TextMeshProUGUI txt = button.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.alpha = enable ? 255f : 0f;
    }

    private void SetButtonText(Button button, string text)
    {
        TextMeshProUGUI txt = button.GetComponentInChildren<TextMeshProUGUI>();
        if (txt) txt.text = text;
    }

    private void ClearButtonActions(Button button)
    {
        button.onClick.RemoveAllListeners();
        EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
        if (eventTrigger) Destroy(eventTrigger);
    }

    private void EnableText(bool enable, TextMeshProUGUI text)
    {
        text.enabled = enable;
    }

    // Setup a slot to have correct image and actions when clicked/hovered
    private void SetupSlot(Button btn, ItemData item, ItemStatus status)
    {
        // Increase slot usage if putting in inventory slots
        if (status == ItemStatus.inventory) inventoryOccupied++;
        if (status == ItemStatus.shop) shopOccupied++;
        // Setup button visuals
        EnableButton(true, btn);
        btn.image.sprite = Resources.Load<Sprite>(item.inventoryIconLink);
        // Setup hover trigger
        EventTrigger buttonTrigger = btn.gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((eventData) =>
        {
            if (hoverLocked) return;
            SetupActionsItemInspection(item, status);
        });
        buttonTrigger.triggers.Add(enterEntry);
        // Setup click trigger
        btn.onClick.AddListener(() =>
        {
            SetupActionsItemInspection(item, status);
            SetupActionsItemAction(item, status, btn);
        });
    }

    // Setup actions panel for when item is being inspected (hovered over)
    private void SetupActionsItemInspection(ItemData item, ItemStatus status)
    {
        foreach (Button b in actionButtons) EnableButton(false, b);
        EnableText(true, descriptionText);
        EnableButton(false, cancelAction);
        EnableButton(false, throwAction);
        // set description to item text
        descriptionText.text = ItemLibrary.itemLocalization.TryGetValue(item.descriptionTextLinkID, out string val) ? val : "No description";
        if (status != ItemStatus.equipped && item is WeaponData) descriptionText.text += "\n\nAmmo: " +
                ((WeaponData)item).currAmmo + "/" + ((WeaponData)item).maxAmmo;
        if (item is AmmoData) descriptionText.text += "\n\nQuantity: " + ((AmmoData)item).quantity + "/" + ((AmmoData)item).maxStack;
        if (status == ItemStatus.equipped) descriptionText.text += "\n\nClick to unequip the item";
        else if (!isShopOpen && status == ItemStatus.inventory && (item is WeaponData || item is ArmorData))
            descriptionText.text += "\n\nClick to equip the item";
        else if (status == ItemStatus.shop) descriptionText.text += "\n\nClick to purchase the item.\nPrice: " + item.value * ((item is AmmoData) ? ((AmmoData)item).quantity : 1f);
        else if (isShopOpen && status == ItemStatus.inventory) descriptionText.text += "\n\nClick to sell the item.\nPrice: " + (int)Mathf.Round(((item is AmmoData) ? ((AmmoData)item).quantity : 1f) / 2);
        else if (!isShopOpen && (item is UsableData)) descriptionText.text += "\n\nClick to use the item";
    }

    // Setup actions panel to have actions that are dependant on item type, when item is pressed
    private void SetupActionsItemAction(ItemData item, ItemStatus status, Button buttonPressed)
    {
        // Item equipped - attempt to de-equip item
        if (status == ItemStatus.equipped)
        {
            // Get which slot we are de-equipping
            if (ButtonIsWeaponSlot(buttonPressed)) TryUnequipItem(GetWeaponSlotIndex(buttonPressed));
            if (!ButtonIsWeaponSlot(buttonPressed)) TryUnequipItem(GetArmorSlotIndex(buttonPressed));
            LoadInventoryPanel(playerBehaviour, shopItems);
        }
        // Item in inventory - check type of item and add actions
        else if (status == ItemStatus.inventory)
        {
            if (!isShopOpen)
            {
                EnableButton(true, cancelAction);
                EnableButton(true, throwAction);
                SetupThrowItemButton(throwAction, item);
                foreach (Button b in actionButtons) EnableButton(false, b);
                // setup actions according to what we can do with this item
                if (item is WeaponData)
                {
                    // For weapons, allow options to equip in different slots
                    SetupAssignWeaponSlotButton(actionButtons[0], (WeaponData)item, 0);
                    SetupAssignWeaponSlotButton(actionButtons[1], (WeaponData)item, 1);
                    SetupAssignWeaponSlotButton(actionButtons[2], (WeaponData)item, 2);
                    SetupAssignWeaponSlotButton(actionButtons[3], (WeaponData)item, 3);
                    hoverLocked = true;
                }
                if (item is ArmorData)
                {
                    // For armor, allow option to equip item in correct slot
                    SetupAssignArmorButton(actionButtons[0], (ArmorData)item);
                    hoverLocked = true;
                }
                if (item is UsableData)
                {
                    SetupUsableButton(actionButtons[0], (UsableData)item);
                    hoverLocked = true;
                }
            }
            else
            {
                // Setup item sale
                SetupSellItemButton(actionButtons[0], item);
                EnableButton(true, cancelAction);
                hoverLocked = true;
            }
        }
        // Item in shop - setup purchase option
        else if (status == ItemStatus.shop)
        {
            EnableButton(true, cancelAction);
            foreach (Button b in actionButtons) EnableButton(false, b);
            SetupPurchaseItemButton(actionButtons[0], item);
            hoverLocked = true;
        }
    }

    private int GetWeaponSlotIndex(Button btn)
    {
        int i = 0;
        foreach (Button b in weaponSlots)
        {
            if (b == btn) break;
            i++;
        }
        return i;
    }

    private Slot GetArmorSlotIndex(Button btn)
    {
        int i = 0;
        foreach (Button b in armorSlots)
        {
            if (b == btn) break;
            i++;
        }
        Slot s = (Slot)i;
        return s;
    }

    private bool ButtonIsWeaponSlot(Button btn)
    {
        foreach (Button b in weaponSlots) { if (b == btn) return true; }
        return false;
    }

    private void TryUnequipItem(int weaponIndex)
    {
        // No space in inventory - do nothing
        if (inventoryOccupied >= CreatureBehaviour.inventoryLimit) return;
        // De-equip the item
        playerBehaviour.UnequipWeapon(weaponIndex);
    }

    private void TryUnequipItem(Slot armorSlot)
    {
        // No space in inventory - do nothing
        if (inventoryOccupied >= CreatureBehaviour.inventoryLimit) return;
        // De-equip the item
        playerBehaviour.UnequipArmor(armorSlot);
    }

    // Setup given action button to assign a weapon to a specified weapon slot
    private void SetupAssignWeaponSlotButton(Button button, WeaponData weapon, int slot)
    {
        ClearButtonActions(button);
        EnableButton(true, button);
        SetButtonText(button, "Assign weapon to slot " + (slot + 1).ToString());
        button.onClick.AddListener(() =>
        {
            WeaponSlot w = new WeaponSlot(weapon, slot);
            playerBehaviour.EquipWeapon(w);
            LoadInventoryPanel(playerBehaviour, shopItems);
        });
    }

    private void SetupAssignArmorButton(Button button, ArmorData armor)
    {
        ClearButtonActions(button);
        EnableButton(true, button);
        string s = "No correct module";
        switch(armor.slot)
        {
            case Slot.head:
                s = "Head module";
                break;
            case Slot.torso:
                s = "Body module";
                break;
            case Slot.arms:
                s = "Arms module";
                break;
            case Slot.legs:
                s = "Legs module";
                break;
        }
        SetButtonText(button, "Assign to: " + s);
        button.onClick.AddListener(() =>
        {
            ArmorSlot a = new ArmorSlot(armor, armor.slot);
            playerBehaviour.EquipArmor(a);
            LoadInventoryPanel(playerBehaviour, shopItems);
        });
    }

    private void SetupUsableButton(Button button, ItemData item)
    {
        ClearButtonActions(button);
        EnableButton(true, button);
        SetButtonText(button, "Confirm use item");
        button.onClick.AddListener(() =>
        {
            playerBehaviour.Heal(((UsableData)item).restoration);
            playerBehaviour.GetInventory().Remove(item);
            LoadInventoryPanel(playerBehaviour, shopItems);
        });
    }

    private void SetupPurchaseItemButton(Button button, ItemData item)
    {
        ClearButtonActions(button);
        EnableButton(true, button);
        SetButtonText(button, "Confirm purchase");
        button.onClick.AddListener(() =>
        {
            if (playerBehaviour.currencyCount >= item.value * ((item is AmmoData) ? ((AmmoData)item).quantity : 1f) && playerBehaviour.GetInventory().Count < CreatureBehaviour.inventoryLimit)
            {
                playerBehaviour.currencyCount -= item.value * (int)((item is AmmoData) ? ((AmmoData)item).quantity : 1f);
                playerBehaviour.GiveItem(item);
                shopItems.Remove(item);
            }
            LoadInventoryPanel(playerBehaviour, shopItems);
        });
    }

    private void SetupSellItemButton(Button button, ItemData item)
    {
        ClearButtonActions(button);
        EnableButton(true, button);
        SetButtonText(button, "Confirm sale");
        button.onClick.AddListener(() =>
        {
            playerBehaviour.currencyCount += (int)Mathf.Round(item.value * ((item is AmmoData) ? ((AmmoData)item).quantity : 1f) / 2);
            playerBehaviour.GetInventory().Remove(item);
            shopItems.Add(item);
            LoadInventoryPanel(playerBehaviour, shopItems);
        });
    }

    private void SetupThrowItemButton(Button button, ItemData item)
    {
        EnableButton(true, button);
        button.onClick.AddListener(() =>
        {
            playerBehaviour.GetInventory().Remove(item);
            item.pickable = true;
            GameObject obj = ItemBehaviour.FlexibleSpawn(item);
            obj.transform.position = playerBehaviour.groundReferenceObject.transform.position;
            LoadInventoryPanel(playerBehaviour, shopItems);
        });
    }

    // Load buttons given item lists
    private void LoadInventory(List<WeaponSlot> weapons, List<ArmorSlot> armors, List<ItemData> items)
    {
        for (int i = 0; i < weapons.Count; i++) SetupSlot(weaponSlots[weapons[i].index], weapons[i].weapon, ItemStatus.equipped);
        for (int i = 0; i < armors.Count; i++) SetupSlot(armorSlots[(int)armors[i].slot], armors[i].armor, ItemStatus.equipped);
        for (int i = 0; i < items.Count; i++)
        {
            // Do nothing if we run out of slots
            if (inventoryOccupied >= CreatureBehaviour.inventoryLimit) break;
            SetupSlot(inventorySlots[i], items[i], ItemStatus.inventory);
        }
    }

    // Load shop panel using items
    private void LoadShop(List<ItemData> shopItems)
    {
        for (int i = 0; i < shopItems.Count; i++)
        {
            // Do nothing if we run out of slots
            if (shopOccupied >= CreatureBehaviour.inventoryLimit) break;
            SetupSlot(shopSlots[i], shopItems[i], ItemStatus.shop);
        }
    }

    // Load the panel using player's inventory and optionally shop's items
    public void LoadInventoryPanel(PlayerBehaviour player, List<ItemData> shopItems = null)
    {
        ClearPanel();
        playerBehaviour = player;
        currencyText.text = "Scrap: " + player.currencyCount;
        EnableText(true, currencyText);
        LoadInventory(player.weapons, player.armors, player.GetInventory());
        if (shopItems != null)
        {
            shopPanel.SetActive(true);
            shopText.gameObject.SetActive(true);
            isShopOpen = true;
            this.shopItems = shopItems;
            LoadShop(shopItems);
        }
    }
}
