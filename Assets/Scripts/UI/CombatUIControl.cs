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
    }

    public void EnableAmmoPanel(bool enable)
    {
        ammoPanel.SetActive(enable);
    }

    public void UpdateAmmoCounter(int ammoMax, int ammoCurr)
    {
        ammoCounter.text = "Ammunition: " + ammoMax + "/" + ammoCurr;
    }

}
