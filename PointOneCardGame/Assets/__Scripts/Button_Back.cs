using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;



public class Button_Back : MonoBehaviour
{
    public GameObject Title1;
    public GameObject Title2;
    public GameObject Title3;
    public GameObject Title4;
    public GameObject Title5;

    // Button_Back, Button_Next 통합
    void Start()
    {
        Title1 = GameObject.Find("Title1");
        Title2 = GameObject.Find("Title2");
        Title3 = GameObject.Find("Title3");
        Title4 = GameObject.Find("Title4");
        Title5 = GameObject.Find("Title5");
    }
    
    public void ChangeBackImages()
    {
        if (Title1.activeSelf == true)
        {
            SceneManager.LoadScene("Title");
        }
        else if (Title1.activeSelf == false && Title2.activeSelf == true)
        {
            Title1.SetActive(true);
        }
        else if (Title2.activeSelf == false && Title3.activeSelf == true)
        {
            Title2.SetActive(true);
        }
        else if (Title3.activeSelf == false && Title4.activeSelf == true)
        {
            Title3.SetActive(true);
        }
        else if (Title4.activeSelf == false && Title5.activeSelf == true)
        {
            Title4.SetActive(true);
        }
    }

    public void ChangeNextImages()
    {
        if (Title1.activeSelf == true)
        {
            Title1.SetActive(false);
        }
        else if (Title2.activeSelf == true)
        {
            Title2.SetActive(false);
        }
        else if (Title3.activeSelf == true)
        {
            Title3.SetActive(false);
        }
        else if (Title4.activeSelf == true)
        {
            Title4.SetActive(false);
        }
        else if (Title5.activeSelf == true)
        {
            SceneManager.LoadScene("_PointOneCardGame_Scene_0");
        }
    }
}
