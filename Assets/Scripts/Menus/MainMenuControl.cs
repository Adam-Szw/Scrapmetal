using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuControl : MonoBehaviour
{

    [SerializeField] private Button exitButton;

    private void Start()
    {
        exitButton.onClick.AddListener(() =>
        {
            ExitGame();
        });
    }

    public void ExitGame()
    {
        Application.Quit();
    }

}
