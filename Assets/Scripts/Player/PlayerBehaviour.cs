using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ArmorBehaviour;
using static ArmorBehaviour.ArmorSlot;
using static PlayerBehaviour;

/* Everything that is unique to the player and not other humanoid NPCs goes here
 */
public class PlayerBehaviour : HumanoidBehaviour, Saveable<PlayerData>, Spawnable<PlayerData>
{
    // Default stats and player look. Used when changing looks using armor
    public static float PLAYER_BASE_MAX_HP = 100f;
    public static float PLAYER_BASE_SPEED = 5f;
    public static string PLAYER_BASE_COLOR_RGBA = "(1, 1, 1, 1)";
    public static string PLAYER_BASE_CATEGORY_FACE = "Face";
    public static string PLAYER_BASE_LABEL_FACE = "Face1";
    public static string PLAYER_BASE_CATEGORY_HEAD = "Head";
    public static string PLAYER_BASE_LABEL_HEAD = "Body1_0";
    public static string PLAYER_BASE_CATEGORY_TORSO = "Torso";
    public static string PLAYER_BASE_LABEL_TORSO = "Body1_2";
    public static string PLAYER_BASE_CATEGORY_PELVIS = "Pelvis";
    public static string PLAYER_BASE_LABEL_PELVIS = "Body1_5";
    public static string PLAYER_BASE_CATEGORY_ARM_UP_R = "Arm_Right_Up";
    public static string PLAYER_BASE_LABEL_ARM_UP_R = "Body1_1";
    public static string PLAYER_BASE_CATEGORY_ARM_UP_L = "Arm_Left_Up";
    public static string PLAYER_BASE_LABEL_ARM_UP_L = "Body1_3";
    public static string PLAYER_BASE_CATEGORY_ARM_LOW_R = "Arm_Right_Low";
    public static string PLAYER_BASE_LABEL_ARM_LOW_R = "Body1_4";
    public static string PLAYER_BASE_CATEGORY_ARM_LOW_L = "Arm_Left_Low";
    public static string PLAYER_BASE_LABEL_ARM_LOW_L = "Body1_6";
    public static string PLAYER_BASE_CATEGORY_HAND_R = "Hand_Right";
    public static string PLAYER_BASE_LABEL_HAND_R = "Body1_7";
    public static string PLAYER_BASE_CATEGORY_HAND_L = "Hand_Left";
    public static string PLAYER_BASE_LABEL_HAND_L = "Body1_8";
    public static string PLAYER_BASE_CATEGORY_LEG_UP_R = "Leg_Right_Up";
    public static string PLAYER_BASE_LABEL_LEG_UP_R = "Body1_10";
    public static string PLAYER_BASE_CATEGORY_LEG_UP_L = "Leg_Left_Up";
    public static string PLAYER_BASE_LABEL_LEG_UP_L = "Body1_11";
    public static string PLAYER_BASE_CATEGORY_LEG_LOW_R = "Leg_Right_Low";
    public static string PLAYER_BASE_LABEL_LEG_LOW_R = "Body1_9";
    public static string PLAYER_BASE_CATEGORY_LEG_LOW_L = "Leg_Left_Low";
    public static string PLAYER_BASE_LABEL_LEG_LOW_L = "Body1_12";
    public static string PLAYER_BASE_CATEGORY_FOOT_R = "Foot_Right";
    public static string PLAYER_BASE_LABEL_FOOT_R = "Body1_13";
    public static string PLAYER_BASE_CATEGORY_FOOT_L = "Foot_Left";
    public static string PLAYER_BASE_LABEL_FOOT_L = "Body1_14";


    [HideInInspector] public int currencyCount = 0;

    private int weaponSelected = -1;
    private Dictionary<GameObject, float> interactibles = new Dictionary<GameObject, float>();
    private GameObject selectedInteractible = null;

    private float bonusHP = 0f;
    private float bonusSpeedMult = 0f;
    [HideInInspector] public bool hasScrapGeneration = false;
    [HideInInspector] public bool hasChestOpening = false;

    [Serializable]
    public struct WeaponSlot
    {
        public WeaponData weapon;
        public int index;

        public WeaponSlot(WeaponData weapon, int index)
        {
            this.weapon = weapon;
            this.index = index;
        }
    }

