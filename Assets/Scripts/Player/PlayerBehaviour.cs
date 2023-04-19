using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static AmmoBehaviour;
using static ArmorBehaviour;
using static ArmorBehaviour.ArmorSlot;
using static PlayerBehaviour;
using Random = UnityEngine.Random;

/* Everything that is unique to the player and not other humanoid NPCs goes here
 */
public class PlayerBehaviour : HumanoidBehaviour, Saveable<PlayerData>, Spawnable<PlayerData>
{
    public GameObject visionMask;

    // Default stats and player look. Used when changing looks using armor
    public static float PLAYER_BASE_MAX_HP = 100f;
    public static float PLAYER_BASE_SPEED = 5f;
    public static float[] PLAYER_BASE_COLOR_RGBA = new float[] { 1f, 1f, 1f, 1f };

    [HideInInspector] public int currencyCount = 0;

    private int weaponSelected = -1;
    private Dictionary<GameObject, float> interactibles = new Dictionary<GameObject, float>();
    private GameObject selectedInteractible = null;

    [HideInInspector] public bool hasScrapGeneration = false;
    [HideInInspector] public bool hasLootGeneration = false;
    [HideInInspector] public bool hasChestOpening = false;

    private float bonusHP = 0f;
    private float bonusSpeedMult = 0f;
    private float reloadTimer = 0f;

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

    [HideInInspector] private List<WeaponSlot> weapons = new List<WeaponSlot>();
    [HideInInspector] private List<ArmorSlot> armors = new List<ArmorSlot>();

    public static float interactibleInteravalTime = 0.2f;

    new protected void Awake()
    {
        base.Awake();
        StartCoroutine(PickableHighlightLoop());
        visionMask.SetActive(true);
    }

    private new void OnDestroy()
    {
        base.OnDestroy();
        StopAllCoroutines();
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
            // Do nothing if currently reloading
            if (reloadTimer > 0) return;
            SetAttackTarget(PlayerInput.mousePos);
            Attack();
        }

        // Interact with selected interactible
        if (PlayerInput.e) UseInteractible();

