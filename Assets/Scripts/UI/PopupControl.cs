using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupControl : MonoBehaviour
{
    public TextMeshProUGUI popupTextObj;
    public Button okButton;
    public Image imageObj;
    public GameObject overlayPanel;

    private float timer = 0f;

    private static float fadeInTime = 0.5f;
    private static float fadeOutTime = 0.25f;

    private void Start()
    {
        okButton.onClick.AddListener(() =>
        {
            UIControl.DestroyPopup();
        });
        StartCoroutine(PopupFadeIn());
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    public void SelfDestruct()
    {
        StartCoroutine(PopupFadeOut());
    }

    public void Initialize(string text, string imageLink)
    {
        popupTextObj.text = text;
        imageObj.sprite = Resources.Load<Sprite>(imageLink);
    }

    private IEnumerator PopupFadeIn()
    {
        SetElementsAlpha(0f);
        timer = fadeInTime;
        while (timer > 0f)
        {
            SetElementsAlpha((fadeInTime - timer) / fadeInTime);
            yield return new WaitForSecondsRealtime(.01f);
            timer -= 0.01f;
        }
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
    }

    private void SetElementsAlpha(float alpha)
    {
        popupTextObj.alpha = alpha;
        Color c;
        c = okButton.image.color;
        c.a = alpha;
        okButton.image.color = c;
        c = imageObj.color;
        c.a = alpha;
        imageObj.color = c;
        c = overlayPanel.GetComponent<Image>().color;
        c.a = alpha;
        overlayPanel.GetComponent<Image>().color = c;
    }

}
