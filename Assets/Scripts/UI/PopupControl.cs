using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupControl : MonoBehaviour
{
    public delegate void Effect();

    public TextMeshProUGUI popupTextObj;
    public Button okButton;
    public GameObject overlayPanel;

    private float timer = 0f;
    private static float fadeOutTime = 0.25f;
    private Effect effect = null;

    private void OnDestroy()
    {
        StopAllCoroutines();
        effect?.Invoke();
    }

    public void SelfDestruct()
    {
        StartCoroutine(PopupFadeOut());
    }

    public void Initialize(string text, string imageLink, float fadeInTime, Effect specialEffect = null)
    {
        effect = specialEffect;
        okButton.onClick.AddListener(() =>
        {
            UIControl.DestroyPopup();
        });
        popupTextObj.text = text;
        StartCoroutine(PopupFadeIn(fadeInTime));
    }

    private IEnumerator PopupFadeIn(float fadeInTime)
    {
        SetElementsAlpha(0f);
        timer = fadeInTime;
        while (timer > 0f)
        {
            SetElementsAlpha((fadeInTime - timer) / fadeInTime);
            yield return new WaitForSecondsRealtime(.01f);
            timer -= 0.01f;
        }
        SetElementsAlpha(1f);
    }

    private IEnumerator PopupFadeOut()
    {
        SetElementsAlpha(1f);
        timer = fadeOutTime;
        while (timer > 0f)
        {
            SetElementsAlpha(timer / fadeOutTime);
            yield return new WaitForSecondsRealtime(.01f);
            timer -= 0.01f;
        }
        Destroy(gameObject);
        effect?.Invoke();
        effect = null;
    }

    private void SetElementsAlpha(float alpha)
    {
        popupTextObj.alpha = alpha;
        Color c;
        c = okButton.image.color;
        c.a = alpha;
        okButton.image.color = c;
        c = overlayPanel.GetComponent<Image>().color;
        c.a = alpha;
        overlayPanel.GetComponent<Image>().color = c;
    }

}
