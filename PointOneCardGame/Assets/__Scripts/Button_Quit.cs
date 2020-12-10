using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class Button_Quit : MonoBehaviour
{
    //Quit, Rule, Game, Title 통합
    public void QuitGame()
    {
        Application.Quit();
    }

    public void ChangeRuleScene()
    {
        SceneManager.LoadScene("Rules");
    }

    public void ChangeGameScene()
    {
        SceneManager.LoadScene("_PointOneCardGame_Scene_0");
    }

    public void ChangeBackToTItleScene()
    {
        SceneManager.LoadScene("Title");
    }
}
