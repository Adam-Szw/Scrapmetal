using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuControl : MonoBehaviour
{
    public Button loadButton;
    public Button saveButton;
    public Button exitButton;

    private void Start()
    {
        exitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
        saveButton.onClick.AddListener(() =>
        {
            GlobalControl.Save();
        });
        loadButton.onClick.AddListener(() =>
        {
            GlobalControl.Load();
            UIControl.DestroyMenu();
        });
    }
}
