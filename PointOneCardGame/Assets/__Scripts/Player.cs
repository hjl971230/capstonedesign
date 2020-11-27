using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;

public enum PlayerType
{
    human,
    ai
}

[System.Serializable]
public class Player
{
    public PlayerType type = PlayerType.ai;
    public int playerNum;
    public List<Card> hand;
    public SlotDefPointOneCardGame handSlotdef;
    private int _score = 0;
    const int STD_SCORE = 10;
    public int score
    {
		get
		{
            return _score;
		}
		set
		{
            _score = value;
		}
    }

    public Card AddCard(Card eCB)
    {
        if (hand == null) hand = new List<Card>();
        hand.Add(eCB);

        if(type == PlayerType.human)
        {
            Card[] cards = hand.ToArray();

            cards = cards.OrderBy(cd => cd.rank).ToArray();

            hand = new List<Card>(cards);
        }

        eCB.SetSortingLayerName("10");
        eCB.eventualSortLayer = handSlotdef.layerName;

        FanHand();
        return eCB;
    }

    public Card RemoveCard(Card cb)
    {
        if (hand == null || !hand.Contains(cb)) return null;
        hand.Remove(cb);
        Manager.Sounds.PlayCardSound();
        FanHand();
        return cb;
    }

    public void FanHand()
    {
        float startRot = handSlotdef.rot;
        if(hand.Count > 1)
        {
            startRot += PointOneCardGame.S.handFanDegrees * (hand.Count - 1) / 2;
        }

        Vector3 pos;
        float rot;
        Quaternion rotQ;

        for (int i = 0; i < hand.Count; i++)
        {
            rot = startRot - PointOneCardGame.S.handFanDegrees * i;
            rotQ = Quaternion.Euler(0, 0, rot);

            pos = Vector3.up * Card.CARD_HEIGHT / 2f;
            pos = rotQ * pos;

            pos += handSlotdef.pos;

            pos.z = -0.5f * i;

            if(PointOneCardGame.S.phase != TurnPhase.idle)
            {
                hand[i].timeStart = 0;
            }

            hand[i].MoveTo(pos, rotQ);
            hand[i].state = CBState.toHand;

            //hand[i].transform.localPosition = pos;
            //hand[i].transform.rotation = rotQ;
            //hand[i].state = CBState.hand;

            hand[i].faceUP = (type == PlayerType.human);

            hand[i].eventualSortOrder = i * 4;
            //hand[i].SetSortOrder(i * 4);
        }
    }

