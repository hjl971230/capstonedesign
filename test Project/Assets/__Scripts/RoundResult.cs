using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoundResult : MonoBehaviour
{
    private Text txt;

    private void Awake()
    {
        txt = GetComponent<Text>();
        txt.text = "";
    }

    void Update()
    {
        if (Bartok.S.phase != TurnPhase.gameOver)
        {
            txt.text = "";
            return;
        }

        Player cp = Bartok.CURRENT_PLAYER;

        if (cp == null || cp.type == PlayerType.human)
        {
            txt.text = "";
        }
        else
        {
            txt.text = "Player " + cp.playerNum + " Won";
        }
    }
}
