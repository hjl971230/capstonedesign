using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
//using UnityEditor.SceneManagement;

public enum PlayerType
{
    human,
    ai,
    gameend
}

public enum AiType
{
    type1,
    type2,
    type3
}

[System.Serializable]
public class Player
{
    public PlayerType type = PlayerType.ai;
    public AiType aiType = AiType.type1;
    public int playerNum;
    public string PlayerName;
    public List<Card> hand;
    public SlotDefPointOneCardGame handSlotdef;
    private int _score = 0;
    const int STD_SCORE = 10;
    private int _rank = 0;
    List<Card> comboList = new List<Card>();

    public int score
    {
        get{ return _score; }
        set{ _score = value; }
    }

    public int rank
    {
        get{ return _rank; }
        set { _rank = value; }
    }


    public Card AddCard(Card eCB)
    {
        if (hand == null) hand = new List<Card>();
        hand.Add(eCB);

        if (type == PlayerType.human)
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
        if (hand.Count > 1)
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

            if (PointOneCardGame.S.phase != TurnPhase.idle)
            {
                hand[i].timeStart = 0;
            }

            hand[i].MoveTo(pos, rotQ);
            hand[i].state = CBState.toHand;

            //hand[i].transform.localPosition = pos;
            //hand[i].transform.rotation = rotQ;
            //hand[i].state = CBState.hand;

            hand[i].faceUP = (type == PlayerType.human);
            //hand[i].faceUP = true;

            hand[i].eventualSortOrder = i * 4;
            //hand[i].SetSortOrder(i * 4);
        }
    }

    public void TakeTurn()
    {
        if (PointOneCardGame.S.jack != 0) PointOneCardGame.S.jack = 0;
        if (PointOneCardGame.S.king != 0) PointOneCardGame.S.king = 0;

        if (type == PlayerType.human || type == PlayerType.gameend) return;

        PointOneCardGame.S.phase = TurnPhase.waiting;
        Card cb = null;
        bool drawflag = true;
        bool comboflag = false;

        List<Card> validCards = new List<Card>();
        List<Card> attackvalidCards = new List<Card>();

        foreach (Card tCB in hand)
        {
            if (PointOneCardGame.S.ValidPlay(tCB))
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

        if (attackvalidCards.Count == 0 && PointOneCardGame.attack_stack > 0 && comboList.Count <= 0)
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
        else if (attackvalidCards.Count > 0 && PointOneCardGame.attack_stack > 0 && comboList.Count <= 0)
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

        List<Player> alonePlayers = new List<Player>();
        foreach (var tmp in PointOneCardGame.S.players)
        {
            if (PointOneCardGame.S.checkAlone(tmp))
            {
                alonePlayers.Add(tmp);
            }
        }

        if (comboList.Count >= 1) comboflag = true;
        
        if (drawflag)
        {
            if(comboflag)
            {
                cb = comboList[Random.Range(0, comboList.Count)];
                comboList.Clear();
            }
            else cb = validCards[Random.Range(0, validCards.Count)];
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
                if (PointOneCardGame.attack_stack > 0) PointOneCardGame.attack_stack = 0;
                break;
            case 7:
                PointOneCardGame.S.deck.ChangeCard(PointOneCardGame.S.targetCard, ReturnSuit());
                break;
            case 11:
                if (alonePlayers.Count >= 2)
                    PointOneCardGame.S.jack = (PointOneCardGame.S.players.Count - 1) * PointOneCardGame.S.queen;
                else PointOneCardGame.S.jack = 1 * PointOneCardGame.S.queen;
                PointOneCardGame.S.jack = 1 * PointOneCardGame.S.queen;
                break;
            case 12:
                PointOneCardGame.S.queen *= -1;
                break;
            case 13:
                PointOneCardGame.S.king = (PointOneCardGame.S.players.Count - 1) * PointOneCardGame.S.queen;
                PointOneCardGame.S.combo_stack *= 2;
                if (PointOneCardGame.S.combo_turn <= 0)
                {
                    cb.callbackPlayer = this;
                }
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

        if (cb.rank != 11) PointOneCardGame.S.jack = 0;
        if (cb.rank != 13) PointOneCardGame.S.king = 0;
        foreach (Card tmp in hand)
        {
            if (PointOneCardGame.S.ValidCombo(tmp))
            {
                comboList.Add(tmp);
            }
        }

        if (comboList.Count >= 1)
        {
            PointOneCardGame.S.combo_stack *= 2;
            if (comboList.Find(tmp => tmp.rank == 11)) PointOneCardGame.S.jack = 0;
            //if(comboList.Find(tmp => tmp.rank == 12))queen = 1;
            PointOneCardGame.S.combo_turn = (PointOneCardGame.S.players.Count - 1) * PointOneCardGame.S.queen;
            cb.callbackPlayer = this;
        }
        else if (comboList.Count <= 0)
        {
            PointOneCardGame.S.combo_turn = 0;
            PointOneCardGame.S.combo_stack = 1;
        }

        if (PointOneCardGame.S.combo_stack <= 1) PointOneCardGame.S.combo_stack = 1;
    }

    //public Card selectCard(List<Card> lCD)
    //{
    //    List<Card> suitfindList;
    //    List<Card> rankfindList;
    //    List<List<Card>> suitCount = new List<List<Card>>(4);
    //    List<List<Card>> rankCount = new List<List<Card>>(13);

    //    suitCount[0] = lCD.FindAll(tmpsuit => tmpsuit.suit == "C");
    //    suitCount[1] = lCD.FindAll(tmpsuit => tmpsuit.suit == "D");
    //    suitCount[2] = lCD.FindAll(tmpsuit => tmpsuit.suit == "H");
    //    suitCount[3] = lCD.FindAll(tmpsuit => tmpsuit.suit == "S");

    //    int suitidx = -1;
    //    switch (PointOneCardGame.S.targetCard.suit)
    //    {
    //        case "C":
    //            suitidx = 0;
    //            break;
    //        case "D":
    //            suitidx = 1;
    //            break;
    //        case "H":
    //            suitidx = 2;
    //            break;
    //        case "S":
    //            suitidx = 3;
    //            break;
    //    }

    //    for (int i = 1; i <= 13; i++)
    //    {
    //        rankCount[i - 1] = lCD.FindAll(tmprank => tmprank.rank == i);
    //    }

    //    suitfindList = lCD.FindAll(tmpsuit => tmpsuit.suit == PointOneCardGame.S.targetCard.suit);
    //    rankfindList = lCD.FindAll(tmprank => tmprank.rank == PointOneCardGame.S.targetCard.rank);

    //    if (suitCount[suitidx].Count > 0 && rankCount[PointOneCardGame.S.targetCard.rank - 1].Count > 0)
    //    {
    //        if (suitCount[suitidx].Count > lCD.Count / 2)
    //        {
    //            return lCD[suitCount[suitidx].Count];
    //        }
    //        else
    //        {
    //            return lCD.Find(tmprank => tmprank.rank == lCD.Max().rank);
    //        }
    //    }

    //    foreach (Card tCB in lCD)
    //    {
    //        if (PointOneCardGame.S.targetCard.rank == tCB.rank && PointOneCardGame.S.targetCard.suit == tCB.suit)
    //        {
    //            if (suitCount[suitidx].Count > lCD.Count / 2)
    //            {
    //                return tCB;
    //            }
    //        }
    //        else if (PointOneCardGame.S.targetCard.rank == tCB.rank || PointOneCardGame.S.targetCard.suit == tCB.suit)
    //        {
    //            return tCB;
    //        }
    //    }

    //    return null;
    //}

    public Player nextHandCheck()
    {
        int num;
        Player checkTarget = null;

        if(PointOneCardGame.S.queen == 1)
        {
            for (int i = 0; i < PointOneCardGame.S.players.Count; i++)
            {
                if(PointOneCardGame.S.players[i] == this)
                {
                    num = i + 1;
                    if(num >= PointOneCardGame.S.players.Count) { num %= PointOneCardGame.S.players.Count; }
                    checkTarget = PointOneCardGame.S.players[num];
                    break;
                }
            }
            return checkTarget;
        }
        else
        {
            for (int i = 0; i > -PointOneCardGame.S.players.Count; i--)
            {
                if (PointOneCardGame.S.players[i] == this)
                {
                    num = i - 1;
                    if (num < 0) num += PointOneCardGame.S.players.Count;
                    if (num >= PointOneCardGame.S.players.Count) { num %= PointOneCardGame.S.players.Count; }
                    checkTarget = PointOneCardGame.S.players[num];
                    break;
                }
            }
            return checkTarget;
        }
    }

    public Card selectCard(List<Card> lCD)
    {
        int attack = PointOneCardGame.attack_stack;
        int fullHand = PointOneCardGame.fullHand;
        Player nextPlayer = nextHandCheck();
        List<Player> Players = PointOneCardGame.S.players;
        int cardAttackPoint = 0;
        var findAllAttack = hand.FindAll(tmp => tmp.suit == PointOneCardGame.S.targetCard.suit && tmp.rank <= 2 || tmp.rank >= 14);
        var findAllGuard = hand.FindAll(tmp => tmp.suit == PointOneCardGame.S.targetCard.suit && tmp.rank == 3);
        List<Card> attackList = findAllAttack.ToList();
        List<Card> guardList = findAllGuard.ToList();
        List<Card> specialCardList = attackList;
        specialCardList.AddRange(guardList);
        List<Card> tmpList = new List<Card>();
        List<Player> tmpPlayers = new List<Player>();
        Card tmpCard;
        List<Card> comboList = new List<Card>();

        if (PointOneCardGame.attack_stack >= 1)//1. 공격을 받는 경우
        {
            if(hand.Count + attack > fullHand)//1-1) 파산이 되는 경우
            {
                // 1)) 공격, 방어 카드의 확인 
                if (attackList.Count >= 1 && guardList.Count >= 1)//1-1)) 둘 다 있는 경우
                {
                    foreach (var item in attackList)//1} 다음 차례의 카드 수 확인
                    {
                        switch (item.rank)
                        {
                            case 1:
                                if (item.suit == "S")
                                    cardAttackPoint = 5;
                                else cardAttackPoint = 3;
                                break;
                            case 2:
                                cardAttackPoint = 2;
                                break;
                            case 14:
                                cardAttackPoint = 7;
                                break;
                            case 15:
                                cardAttackPoint = 10;
                                break;
                            default:
                                break;
                        }

                        if(nextPlayer.hand.Count + cardAttackPoint + attack >= fullHand)
                        {
                            tmpList.Add(item);
                        }
                    }
                    if(tmpList.Count >= 1)//1-1} 공격을 넘겨서 파산이 가능
                    {
                        tmpPlayers = Players;
                        tmpPlayers.Remove(nextPlayer);//- 다음 차례를 제외한 3인의 카드의 수를 확인
                        var tmp = from n in tmpPlayers orderby n.hand.Count descending, n.score ascending select n;
                        // 카드 수 오름차순 정렬, 점수 내림차순 정렬
                        tmpPlayers = tmp.ToList();
                        if (tmpPlayers[0] == this)//-3인의 카드의 수에 따라 차등점수를 더하고 더했을시 현재플레이어가 가장 높게 나온다면 실행
                        {
                            return tmpList[Random.Range(0, attackList.Count)];
                        }
                        else return guardList[Random.Range(0, guardList.Count)];//-현재 플레이어의 수가 가장 많다면 방어카드
                    }
                    else//1-2} 공격을 넘겨도 파산이 불가능
                    {
                        //다음 차례의 점수와 나의 점수를 비교
                        if (nextPlayer.score > score && nextPlayer.hand.Count > hand.Count)//1] 다음 차례가 점수가 높고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 점수가 높으니 점수도 떨어뜨리고 카드의 개수가 많으니 다음 차례에 파산을 유도하겠다
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))//(최대한 많은 카드를 받을 수 있게 Joker, A ,2 콤보 포함 순 으로 공격)
                                        return tmpCard;
                                    else if(tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 점수가 높고 카드도 많으니 콤보가 될 확률도 있고 더 이상 카드를 주지 않겠다
                                    if (tmpCard = attackList.Find(n => n.rank == 2))//(최대한 적은 카드를 받을 수 있게 2 , A, Joker 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                                case AiType.type3:
                                    //타입3: 굳이 카드를 줄 이유가 없다 -> 방어
                                    return guardList[Random.Range(0, guardList.Count)];
                            }
                        }
                        else if(nextPlayer.score > score && nextPlayer.hand.Count < hand.Count)//2] 다음 차례가 점수가 높고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //점수가 높으니 점수도 떨어뜨리고 내가 먼저 끝낼 가능성을 높이기 위해 상대방에게 많은 카드를 주겠다
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))//(최대한 많은 카드를 받을 수 있게 Joker, A ,2 콤보 포함 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                case AiType.type3:
                                    //타입2: 점수가 높으나 카드도 적어 공격을 받아도 영향이 없고 더 도움만 될 것이다
                                    if (tmpCard = attackList.Find(n => n.rank == 2))//(최대한 적은 카드를 받을 수 있게 2 , A, Joker 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                            }
                        }
                        else if (nextPlayer.score == score && nextPlayer.hand.Count > hand.Count)//3] 다음 차례가 점수가 비슷하고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 점수가 높으니 점수도 떨어뜨리고 카드의 개수가 많으니 다음 차례에 파산을 유도하겠다
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))//(최대한 많은 카드를 받을 수 있게 Joker, A ,2 콤보 포함 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 점수가 높고 카드도 많으니 콤보가 될 확률도 있고 더 이상 카드를 주지 않겠다
                                    if (tmpCard = attackList.Find(n => n.rank == 2))//(최대한 적은 카드를 받을 수 있게 2 , A, Joker 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                                case AiType.type3:
                                    //타입3: 굳이 카드를 줄 이유가 없다 -> 방어
                                    return guardList[Random.Range(0, guardList.Count)];
                            }
                        }
                        else if (nextPlayer.score == score && nextPlayer.hand.Count < hand.Count)//4] 다음 차례가 점수가 비슷하고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 점수가 높으니 점수도 떨어뜨리고 카드의 개수가 많으니 다음 차례에 파산을 유도하겠다
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))//(최대한 많은 카드를 받을 수 있게 Joker, A ,2 콤보 포함 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 점수가 높고 카드도 많으니 콤보가 될 확률도 있고 더 이상 카드를 주지 않겠다
                                    if (tmpCard = attackList.Find(n => n.rank == 2))//(최대한 적은 카드를 받을 수 있게 2 , A, Joker 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                                case AiType.type3:
                                    //타입3: 굳이 카드를 줄 이유가 없다 -> 방어
                                    return guardList[Random.Range(0, guardList.Count)];
                            }
                        }
                        else if (nextPlayer.score < score && nextPlayer.hand.Count > hand.Count)//5] 다음 차례가 점수가 적고 카드의 개수가 많다
                        {
                            return guardList[Random.Range(0, guardList.Count)];//-> 방어
                        }
                        else if (nextPlayer.score < score && nextPlayer.hand.Count < hand.Count)//6] 다음 차례가 점수가 적고 카드의 개수가 적다
                        {
                            return guardList[Random.Range(0, guardList.Count)];//-> 방어
                        }
                    }
                }
                else if(attackList.Count >= 1 && guardList.Count <= 0)// 1 - 2)) 공격 카드만 있는 경우
                {
                    //1} 다음 차례의 카드 수 확인
                    foreach (var item in attackList)
                    {
                        switch (item.rank)
                        {
                            case 1:
                                if (item.suit == "S")
                                    cardAttackPoint = 5;
                                else cardAttackPoint = 3;
                                break;
                            case 2:
                                cardAttackPoint = 2;
                                break;
                            case 14:
                                cardAttackPoint = 7;
                                break;
                            case 15:
                                cardAttackPoint = 10;
                                break;
                            default:
                                break;
                        }

                        if (nextPlayer.hand.Count + cardAttackPoint + attack >= fullHand)
                        {
                            tmpList.Add(item);
                        }
                    }
                    if (tmpList.Count >= 1)//1-1} 공격을 넘겨서 파산이 가능
                    {
                        tmpPlayers = Players;
                        tmpPlayers.Remove(nextPlayer);//- 다음 차례를 제외한 3인의 카드의 수를 확인
                        var tmp = from n in tmpPlayers orderby n.hand.Count descending, n.score ascending select n;
                        // 카드 수 오름차순 정렬, 점수 내림차순 정렬
                        tmpPlayers = tmp.ToList();
                        //그냥 공격
                        return tmpList[Random.Range(0, attackList.Count)];
                    }
                    else//1-2} 공격을 넘겨도 파산이 불가능
                    {
                        //다음 차례의 점수와 나의 점수를 비교
                        if (nextPlayer.score > score && nextPlayer.hand.Count > hand.Count)//1] 다음 차례가 점수가 높고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 점수가 높으니 점수도 떨어뜨리고 카드의 개수가 많으니 다음 차례에 파산을 유도하겠다
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))//(최대한 많은 카드를 받을 수 있게 Joker, A ,2 콤보 포함 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                case AiType.type3:
                                    //타입2: 점수가 높고 카드도 많으니 콤보가 될 확률도 있고 더 이상 카드를 주지 않겠다
                                    if (tmpCard = attackList.Find(n => n.rank == 2))//(최대한 적은 카드를 받을 수 있게 2 , A, Joker 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                            }
                        }
                        else if (nextPlayer.score > score && nextPlayer.hand.Count < hand.Count)//2] 다음 차례가 점수가 높고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //점수가 높으니 점수도 떨어뜨리고 내가 먼저 끝낼 가능성을 높이기 위해 상대방에게 많은 카드를 주겠다
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))//(최대한 많은 카드를 받을 수 있게 Joker, A ,2 콤보 포함 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                case AiType.type3:
                                    //타입2: 점수가 높으나 카드도 적어 공격을 받아도 영향이 없고 더 도움만 될 것이다
                                    if (tmpCard = attackList.Find(n => n.rank == 2))//(최대한 적은 카드를 받을 수 있게 2 , A, Joker 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                            }
                        }
                        else if (nextPlayer.score == score && nextPlayer.hand.Count > hand.Count)//3] 다음 차례가 점수가 비슷하고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //점수가 높으니 점수도 떨어뜨리고 내가 먼저 끝낼 가능성을 높이기 위해 상대방에게 많은 카드를 주겠다
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))//(최대한 많은 카드를 받을 수 있게 Joker, A ,2 콤보 포함 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                case AiType.type3:
                                    //타입2: 점수가 높으나 카드도 적어 공격을 받아도 영향이 없고 더 도움만 될 것이다
                                    if (tmpCard = attackList.Find(n => n.rank == 2))//(최대한 적은 카드를 받을 수 있게 2 , A, Joker 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                            }
                        }
                        else if (nextPlayer.score == score && nextPlayer.hand.Count < hand.Count)//4] 다음 차례가 점수가 비슷하고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //점수가 높으니 점수도 떨어뜨리고 내가 먼저 끝낼 가능성을 높이기 위해 상대방에게 많은 카드를 주겠다
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))//(최대한 많은 카드를 받을 수 있게 Joker, A ,2 콤보 포함 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                case AiType.type3:
                                    //타입2: 점수가 높으나 카드도 적어 공격을 받아도 영향이 없고 더 도움만 될 것이다
                                    if (tmpCard = attackList.Find(n => n.rank == 2))//(최대한 적은 카드를 받을 수 있게 2 , A, Joker 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                            }
                        }
                        else if (nextPlayer.score < score && nextPlayer.hand.Count > hand.Count)//5] 다음 차례가 점수가 적고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                case AiType.type2:
                                case AiType.type3:
                                    //타입2: 점수가 높으나 카드도 적어 공격을 받아도 영향이 없고 더 도움만 될 것이다
                                    if (tmpCard = attackList.Find(n => n.rank == 2))//(최대한 적은 카드를 받을 수 있게 2 , A, Joker 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                            }
                        }
                        else if (nextPlayer.score < score && nextPlayer.hand.Count < hand.Count)//6] 다음 차례가 점수가 적고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                case AiType.type2:
                                case AiType.type3:
                                    //타입2: 점수가 높으나 카드도 적어 공격을 받아도 영향이 없고 더 도움만 될 것이다
                                    if (tmpCard = attackList.Find(n => n.rank == 2))//(최대한 적은 카드를 받을 수 있게 2 , A, Joker 순 으로 공격)
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                            }
                        }
                    }
                }
                else if(attackList.Count <= 0 && guardList.Count >= 1)//1-3)) 방어 카드만 있는 경우
                {
                    return guardList[Random.Range(0, guardList.Count)];// -> 방어 카드를 낸다
                }
            }
            else
            {
                // 1)) 공격, 방어 카드의 확인 
                if (attackList.Count >= 1 && guardList.Count >= 1)//1-1)) 둘 다 있는 경우
                {
                    foreach (var item in attackList)//1} 다음 차례의 카드 수 확인
                    {
                        switch (item.rank)
                        {
                            case 1:
                                if (item.suit == "S")
                                    cardAttackPoint = 5;
                                else cardAttackPoint = 3;
                                break;
                            case 2:
                                cardAttackPoint = 2;
                                break;
                            case 14:
                                cardAttackPoint = 7;
                                break;
                            case 15:
                                cardAttackPoint = 10;
                                break;
                            default:
                                break;
                        }

                        if (nextPlayer.hand.Count + cardAttackPoint + attack >= fullHand)
                        {
                            tmpList.Add(item);
                        }
                    }
                    if (tmpList.Count >= 1)//1-1} 공격을 넘겨서 파산이 가능
                    {
                        tmpPlayers = Players;
                        tmpPlayers.Remove(nextPlayer);//- 다음 차례를 제외한 3인의 카드의 수를 확인
                        var tmp = from n in tmpPlayers orderby n.hand.Count descending, n.score ascending select n;
                        // 카드 수 오름차순 정렬, 점수 내림차순 정렬
                        tmpPlayers = tmp.ToList();
                        return tmpList[Random.Range(0, attackList.Count)];
                    }
                    else//1-2} 공격을 넘겨도 파산이 불가능
                    {
                        //다음 차례의 점수와 나의 점수를 비교
                        if (nextPlayer.score > score && nextPlayer.hand.Count > hand.Count)//1] 다음 차례가 점수가 높고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 공격 ( Joker, a, 2 순)
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 방어
                                    return guardList[Random.Range(0, guardList.Count)];
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                        else if (nextPlayer.score > score && nextPlayer.hand.Count < hand.Count)//2] 다음 차례가 점수가 높고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 공격 ( Joker, a, 2 순)
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 방어
                                    return guardList[Random.Range(0, guardList.Count)];
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                        else if (nextPlayer.score == score && nextPlayer.hand.Count > hand.Count)//3] 다음 차례가 점수가 비슷하고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 공격 (여기선 2,a,joker순)
                                    if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 방어
                                    return guardList[Random.Range(0, guardList.Count)];
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                        else if (nextPlayer.score == score && nextPlayer.hand.Count < hand.Count)//4] 다음 차례가 점수가 비슷하고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 공격 ( Joker, a, 2 순)
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 방어
                                    return guardList[Random.Range(0, guardList.Count)];
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                        else if (nextPlayer.score < score && nextPlayer.hand.Count > hand.Count)//5] 다음 차례가 점수가 적고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 공격 (여기선 2,a,joker순)
                                    if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 방어
                                    return guardList[Random.Range(0, guardList.Count)];
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                        else if (nextPlayer.score < score && nextPlayer.hand.Count < hand.Count)//6] 다음 차례가 점수가 적고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 공격 ( Joker, a, 2 순)
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 방어
                                    return guardList[Random.Range(0, guardList.Count)];
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                    }
                }
                else if (attackList.Count >= 1 && guardList.Count <= 0)// 1 - 2)) 공격 카드만 있는 경우
                {
                    //1} 다음 차례의 카드 수 확인
                    foreach (var item in attackList)
                    {
                        switch (item.rank)
                        {
                            case 1:
                                if (item.suit == "S")
                                    cardAttackPoint = 5;
                                else cardAttackPoint = 3;
                                break;
                            case 2:
                                cardAttackPoint = 2;
                                break;
                            case 14:
                                cardAttackPoint = 7;
                                break;
                            case 15:
                                cardAttackPoint = 10;
                                break;
                            default:
                                break;
                        }

                        if (nextPlayer.hand.Count + cardAttackPoint + attack >= fullHand)
                        {
                            tmpList.Add(item);
                        }
                    }
                    if (tmpList.Count >= 1)//1-1} 공격을 넘겨서 파산이 가능
                    {
                        tmpPlayers = Players;
                        tmpPlayers.Remove(nextPlayer);//- 다음 차례를 제외한 3인의 카드의 수를 확인
                        var tmp = from n in tmpPlayers orderby n.hand.Count descending, n.score ascending select n;
                        // 카드 수 오름차순 정렬, 점수 내림차순 정렬
                        tmpPlayers = tmp.ToList();
                        //그냥 공격
                        return tmpList[Random.Range(0, attackList.Count)];
                    }
                    else//1-2} 공격을 넘겨도 파산이 불가능
                    {
                        //다음 차례의 점수와 나의 점수를 비교
                        if (nextPlayer.score > score && nextPlayer.hand.Count > hand.Count)//1] 다음 차례가 점수가 높고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 공격 ( Joker, a, 2 순)
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 방어적 (2, a, joker 순)
                                    if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                        else if (nextPlayer.score > score && nextPlayer.hand.Count < hand.Count)//2] 다음 차례가 점수가 높고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                    //타입1: 공격 ( Joker, a, 2 순)
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type2:
                                    //타입2: 방어적 (2, a, joker 순)
                                    if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                        else if (nextPlayer.score == score && nextPlayer.hand.Count > hand.Count)//3] 다음 차례가 점수가 비슷하고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                case AiType.type2:
                                    //타입2: 방어적 (2, a, joker 순)
                                    if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    break;
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                        else if (nextPlayer.score == score && nextPlayer.hand.Count < hand.Count)//4] 다음 차례가 점수가 비슷하고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                case AiType.type2:
                                    //타입1: 공격 ( Joker, a, 2 순)
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                        else if (nextPlayer.score < score && nextPlayer.hand.Count > hand.Count)//5] 다음 차례가 점수가 적고 카드의 개수가 많다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                case AiType.type2:
                                    //타입1: 공격 ( Joker, a, 2 순)
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                        else if (nextPlayer.score < score && nextPlayer.hand.Count < hand.Count)//6] 다음 차례가 점수가 적고 카드의 개수가 적다
                        {
                            switch (aiType)
                            {
                                case AiType.type1:
                                case AiType.type2:
                                    //타입1: 공격 ( Joker, a, 2 순)
                                    if (tmpCard = attackList.Find(n => n.rank >= 14))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 1))
                                        return tmpCard;
                                    else if (tmpCard = attackList.Find(n => n.rank == 2))
                                        return tmpCard;
                                    break;
                                case AiType.type3:
                                    //타입3: 공격 받음
                                    return null;
                            }
                        }
                    }
                }
                else if (attackList.Count <= 0 && guardList.Count >= 1)//1-3)) 방어 카드만 있는 경우
                {
                    //1} 다음 차례의 카드 수 확인
                    foreach (var item in attackList)
                    {
                        switch (item.rank)
                        {
                            case 1:
                                if (item.suit == "S")
                                    cardAttackPoint = 5;
                                else cardAttackPoint = 3;
                                break;
                            case 2:
                                cardAttackPoint = 2;
                                break;
                            case 14:
                                cardAttackPoint = 7;
                                break;
                            case 15:
                                cardAttackPoint = 10;
                                break;
                            default:
                                break;
                        }

                        if (nextPlayer.hand.Count + cardAttackPoint + attack >= fullHand)
                        {
                            tmpList.Add(item);
                        }
                    }
                    //다음 차례의 점수와 나의 점수를 비교
                    if (nextPlayer.score > score && nextPlayer.hand.Count > hand.Count)//1] 다음 차례가 점수가 높고 카드의 개수가 많다
                    {
                        switch (aiType)
                        {
                            case AiType.type1:
                            case AiType.type2:
                            case AiType.type3:
                                //타입3: 공격 받음
                                return null;
                        }
                    }
                    else if (nextPlayer.score > score && nextPlayer.hand.Count < hand.Count)//2] 다음 차례가 점수가 높고 카드의 개수가 적다
                    {
                        switch (aiType)
                        {
                            case AiType.type1:
                                return null;
                            case AiType.type2:
                            case AiType.type3:
                                return guardList[Random.Range(0, guardList.Count)];
                        }
                    }
                    else if (nextPlayer.score == score && nextPlayer.hand.Count > hand.Count)//3] 다음 차례가 점수가 비슷하고 카드의 개수가 많다
                    {
                        switch (aiType)
                        {
                            case AiType.type1:
                                return null;
                            case AiType.type2:
                            case AiType.type3:
                                return guardList[Random.Range(0, guardList.Count)];
                        }
                    }
                    else if (nextPlayer.score == score && nextPlayer.hand.Count < hand.Count)//4] 다음 차례가 점수가 비슷하고 카드의 개수가 적다
                    {
                        switch (aiType)
                        {
                            case AiType.type1:
                                return null;
                            case AiType.type2:
                            case AiType.type3:
                                return guardList[Random.Range(0, guardList.Count)];
                        }
                    }
                    else if (nextPlayer.score < score && nextPlayer.hand.Count > hand.Count)//5] 다음 차례가 점수가 적고 카드의 개수가 많다
                    {
                        switch (aiType)
                        {
                            case AiType.type1:
                                return null;
                            case AiType.type2:
                            case AiType.type3:
                                return guardList[Random.Range(0, guardList.Count)];
                        }
                    }
                    else if (nextPlayer.score < score && nextPlayer.hand.Count < hand.Count)//6] 다음 차례가 점수가 적고 카드의 개수가 적다
                    {
                        switch (aiType)
                        {
                            case AiType.type1:
                                return null;
                            case AiType.type2:
                            case AiType.type3:
                                return guardList[Random.Range(0, guardList.Count)];
                        }
                    }
                }
            }
        }
        else //2. 공격이 아닌 경우
        {
            switch (aiType)
            {
                case AiType.type1:
                    foreach (Card tmp in lCD)
                    {
                        if (PointOneCardGame.S.ValidCombo(tmp))
                        {
                            comboList.Add(tmp);
                        }
                    }
                    //1) 콤보 가능 여부
                    break;
                case AiType.type2:
                    break;
                case AiType.type3:
                    break;
            }
        }

        return null;
    }

    public void CBCallback(Card tCB)
    {
        if (PointOneCardGame.CURRENT_PLAYER.type != PlayerType.human) PointOneCardGame.S.turnEndButton.gameObject.SetActive(false);
        else PointOneCardGame.S.turnEndButton.gameObject.SetActive(true);
        PointOneCardGame.S.BossCheck();
    }

    public string ReturnSuit()
    {
        int sNum = 0;
        int cNum = 0;
        int hNum = 0;
        int dNum = 0;

        foreach (var card in hand)
        {
            switch (card.suit)
            {
                case "S":
                    sNum += 1;
                    break;
                case "C":
                    cNum += 1;
                    break;
                case "H":
                    hNum += 1;
                    break;
                case "D":
                    dNum += 1;
                    break;
            }
        }
        if (sNum >= cNum && sNum >= hNum && sNum >= dNum)
        {
            return "S";
        }
        if (cNum > sNum && cNum >= hNum && cNum >= dNum)
        {
            return "C";
        }
        if (hNum > sNum && hNum > cNum && hNum >= dNum)
        {
            return "H";
        }
        if (dNum > sNum && dNum > cNum && dNum > hNum)
        {
            return "D";
        }
        return "S";
    }
}
