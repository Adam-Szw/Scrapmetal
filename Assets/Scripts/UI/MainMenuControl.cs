using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Settings;

public class MainMenuControl : MonoBehaviour
{
    // Scene version of main menu
    public GameObject scePanel;
    public Button sceNewGameBtn;
    public Button sceShowLoadBtn;
    public Button sceShowSettingBtn;
    public Button sceExitBtn;

    // In-game version of main menu
    public GameObject ingPanel;
    public Button ingShowLoadBtn;
    public Button ingShowSaveBtn;
    public Button ingShowSettingBtn;
    public Button ingResumeBtn;
    public Button ingExitBtn;

    // Saving panel
    public GameObject savPanel;
    public Button[] saveButtons;
    public Button savConfirmBtn;
    public Button savCancelBtn;

    // Loading panel
    public GameObject lodPanel;
    public Button[] loadButtons;
    public Button lodConfirmBtn;
    public Button lodCancelBtn;

    private bool inGameOpen = false;
    private int indexSelected = -1;

    private void Start()
    {
        // Showing load menu
        ingShowLoadBtn.onClick.AddListener(() => { OpenLoadMenu(); });
        sceShowLoadBtn.onClick.AddListener(() => { OpenLoadMenu(); });
        // Showing save menu
        ingShowSaveBtn.onClick.AddListener(() => { OpenSaveMenu(); });
        // Exit from save/load menu
        savCancelBtn.onClick.AddListener(() => { if (inGameOpen) OpenIngameMenu(); else OpenSceneMenu(); });
        lodCancelBtn.onClick.AddListener(() => { if (inGameOpen) OpenIngameMenu(); else OpenSceneMenu(); });
        // Exit ingame menu
        ingResumeBtn.onClick.AddListener(() => { UIControl.DestroyMenu(); });
        // Exit game
        sceExitBtn.onClick.AddListener(() => { Application.Quit(); });
        // Exit scene to menu
        ingExitBtn.onClick.AddListener(() =>
        {
            GlobalControl.SaveGame(4);
            GlobalControl.ReturnToTitle();
            OpenSceneMenu();
        });
        // Confirm saving
        savConfirmBtn.onClick.AddListener(() => {
            if (indexSelected != -1) GlobalControl.SaveGame(indexSelected);
            OpenSaveMenu();
        });
        // Confirm loading
        lodConfirmBtn.onClick.AddListener(() => {
            if (indexSelected != -1)
            {
                UIControl.DestroyMenu();
                GlobalControl.LoadGame(indexSelected);
            }
        });
        // New game
        sceNewGameBtn.onClick.AddListener(() =>
        {
            UIControl.DestroyMenu();
            GlobalControl.saveIndex = -1;
            GlobalControl.gameLoopOn = false;
            GlobalControl.SwitchScene("DeveloperRoom");
        });
    }

    public void OpenSceneMenu()
    {
        ingPanel.SetActive(false);
        savPanel.SetActive(false);
        lodPanel.SetActive(false);
        scePanel.SetActive(true);
        inGameOpen = false;
    }

    public void OpenIngameMenu()
    {
        scePanel.SetActive(false);
        savPanel.SetActive(false);
        lodPanel.SetActive(false);
        ingPanel.SetActive(true);
        inGameOpen = true;
    }

    private void OpenSaveMenu()
    {
        indexSelected = -1;
        scePanel.SetActive(false);
        ingPanel.SetActive(false);
        lodPanel.SetActive(false);
        savPanel.SetActive(true);
        savConfirmBtn.gameObject.SetActive(false);
        for (int i = 0; i < saveButtons.Length; i++) SetupSlotButton(true, saveButtons[i], i, "Unused", true);
        SetSaveOptions();
    }

    private void OpenLoadMenu()
    {
        indexSelected = -1;
        scePanel.SetActive(false);
        ingPanel.SetActive(false);
        savPanel.SetActive(false);
        lodPanel.SetActive(true);
        lodConfirmBtn.gameObject.SetActive(false);
        for (int i = 0; i < loadButtons.Length; i++) SetupSlotButton(false, loadButtons[i], -1, "Unused", false);
        SetLoadOptions();
    }

    private void SetSaveOptions()
    {
        Settings settings = Settings.GetSettings();

        foreach (KeyValuePair<int, SaveInfo> pair in settings.saves)
        {
            if (pair.Key >= saveButtons.Length) return;
            string text = "Date: " + pair.Value.date;
            text += " Location: " + pair.Value.location;
            SetupSlotButton(true, saveButtons[pair.Key], pair.Key, text, true);
        }
    }


    private void SetLoadOptions()
    {
        Settings settings = Settings.GetSettings();
        foreach (KeyValuePair<int, SaveInfo> pair in settings.saves)
        {
            if (pair.Key >= loadButtons.Length) return;
            string text = "Date: " + pair.Value.date;
            text += " Location: " + pair.Value.location;
            SetupSlotButton(true, loadButtons[pair.Key], pair.Key, text, false);
        }
    }


    private void SetupSlotButton(bool active, Button button, int index, string desc, bool forSaving)
    {
        button.transform.Find("Desc").gameObject.GetComponent<TextMeshProUGUI>().text = desc;
        button.onClick.RemoveAllListeners();
        if (active) button.onClick.AddListener(() =>
        {
            indexSelected = index;
            if (forSaving)
            {
                savConfirmBtn.gameObject.SetActive(true);
            }
            if (!forSaving) lodConfirmBtn.gameObject.SetActive(true);
        });
    }


}