    [HideInInspector] public List<WeaponSlot> weapons = new List<WeaponSlot>();
    [HideInInspector] public List<ArmorSlot> armors = new List<ArmorSlot>();

    public static float interactibleInteravalTime = 0.2f;

    protected void Start()
    {
        UIRefresh();
        RefreshPlayerLimbs();
    }

    new protected void Awake()
    {
        base.Awake();
        StartCoroutine(PickableHighlightLoop());
    }

    new protected void Update()
    {
        base.Update();
        if (GlobalControl.paused) return;

        // Aiming needs to be done every frame
        SetAimingLocation(PlayerInput.mousePos);

        // Trigger update of animations, rigidbody settings etc. if new input provided
        if (PlayerInput.InputChanged()) UpdateState();

        // Grab weapon from slot
        if (PlayerInput.num1) SetWeaponFromSlot(0);
        if (PlayerInput.num2) SetWeaponFromSlot(1);
        if (PlayerInput.num3) SetWeaponFromSlot(2);
        if (PlayerInput.num4) SetWeaponFromSlot(3);

        // Send attack message
        if (PlayerInput.leftclick)
        {
            SetAttackTarget(PlayerInput.mousePos);
            bool attackExecuted = Attack();
            // If attack executed then we must have weapon selected. Update UI accordingly
            if (attackExecuted && UIControl.combatUI)
            {
                WeaponBehaviour weapon = (WeaponBehaviour)activeItemBehaviour;
                UIControl.combatUI.GetComponent<CombatUIControl>().UpdateAmmoCounter(weapon.currAmmo, weapon.maxAmmo);
            }
        }

        // Interact with selected interactible
        if (PlayerInput.e) UseInteractible();

        // Reload currently selected weapon
        //if (PlayerInput.r)
    }

    public void NotifyDetectedInteractible(GameObject obj)
    {
        float distance = (gameObject.transform.position - obj.transform.position).magnitude;
        interactibles[obj] = distance;
    }

    public void NotifyDetectedInteractibleLeft(GameObject obj)
    {
        interactibles.Remove(obj);
    }

    public void UnequipWeapon(int index)
    {
        WeaponSlot? toRemove = null;
        foreach (WeaponSlot slot in weapons)
        {
            if (slot.index == index)
            {
                inventory.Add(slot.weapon);
                toRemove = slot;
                break;
            }
        }
        if (toRemove.HasValue) weapons.Remove(toRemove.Value);
        SetWeaponFromSlot(weaponSelected);
    }

    public void UnequipArmor(Slot aSlot)
    {
        ArmorSlot? toRemove = null;
        foreach (ArmorSlot slot in armors)
        {
            if (slot.slot == aSlot)
            {
                inventory.Add(slot.armor);
                toRemove = slot;
                break;
            }
        }
        if (toRemove.HasValue) armors.Remove(toRemove.Value);
        RefreshPlayerStats();
    }

    public void EquipWeapon(WeaponSlot wSlot)
    {
        UnequipWeapon(wSlot.index);
        weapons.Add(wSlot);
        if (inventory.Contains(wSlot.weapon)) inventory.Remove(wSlot.weapon);
        SetWeaponFromSlot(weaponSelected);
    }

    public void EquipArmor(ArmorSlot aSlot)
    {
        UnequipArmor(aSlot.slot);
        armors.Add(aSlot);
        if (inventory.Contains(aSlot.armor)) inventory.Remove(aSlot.armor);
        RefreshPlayerStats();
    }

    public void UIRefresh() 
    {
        ItemBehaviour b = activeItemBehaviour;
        if (b is WeaponBehaviour)
        {
            if (UIControl.combatUI)
            {
                UIControl.combatUI.GetComponent<CombatUIControl>().EnableAmmoPanel(true);
                UIControl.combatUI.GetComponent<CombatUIControl>().UpdateAmmoCounter(((WeaponBehaviour)b).currAmmo, ((WeaponBehaviour)b).maxAmmo);
            }
        }
        else
        {
            if (UIControl.combatUI) UIControl.combatUI.GetComponent<CombatUIControl>().EnableAmmoPanel(false);
        }
    }

    // Continuously run highlight code every 0.2 seconds for closest object
    private IEnumerator PickableHighlightLoop()
    {
        while (true)
        {
            if (!GlobalControl.paused) RefreshInteractibles();
            yield return new WaitForSeconds(interactibleInteravalTime);
        }
    }

