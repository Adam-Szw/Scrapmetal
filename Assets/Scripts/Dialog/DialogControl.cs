using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogControl : MonoBehaviour 
{
    public Button[] dialogBtns;
    public TextMeshProUGUI dialogTitle;
    public TextMeshProUGUI dialogName;

    private List<DialogOption> options = new List<DialogOption>();
    private int indxCurr = 0;
    private Button btnNext = null;

    private void Awake()
    {
        foreach (Button b in dialogBtns) AddTrigger(b);
    }

    private void ClearDialog()
    {
        foreach (Button b in dialogBtns) EnableButton(false, b);
        foreach (Button b in dialogBtns) b.GetComponentInChildren<TextMeshProUGUI>().text = "";
        dialogTitle.text = "";
        indxCurr = 0;
        btnNext = dialogBtns[indxCurr];
        options.Clear();
    }

    private void EnableButton(bool enable, Button button)
    {
        button.image.enabled = enable;
        button.interactable = enable;
        button.GetComponentInChildren<TextMeshProUGUI>().alpha = enable ? 255f : 0f;
    }

    private void AddTrigger(Button button)
    {
        int btnIndex = int.Parse(button.name.Substring(button.name.Length - 1));
        button.onClick.AddListener(() =>
        {
            DialogOption optionSelected = options[btnIndex - 1];
            UseOption(optionSelected);
        });
    }

    private Button GetNextBtn()
    {
        Button toRet = btnNext;
        indxCurr++;
        if (indxCurr >= dialogBtns.Length)
        {
            btnNext = null;
            return toRet;
        }
        else btnNext = dialogBtns[indxCurr];
        return toRet;
    }

    private void AddOption(DialogOption option)
    {
        Button btn = GetNextBtn();
        if (!btn) return;
        options.Add(option);
        EnableButton(true, btn);
        btn.GetComponentInChildren<TextMeshProUGUI>().text = DialogLibrary.GetDialogOptionText(option.ID);
    }

    private void UseOption(DialogOption option)
    {
        if (option.doClearDialog) ClearDialog();
        dialogTitle.text = DialogLibrary.GetDialogOptionResponse(option.ID);
        option.DoEffect();
        foreach (ulong oID in option.optionIDsResulting)
        {
            DialogOption optNew = DialogLibrary.getDialogOptionByID(oID);
            if (optNew != null) AddOption(optNew);
        }
    }

    public void Initialize(DialogOption initialOption, string dialogRespondentName)
    {
        ClearDialog();
        dialogName.text = dialogRespondentName;
        UseOption(initialOption);
    }
}