        // Reload currently selected weapon
        if (PlayerInput.r) ReloadCurrentWeapon();
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
                GetInventory().Add(slot.weapon);
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
                GetInventory().Add(slot.armor);
                toRemove = slot;
                break;
            }
        }
        if (toRemove.HasValue) armors.Remove(toRemove.Value);
        RefreshPlayerStats();
        RefreshPlayerLimbs();
    }

    public void EquipWeapon(WeaponSlot wSlot)
    {
        UnequipWeapon(wSlot.index);
        weapons.Add(wSlot);
        if (GetInventory().Contains(wSlot.weapon)) GetInventory().Remove(wSlot.weapon);
        SetWeaponFromSlot(weaponSelected);
    }

    public void EquipArmor(ArmorSlot aSlot)
    {
        UnequipArmor(aSlot.slot);
        armors.Add(aSlot);
        if (GetInventory().Contains(aSlot.armor)) GetInventory().Remove(aSlot.armor);
        RefreshPlayerStats();
        RefreshPlayerLimbs();
    }

    public int? GetCurrWeaponAmmo()
    {
        ItemBehaviour b = activeItemBehaviour;
        if (!b) return null;
        if (b is WeaponBehaviour) return ((WeaponBehaviour)b).currAmmo;
        return null;
    }

    public List<WeaponSlot> GetPlayerWeapons()
    {
        // Refresh weapon thats used in active weapon slot
        SetWeaponFromSlot(weaponSelected);
        return weapons;
    }

    public List<ArmorSlot> GetPlayerArmors()
    {
        return armors;
    }

    // Painting mechanic for NPC
    public void PaintArmors()
    {
        foreach (ArmorSlot aSlot in GetPlayerArmors())
        {
            //UnequipArmor(aSlot.slot);
            aSlot.armor.colorRGBA = new float[] { Random.value, Random.value, Random.value, 1f };
            //EquipArmor(aSlot);
        }
    }


    public int? GetCurrWeaponMaxAmmo()
    {
        ItemBehaviour b = activeItemBehaviour;
        if (!b) return null;
        if (b is WeaponBehaviour) return ((WeaponBehaviour)b).maxAmmo;
        return null;
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
        ItemData toStore = SaveItemActive();
        StoreItem(toStore);
        WeaponData weapon = GetWeaponBySlot(index);
        SetItemActive(weapon);
        weaponSelected = index;
        if (weapon == null) weaponSelected = -1;
    }

    private WeaponData GetWeaponBySlot(int index)
    {
        foreach (WeaponSlot slot in weapons) if (slot.index == index) return slot.weapon;
        return null;
    }

    private ArmorData GetArmorBySlot(Slot slot)
    {
        foreach (ArmorSlot aSlot in armors) if (slot == aSlot.slot) return aSlot.armor;
        return null;
    }

    // Check if this item is owned by the player and replace its data with new
    private void StoreItem(ItemData item)
    {
        if (item == null) return;
        // First search inventory to replace item
        int indToReplace = -1;
        for (int i = 0; i < GetInventory().Count; i++) { if (GetInventory()[i].ID == item.ID) indToReplace = i; }
        if (indToReplace != -1)
        {
            GetInventory()[indToReplace] = item;
            return;
        }
        // Now try to save in assigned weapons
        for (int i = 0; i < weapons.Count; i++) { if (weapons[i].weapon.ID == item.ID) indToReplace = i; }
        if (indToReplace != -1) weapons[indToReplace] = new WeaponSlot((WeaponData)item, weapons[indToReplace].index);

    }

    private void ReloadCurrentWeapon()
    {
        // Do nothing if on cooldown
        if (reloadTimer > 0f)
        {
            SpawnFloatingText(Color.blue, "Reloading" + Mathf.Round(reloadTimer * 10f) / 10f + "s", 0.3f);
            return;
        }

        // If we dont have weapon do nothing
        if (!activeItemBehaviour || activeItemBehaviour is not WeaponBehaviour)
        {
            SpawnFloatingText(Color.blue, "No weapon", 0.3f);
            return;
        }

        // Get currently active weapon
        WeaponBehaviour weapon = (WeaponBehaviour)activeItemBehaviour;
        // Search inventory to find ammo
        List<ItemData> toRemove = new List<ItemData>();
        bool reloadWasNeeded = false;
        foreach (ItemData item in GetInventory())
        {
            // Break if ammo satisfied
            if (weapon.currAmmo >= weapon.maxAmmo) break;
            // Do nothing if not ammo or wrong link
            if (item is not AmmoData) continue;
            AmmoData ammo = (AmmoData)item;
            if (ammo.link != weapon.ammoLink) continue;
            // Drain as much ammo as possible
            reloadWasNeeded = true;
            int ammoNeeded = Mathf.Max(weapon.maxAmmo - weapon.currAmmo, 0);
            int ammoReceivedMax = ammo.quantity;
            int ammoDrained = Mathf.Min(ammoNeeded, ammoReceivedMax);
            weapon.currAmmo = weapon.currAmmo + ammoDrained;
            ammo.quantity -= ammoDrained;
            // Remove ammo item if fully drained
            if (ammo.quantity <= 0) toRemove.Add(item);
        }
        foreach (ItemData item in toRemove) GetInventory().Remove(item);
        if (reloadWasNeeded) StartCoroutine(ReloadTimerCoroutine(weapon.reloadCooldown));
    }

    private IEnumerator ReloadTimerCoroutine(float time)
    {
        reloadTimer = Mathf.Max(time, 0f);
        if (time > 0) SpawnFloatingText(Color.blue, "Reloading: " + Mathf.Round(time * 10f) / 10f + "s", time);
        while (reloadTimer > 0)
        {
            yield return new WaitForSeconds(.2f);
            reloadTimer -= 0.2f;
        }
        reloadTimer = 0f;
    }

    public void RefreshPlayerStats()
    {
        // Record current health ratio
        float hpRatio = GetHealth() / GetMaxHealth();
        // Reset to base stats
        bonusHP = 0f;
        bonusSpeedMult = 1f;
        hasScrapGeneration = false;
        hasLootGeneration = false;
        hasChestOpening = false;
        // Get armor buffs
        foreach (ArmorSlot aSlot in armors)
        {
            ArmorData armor = aSlot.armor;
            if (armor.buffsScrapGeneration) hasScrapGeneration = true;
            if (armor.buffsLootGeneration) hasLootGeneration = true;
            if (armor.buffsChestOpening) hasChestOpening = true;
            bonusHP += armor.hpIncrease;
            bonusSpeedMult += armor.speedMultiplierBonus;
        }
        // Apply new stats
        SetMaxHealth(PLAYER_BASE_MAX_HP + bonusHP);
        moveSpeed = PLAYER_BASE_SPEED * bonusSpeedMult;
        SetHealth(GetMaxHealth() * hpRatio);
    }

    public void RefreshPlayerLimbs()
    {
        // Reset player graphics
        for (int i = 1; i < BODYPARTS.Length; i++) SetBodypart(i, 1, PLAYER_BASE_COLOR_RGBA);
        // Apply armor parts
        foreach (ArmorSlot armor in armors)
        {
            if (armor.slot == Slot.head)
            {
                SetBodypart(1, armor.armor.labelIndex, armor.armor.colorRGBA);
            }
            if (armor.slot == Slot.torso)
            {
                SetBodypart(2, armor.armor.labelIndex, armor.armor.colorRGBA);
                SetBodypart(3, armor.armor.labelIndex, armor.armor.colorRGBA);
            }
            if (armor.slot == Slot.arms) for (int i = 4; i < 10; i++) SetBodypart(i, armor.armor.labelIndex, armor.armor.colorRGBA);
            if (armor.slot == Slot.legs) for (int i = 10; i < 16; i++) SetBodypart(i, armor.armor.labelIndex, armor.armor.colorRGBA);
        }
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
        data.weapons = GetPlayerWeapons();
        data.armors = GetPlayerArmors();
        data.reloadTimer = reloadTimer;
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
        reloadTimer = data.reloadTimer;
        SetWeaponFromSlot(weaponSelected);
        RefreshPlayerLimbs();
        UpdateState();
        RefreshPlayerStats();
        StartCoroutine(ReloadTimerCoroutine(reloadTimer));
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
    public float reloadTimer = 0f;

}