    // Updates distances to all interactible objects. After this, triggers highlight action for closest one.
    private void RefreshInteractibles()
    {
        selectedInteractible = null;
        Dictionary<GameObject, float> dictNew = new Dictionary<GameObject, float>();
        foreach (KeyValuePair<GameObject, float> pair in interactibles)
        {
            GameObject obj = pair.Key;
            float distance = (groundReferenceObject.transform.position - obj.transform.position).magnitude;
            dictNew[obj] = distance;
        }
        interactibles = dictNew;
        GameObject closest = interactibles.OrderBy(pair => pair.Value).FirstOrDefault().Key;
        if (!closest) return;
        selectedInteractible = closest;
        EntityBehaviour b = closest.GetComponent<EntityBehaviour>();
        if (!b) return;
        b.interactionEnterEffect?.Invoke(this);
    }


    // Trigger selected interactible's effect. If no interactible is selected do nothing
    private void UseInteractible()
    {
        if (!selectedInteractible) return;
        EntityBehaviour b = selectedInteractible.GetComponent<EntityBehaviour>();
        if (!b) return;
        b.interactionUseEffect?.Invoke(this);
    }

    private void SetWeaponFromSlot(int index)
    {
        WeaponData weapon = GetWeaponBySlot(index);
        ItemData toStore = SetItemActive(weapon);
        if (toStore != null) StoreItem(toStore);
        weaponSelected = index;
        if (weapon == null) weaponSelected = -1;
        // Show or hide UI ammo counter if applicable
        UIRefresh();
    }

    private WeaponData GetWeaponBySlot(int index)
    {
        foreach (WeaponSlot slot in weapons)
        {
            if (slot.index == index)
            {
                return slot.weapon;
            }
        }
        return null;
    }

    private ArmorData GetArmorBySlot(Slot slot)
    {
        foreach (ArmorSlot aSlot in armors)
        {
            if (slot == aSlot.slot)
            {
                return aSlot.armor;
            }
        }
        return null;
    }

    // Check if this item is owned by the player and replace its data with new
    private void StoreItem(ItemData item)
    {
        // First search inventory to replace item
        int indToReplace = -1;
        for (int i = 0; i < inventory.Count; i++) { if (inventory[i].ID == item.ID) indToReplace = i; }
        if (indToReplace != -1)
        {
            inventory[indToReplace] = item;
            return;
        }
        // Now try to save in assigned weapons
        for (int i = 0; i < weapons.Count; i++) { if (weapons[i].weapon.ID == item.ID) indToReplace = i; }
        if (indToReplace != -1) weapons[indToReplace] = new WeaponSlot((WeaponData)item, weapons[indToReplace].index);

    }

    private void RefreshPlayerStats()
    {
        // Reset to base stats
        bonusHP = 0f;
        bonusSpeedMult = 1f;
        hasScrapGeneration = false;
        hasChestOpening = false;
        // Get armor buffs
        foreach (ArmorSlot aSlot in armors)
        {
            ArmorData armor = aSlot.armor;
            if (armor.buffsScrapGeneration) hasScrapGeneration = true;
            if (armor.buffsChestOpening) hasChestOpening = true;
            bonusHP += armor.hpIncrease;
            bonusSpeedMult += armor.speedMultiplier;
        }
        // Apply new stats
        SetMaxHealth(PLAYER_BASE_MAX_HP + bonusHP);
        moveSpeed = PLAYER_BASE_SPEED * bonusSpeedMult;
    }

