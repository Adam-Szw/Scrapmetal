using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthbarBehaviour : MonoBehaviour
{
    public float maxX;
    public float minX;
    public GameObject frame;
    public GameObject filler;
    public bool isUI = false;

    private Transform fillTransform = null;
    private SpriteRenderer fillRenderer = null;
    private SpriteRenderer frameRenderer = null;

    void Awake()
    {
        fillTransform = filler.transform;
        if (isUI) return;
        fillRenderer = filler.GetComponent<SpriteRenderer>();
        frameRenderer = frame.GetComponent<SpriteRenderer>();
        Enable(false);
    }

    public float GetHealthPercentage(float health, float maxHealth)
    {
        return Mathf.Clamp(health / Mathf.Max(maxHealth, 0.0001f), 0f, 100f);
    }

    public void UpdateHealthbar(float health, float maxHealth)
    {
        float hpPercentage = GetHealthPercentage(health, maxHealth);
        if (hpPercentage < 1.0f) Enable(true);
        if (hpPercentage >= 1.0f) Enable(false);
        if (hpPercentage <= 0f) Enable(false);
        if (fillTransform) fillTransform.localPosition = new Vector3(minX + hpPercentage * (maxX - minX), 0f, 0f);
    }

    private void Enable(bool enabled)
    {
        if (isUI) return;
        if (fillRenderer) fillRenderer.enabled = enabled;
        if (frameRenderer) frameRenderer.enabled = enabled;
    }

}