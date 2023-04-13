using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static PlayerBehaviour.ArmorSlot;

/* Everything that is unique to the player and not other humanoid NPCs goes here
 */
public class PlayerBehaviour : HumanoidBehaviour, Saveable<PlayerData>, Spawnable<PlayerData>
{
    [SerializeField] private float playerSpeed;
    [SerializeField] private float playerSpeedBackward;

    [HideInInspector] public int currencyCount = 0;
    private int weaponSelected = -1;

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

    public struct ArmorSlot
    {
        public enum Slot
        {
            head, torso, arms, legs
        }

        public ArmorData armor;
        public Slot slot;

        public ArmorSlot(ArmorData armor, Slot slot)
        {
            this.armor = armor;
            this.slot = slot;
        }
    }

    [HideInInspector] public List<WeaponSlot> weapons = new List<WeaponSlot>();
    [HideInInspector] public List<ArmorSlot> armors = new List<ArmorSlot>();

    new protected void Awake()
    {
        base.Awake();
        GameObject gun = WeaponBehaviour.Spawn("Prefabs/Items/Weapons/Rivetgun", transform.position, transform.rotation);
        WeaponData data = gun.GetComponent<WeaponBehaviour>().Save();
        Destroy(gun);
        inventory.Add(data);
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

        if (PlayerInput.leftclick)
        {
            SetAttackTarget(PlayerInput.mousePos);
            Attack();
        }
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
    }

    private void SetWeaponFromSlot(int index)
    {
        WeaponData weapon = GetWeaponBySlot(index);
        ItemData toStore = SetItemActive(weapon);
        if (toStore != null) StoreItem(toStore);
        weaponSelected = index;
        if (weapon == null) weaponSelected = -1;
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
        if (moveVector.magnitude > 0) SetSpeed(animations.IsMovingAgainstFacing() ? playerSpeedBackward : playerSpeed);
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
        return data;
    }

    // Load player using saved data
    public void Load(PlayerData data, bool loadTransform = true)
    {
        base.Load(data, loadTransform);
        currencyCount = data.currencyCount;
        weaponSelected = data.weaponSelected;
        GetWeaponBySlot(weaponSelected);
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

}
