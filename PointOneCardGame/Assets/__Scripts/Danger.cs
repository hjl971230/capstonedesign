using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Danger : MonoBehaviour
{
    public static Danger S;
    public GameObject Danger1;
    public GameObject Danger2;

    private void Awake()
    {
        S = this;
    }

    void Start()
    {
        Danger1 = GameObject.Find("Danger1");
        Danger2 = GameObject.Find("Danger2");
        Danger1.SetActive(false);
        Danger2.SetActive(false);
    }

    //void Update()
    //{

    //    /* 일정횟수이상 지날시 ture시키고 클릭하면 이미지 false로 되돌리기
    //     * 둘중에 하나 랜덤으로 발생
    //     */
    //    if (Danger1.activeSelf == true)
    //    {
    //        if (Input.GetMouseButtonDown(0))
    //        {
    //            Danger1.SetActive(false);
    //        }
    //    }
    //    else if (Danger2.activeSelf == true)
    //    {
    //        if (Input.GetMouseButtonDown(0))
    //        {
    //            Danger2.SetActive(false);
    //        }
    //    }
    //}
}