    private void RefreshPlayerLimbs()
    {
        // Reset player graphics
        SetBodypart("Face", PLAYER_BASE_CATEGORY_FACE, PLAYER_BASE_LABEL_FACE, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Head", PLAYER_BASE_CATEGORY_HEAD, PLAYER_BASE_LABEL_HEAD, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Torso", PLAYER_BASE_CATEGORY_TORSO, PLAYER_BASE_LABEL_TORSO, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Pelvis", PLAYER_BASE_CATEGORY_PELVIS, PLAYER_BASE_LABEL_PELVIS, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Arm_Up_R", PLAYER_BASE_CATEGORY_ARM_UP_R, PLAYER_BASE_LABEL_ARM_UP_R, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Arm_Low_R", PLAYER_BASE_CATEGORY_ARM_LOW_R, PLAYER_BASE_LABEL_ARM_LOW_R, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Hand_R", PLAYER_BASE_CATEGORY_HAND_R, PLAYER_BASE_LABEL_HAND_R, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Arm_Up_L", PLAYER_BASE_CATEGORY_ARM_UP_L, PLAYER_BASE_LABEL_ARM_UP_L, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Arm_Low_L", PLAYER_BASE_CATEGORY_ARM_LOW_L, PLAYER_BASE_LABEL_ARM_LOW_L, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Hand_L", PLAYER_BASE_CATEGORY_HAND_L, PLAYER_BASE_LABEL_HAND_L, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Leg_Up_R", PLAYER_BASE_CATEGORY_LEG_UP_R, PLAYER_BASE_LABEL_LEG_UP_R, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Leg_Low_R", PLAYER_BASE_CATEGORY_LEG_LOW_R, PLAYER_BASE_LABEL_LEG_LOW_R, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Foot_R", PLAYER_BASE_CATEGORY_FOOT_R, PLAYER_BASE_LABEL_FOOT_R, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Leg_Up_L", PLAYER_BASE_CATEGORY_LEG_UP_L, PLAYER_BASE_LABEL_LEG_UP_L, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Leg_Low_L", PLAYER_BASE_CATEGORY_LEG_LOW_L, PLAYER_BASE_LABEL_LEG_LOW_L, PLAYER_BASE_COLOR_RGBA);
        SetBodypart("Foot_L", PLAYER_BASE_CATEGORY_FOOT_L, PLAYER_BASE_LABEL_FOOT_L, PLAYER_BASE_COLOR_RGBA);
    }

    // Updates behaviour and animations of humanoid that are part of state machine
    private void UpdateState()
    {
        // Update facing
        animations.SetFacingVector(GetFacingVector());

        // Calculate target movement direction
        Vector2 moveVector = Vector2.zero;
        if (GetAlive())
        {
            if (PlayerInput.up) moveVector += new Vector2(0.0f, 1.0f);
            if (PlayerInput.down) moveVector += new Vector2(0.0f, -1.0f);
            if (PlayerInput.left) moveVector += new Vector2(-1.0f, 0.0f);
            if (PlayerInput.right) moveVector += new Vector2(1.0f, 0.0f);
            moveVector.Normalize();
        }

        // Make player character move based on inputs
        SetMoveVector(moveVector);
        if (moveVector.magnitude > 0) SetSpeed(animations.IsMovingAgainstFacing() ? moveSpeed * backwardSpeedMultiplier : moveSpeed);
        else SetSpeed(0.0f);
    }

    // Returns normalized vector that points from position of character to the pointer location
    private Vector2 GetFacingVector()
    {
        Vector2 targetVector = PlayerInput.mousePos - new Vector2(transform.position.x, transform.position.y);
        targetVector.Normalize();
        return targetVector;
    }

    public new PlayerData Save()
    {
        PlayerData data = new PlayerData(base.Save());
        data.currencyCount = currencyCount;
        data.weaponSelected = weaponSelected;
        data.weapons = weapons;
        data.armors = armors;
        return data;
    }

    // Load player using saved data
    public void Load(PlayerData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        currencyCount = data.currencyCount;
        weapons = data.weapons;
        armors = data.armors;
        weaponSelected = data.weaponSelected;
        SetWeaponFromSlot(weaponSelected);
        UpdateState();
    }

    public static GameObject Spawn(PlayerData data, Vector2 position, Quaternion rotation, Vector2 scale, Transform parent = null)
    {
        GameObject obj = HumanoidBehaviour.Spawn(data, position, rotation, scale, parent);
        obj.GetComponent<PlayerBehaviour>().Load(data, false);
        return obj;
    }

    public static GameObject Spawn(PlayerData data, Transform parent = null)
    {
        GameObject obj = HumanoidBehaviour.Spawn(data, parent);
        obj.GetComponent<PlayerBehaviour>().Load(data);
        return obj;
    }
}

[Serializable]
public class PlayerData : HumanoidData
{
    public PlayerData() { }

    public PlayerData(HumanoidData data) : base(data)
    {
        itemActive = data.itemActive;
        bodypartData = data.bodypartData;
        animationData = data.animationData;
    }

    public int currencyCount;
    public int weaponSelected;
    public List<WeaponSlot> weapons;
    public List<ArmorSlot> armors;

}
