using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonMenuUI : MonoBehaviour
{ 
    [Header("Set Dynamically")]
    public Image menuImage;
    
    //public Card tCard; //targetcard 저장

    void Start()
    {
        menuImage = GetComponent<Image>();
        ShowMenu(false);
    }

    public void ShowMenu(bool show)//메뉴 보이기
    {
        menuImage.gameObject.SetActive(show);
        /*
        if(!set) //set이 true인 상태에서는 호출하지 않음
        {
            set = true;
            if(show) // 메뉴를 보이려면
            {
                menuImage.gameObject.SetActive(show);
            }
            if(!show) // 메뉴를 닫을려면
            {
                menuImage.gameObject.SetActive(show);
            }
        }
        */
    }

    /*
    public void GetCard(Card card)//targetCard 얻기
    {
        //tCard = new Card();
        tCard = card;
    }
    */

    //card click event
    public void ChangeButtonSpade()
    {
        print("SpadeClick");
        PointOneCardGame.S.deck.ChangeCard(PointOneCardGame.S.targetCard, "S");
        ShowMenu(false);
        PointOneCardGame.S.sevenflag = false;
    }

    public void ChangeButtonClub()
    {
        print("ClubClick");
        PointOneCardGame.S.deck.ChangeCard(PointOneCardGame.S.targetCard, "C");
        ShowMenu(false);
        PointOneCardGame.S.sevenflag = false;
    }

    public void ChangeButtonHeart()
    {
        print("HeartClick");
        PointOneCardGame.S.deck.ChangeCard(PointOneCardGame.S.targetCard, "H");
        ShowMenu(false);
        PointOneCardGame.S.sevenflag = false;
    }

    public void ChangeButtonDiamond()
    {
        print("DiamondClick");
        PointOneCardGame.S.deck.ChangeCard(PointOneCardGame.S.targetCard, "D");
        ShowMenu(false);
        PointOneCardGame.S.sevenflag = false;
    }
}