    public void TakeTurn()
    {
        if (PointOneCardGame.S.jack != 0) PointOneCardGame.S.jack = 0;
        if (PointOneCardGame.S.king != 0) PointOneCardGame.S.king = 0;

        //Utils.tr("Player.TakeTurn");

        if (type == PlayerType.human) return;

		PointOneCardGame.S.phase = TurnPhase.waiting;
        Card cb = null;
        bool drawflag = true;

        List<Card> validCards = new List<Card>();
        List<Card> attackvalidCards = new List<Card>();

        foreach (Card tCB in hand)
        {
            if(PointOneCardGame.S.ValidPlay(tCB))
            {
                validCards.Add(tCB);
            }
        }

        foreach (Card tCB in validCards)
        {
            if (PointOneCardGame.S.AttackValidPlay(tCB))
            {
                attackvalidCards.Add(tCB);
            }
        }

        List<Card> comboList = new List<Card>();

        foreach (Card tmp in validCards)
        {
            if (PointOneCardGame.S.ValidCombo(tmp))
            {
                comboList.Add(tmp);
            }
        }

        if (attackvalidCards.Count == 0 && PointOneCardGame.attack_stack > 0)
        {
            Card aCB = null;
            for (int i = 0; i < PointOneCardGame.attack_stack; i++)
            {
                aCB = AddCard(PointOneCardGame.S.Draw());
            }
            score -= STD_SCORE * PointOneCardGame.attack_stack;
            PointOneCardGame.attack_stack = 0;
            if (aCB != null) aCB.callbackPlayer = this;
            return;
        }
        else if (attackvalidCards.Count > 0 && PointOneCardGame.attack_stack > 0)
        {
            cb = attackvalidCards[Random.Range(0, attackvalidCards.Count)];
            drawflag = false;
        }

        if (validCards.Count == 0)
        {
            cb = AddCard(PointOneCardGame.S.Draw());
            cb.callbackPlayer = this;
            _score -= STD_SCORE;
            return;
        }

        //if (comboList.Count > 0)
        //{
        //    if (comboList.Count > 1)
        //    {
        //        PointOneCardGame.S.combo_turn += 3 * PointOneCardGame.S.queen;
        //    }
        //    PointOneCardGame.S.combo_stack *= 2;
        //    cb = comboList[Random.Range(0, comboList.Count)];
        //    drawflag = false;
        //}
        //else if (comboList.Count <= 0)
        //{
        //    if(PointOneCardGame.S.combo_stack <= 1) PointOneCardGame.S.combo_turn = 0;
        //    PointOneCardGame.S.combo_stack = 1;
        //    drawflag = true;
        //}

        //if (PointOneCardGame.S.combo_stack <= 1) PointOneCardGame.S.combo_stack = 1;

        if (drawflag)
        {
            cb = validCards[Random.Range(0, validCards.Count)];
        }
        RemoveCard(cb);
        PointOneCardGame.S.MoveToTarget(cb);
        cb.callbackPlayer = this;
        switch (cb.rank)
        {
            case 1:
                if (cb.suit == "S")
                {
                    PointOneCardGame.attack_stack += 5;
                }
                else PointOneCardGame.attack_stack += 3;
                break;
            case 2:
                PointOneCardGame.attack_stack += 2;
                break;
            case 3:
                if(PointOneCardGame.attack_stack > 0) PointOneCardGame.attack_stack = 0;
                break;
            case 11:
                PointOneCardGame.S.jack = 1 * PointOneCardGame.S.queen;
                break;
            case 12:
                PointOneCardGame.S.queen *= -1;
                break;
            case 13:
                PointOneCardGame.S.king = 3 * PointOneCardGame.S.queen;
                break;
            case 14:
                PointOneCardGame.attack_stack += 7;
                break;
            case 15:
                PointOneCardGame.attack_stack += 10;
                break;
            default:
                break;
        }
        _score += STD_SCORE * PointOneCardGame.S.combo_stack;
        
    }

    public Card selectCard(List<Card> lCD)
    {
        List<Card> suitfindList;
        List<Card> rankfindList;
        List<List<Card>> suitCount = new List<List<Card>>(4);
        List<List<Card>> rankCount = new List<List<Card>>(13);

        suitCount[0] = lCD.FindAll(tmpsuit => tmpsuit.suit == "C");
        suitCount[1] = lCD.FindAll(tmpsuit => tmpsuit.suit == "D");
        suitCount[2] = lCD.FindAll(tmpsuit => tmpsuit.suit == "H");
        suitCount[3] = lCD.FindAll(tmpsuit => tmpsuit.suit == "S");

        int suitidx = -1;
        switch (PointOneCardGame.S.targetCard.suit)
        {
            case "C":
                suitidx = 0;
                break;
            case "D":
                suitidx = 1;
                break;
            case "H":
                suitidx = 2;
                break;
            case "S":
                suitidx = 3;
                break;
        }

        for (int i = 1; i <= 13; i++)
        {
            rankCount[i - 1] = lCD.FindAll(tmprank => tmprank.rank == i);
        }

        suitfindList = lCD.FindAll(tmpsuit => tmpsuit.suit == PointOneCardGame.S.targetCard.suit);
        rankfindList = lCD.FindAll(tmprank => tmprank.rank == PointOneCardGame.S.targetCard.rank);

        if(suitCount[suitidx].Count > 0 && rankCount[PointOneCardGame.S.targetCard.rank - 1].Count > 0)
        {
            if (suitCount[suitidx].Count > lCD.Count / 2)
            {
                return lCD[suitCount[suitidx].Count];
            }
            else
            {
                return lCD.Find(tmprank => tmprank.rank == lCD.Max().rank);
            }
        }

        foreach (Card tCB in lCD)
        {
            if (PointOneCardGame.S.targetCard.rank == tCB.rank && PointOneCardGame.S.targetCard.suit == tCB.suit)
            {
                if (suitCount[suitidx].Count > lCD.Count / 2)
                {
                    return tCB;
                }
            }
            else if (PointOneCardGame.S.targetCard.rank == tCB.rank || PointOneCardGame.S.targetCard.suit == tCB.suit)
            {
                return tCB;
            }
        }

        return null;
    }

    public void CBCallback(Card tCB)
    {
        //Utils.tr("Player.CBCallback()", tCB.name, "Player " + playerNum);
        PointOneCardGame.S.PassTurn();
    }
}
