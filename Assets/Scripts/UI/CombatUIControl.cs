using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CombatUIControl : MonoBehaviour
{
    public GameObject healthbar;
    public GameObject ammoPanel;
    public TextMeshProUGUI ammoCounter;

    [HideInInspector] public HealthbarBehaviour healthbarBehaviour;

    private void Awake()
    {
        healthbarBehaviour = healthbar.GetComponent<HealthbarBehaviour>();
        StartCoroutine(UIUpdate());
        GlobalControl.GetPlayerBehaviour()?.SetHealthbar(healthbarBehaviour);
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    private void EnableAmmoPanel(bool enable)
    {
        ammoPanel.SetActive(enable);
    }

    private void UpdateAmmoCounter(int ammoCurr, int ammoMax)
    {
        ammoCounter.text = "Ammunition: " + ammoCurr + "/" + ammoMax;
    }

    private IEnumerator UIUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(.1f);
            PlayerBehaviour player = GlobalControl.GetPlayerBehaviour();
            if (player)
            {
                int? ammo = player.GetCurrWeaponAmmo();
                int? maxAmmo = player.GetCurrWeaponMaxAmmo();
                if (maxAmmo.HasValue && ammo.HasValue)
                {
                    EnableAmmoPanel(true);
                    UpdateAmmoCounter(ammo.Value, maxAmmo.Value);
                }
                else EnableAmmoPanel(false);
                player.SetHealthbar(healthbarBehaviour);
            }
            else
            {
                UIControl.DestroyCombatUI();
            }
        }
    }

}
