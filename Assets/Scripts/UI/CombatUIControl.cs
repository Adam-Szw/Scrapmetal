using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatUIControl : MonoBehaviour
{
    public GameObject healthbar;

    [HideInInspector] public HealthbarBehaviour healthbarBehaviour;

    private void Awake()
    {
        healthbarBehaviour = healthbar.GetComponent<HealthbarBehaviour>();
    }

}
