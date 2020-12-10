using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public enum TurnPhase
{
    idle,
    pre,
    waiting,
    post,
    gameOver
}

public class PointOneCardGame : MonoBehaviour
{
    static public PointOneCardGame S;
    static public Player CURRENT_PLAYER;
    static public List<Player> Playerpref = null;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public Vector3 layoutCenter = Vector3.zero;

    public float handFanDegrees = 10f;
    public int numStartingCards = 3;
    public float drawTimeStagger = 0.1f;

    public Button turnEndButton;

    //7
    public ButtonMenuUI buttonMenu;

    [Header("Set Dynamically")]
    public Deck deck;
    public List<Card> drawPile;
    public List<Card> discardPile;

    private LayoutPointOneCardGame layout;
    private Transform layoutAnchor;
    public List<Player> players;
    public List<Player> endPlayers;
    public Card targetCard;
    public TurnPhase phase = TurnPhase.idle;

    public int jack = 0;
    public int queen = 1;
    public int king = 0;
    public static int attack_stack = 0;
    const int STD_SCORE = 10;
    public int combo_stack = 1;
    public int combo_turn = 0;
    public bool handflag = false;
    public bool sevenflag = false;
    public bool bossflag = false;
    public const int fullHand = 18;
    public int bossTurnCount = 0;
    public int bossTurnActiveCount = 0;
    public int rankcount = 0;
    const int AddScore = 400;
    int CurrentAddScore = 0;

    private void Awake()
    {
        S = this;
    }

    void Start()
    {
        jack = 0;
        queen = 1;
        king = 0;
        attack_stack = 0;
        combo_stack = 1;
        combo_turn = 0;
        handflag = false;
        bossTurnActiveCount = 0;
        CURRENT_PLAYER = null;
        CurrentAddScore = AddScore;

        turnEndButton = GameObject.Find("TurnEndButton").GetComponent<Button>();
        turnEndButton.gameObject.SetActive(false);
        float StartTime = Time.time;
        StartCoroutine(Manager.Sounds.StartSound(StartTime));
        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text);
        Deck.Shuffle(ref deck.cards);
        
        layout = GetComponent<LayoutPointOneCardGame>();
        layout.ReadLayout(layoutXML.text);

        drawPile = deck.cards;
        LayoutGame();
    }

    public void ArrangeDrawPile()
    {
        Card tCB;
        for (int i = 0; i < drawPile.Count; i++)
        {
            tCB = drawPile[i];
            tCB.transform.parent = layoutAnchor;
            tCB.transform.localPosition = layout.drawpile.pos;

            tCB.faceUP = false;
            tCB.SetSortingLayerName(layout.drawpile.layerName);
            tCB.SetSortOrder(-i * 4);
            tCB.state = CBState.drawpile;
        }
    }

    void LayoutGame()
    {
        if (layoutAnchor == null)
        {
            GameObject tGO = new GameObject("_LayoutAnchor");

            layoutAnchor = tGO.transform;
            layoutAnchor.transform.position = layoutCenter;
        }

        ArrangeDrawPile();

        Player pl;
        players = new List<Player>();
        foreach (SlotDefPointOneCardGame tSD in layout.slotDefs)
        {
            pl = new Player();
            pl.handSlotdef = tSD;
            players.Add(pl);
            pl.playerNum = players.Count;
        }
        players[0].type = PlayerType.human;
        players[0].PlayerName = "Player";
        List<AiType> typelist = new List<AiType>();
        typelist.Add(AiType.type1);
        typelist.Add(AiType.type2);
        typelist.Add(AiType.type3);
        for (int i = 1; i < players.Count; i++)
        {
            players[i].PlayerName = "Ai " + i.ToString();
            players[i].aiType = typelist[Random.Range(0, typelist.Count)];
            typelist.Remove(players[i].aiType);
        }

        Card tCB;
        //int num = 0;
        //for (int i = 0; i < 7; i++)
        //{
        //    switch (i)
        //    {
        //        case 0:
        //            num = 5;
        //            break;
        //        case 1:
        //            num = 6;
        //            break;
        //        case 2:
        //            num = 12;
        //            break;
        //        case 3:
        //            num = 13;
        //            break;
        //        case 4:
        //            num = 19;
        //            break;
        //        case 5:
        //            num = 20;
        //            break;
        //        case 6:
        //            num = 26;
        //            break;
        //        default:
        //            break;
        //    }
        //    num -= i;
        //    tCB = Draw(num);
        //    tCB.timeStart = Time.time + drawTimeStagger * (i * 4 + 3);
        //    players[0].AddCard(tCB);
        //}

        for (int i = 0; i < numStartingCards; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                tCB = Draw();

                tCB.timeStart = Time.time + drawTimeStagger * (i * 4 + j);

                players[(j + 1) % 4].AddCard(tCB);
            }
        }
        Invoke("DrawFirstTarget", drawTimeStagger * (numStartingCards * 4 + 4));
    }

    public Card MoveToTarget(Card tCB)
    {
        tCB.timeStart = 0;
        tCB.MoveTo(layout.discardpile.pos + Vector3.back);
        tCB.state = CBState.toTarget;
        tCB.faceUP = true;
        tCB.SetSortingLayerName("10");
        tCB.eventualSortLayer = layout.target.layerName;
        if (targetCard != null)
        {
            MoveToDiscard(targetCard);
            if (deck.formerCard != null)
            {
                deck.ReturnCard(deck.formerCard);
                deck.formerCard = null;
            }
        }
        targetCard = tCB;
        return tCB;
    }

    public Card MoveToDiscard(Card tCB)
    {
        tCB.state = CBState.discard;
        discardPile.Add(tCB);
        tCB.SetSortingLayerName(layout.discardpile.layerName);
        tCB.SetSortOrder(discardPile.Count * 4);
        tCB.transform.localPosition = layout.discardpile.pos + Vector3.back / 2;
        return tCB;
    }

    public Card Draw(int i = 0)
    {
        Card cd = drawPile[i];
        drawPile.RemoveAt(i);

        if (drawPile.Count == 0)
        {
            int ndx;
            while (discardPile.Count > 0)
            {
                ndx = Random.Range(0, discardPile.Count);
                drawPile.Add(discardPile[ndx]);
                discardPile.RemoveAt(ndx);
            }

            ArrangeDrawPile();

            float t = Time.time;
            foreach (var tCB in drawPile)
            {
                tCB.transform.localPosition = layout.discardpile.pos;
                tCB.callbackPlayer = null;
                tCB.MoveTo(layout.drawpile.pos);
                tCB.timeStart = t;
                t += 0.02f;
                tCB.state = CBState.toDrawPile;
                tCB.eventualSortLayer = "1";
            }
        }
        if (Time.time > 2)
        {
            Manager.Sounds.PlayDrawSound();
        }
        return cd;
    }

    public void DrawFirstTarget()
    {
        Card tCB = MoveToTarget(Draw());

        tCB.reportFinishTo = this.gameObject;
        //Invoke("StartGame", 1f);
    }

    public void CBCallback(Card cb)
    {
        //Utils.tr("PointOneCardGame.CBCallback()", cb.name);
        StartGame();
    }

    public void StartGame()
    {
        bossTurnActiveCount = Random.Range(24, 28);
        PassTurn(1);
    }

    public void BossCheck()
    {
        int rnum = 0;
        if (bossTurnActiveCount <= bossTurnCount) bossflag = true;
        if ((combo_stack == 1 || king == 0) && !bossflag && CURRENT_PLAYER.type != PlayerType.gameend) bossTurnCount++;

        if (bossflag)
        {
            rnum = Random.Range(0, 2);
            switch (rnum)
            {
                case 0:
                    //pattern 1
                    Danger.S.Danger1.SetActive(true);
                    StartCoroutine(PassTurnWaitMIn());
                    CardChangePattern();
                    break;
                case 1:
                    //pattern 2
                    Danger.S.Danger2.SetActive(true);
                    StartCoroutine(PassTurnWaitMIn());
                    ScoreAttackPattern();
                    break;
                default:
                    break;
            }
            bossTurnActiveCount = Random.Range(24, 28);
            bossTurnCount = 0;
            bossflag = false;
        }
        else PassTurn();
    }

    public void PassTurn(int num = -1)
    {
        if (combo_stack <= 1) handflag = false;

        if (num == -1)
        {
            int ndx = players.IndexOf(CURRENT_PLAYER);
            num = ((ndx + 1 * queen) % players.Count);
        }
        int lastPlayerNum = -1;
        if (CURRENT_PLAYER != null)
        {
            lastPlayerNum = CURRENT_PLAYER.playerNum;
            if (CheckGameOver())
            {
                return;
            }
        }
        int idx = (num + jack + king) + combo_turn;
        if (idx >= players.Count || idx < 0)
        {
            idx %= players.Count;
            if (idx < 0)
            {
                idx += players.Count;
                idx %= players.Count;
            }
        }
        CURRENT_PLAYER = players[idx];
        phase = TurnPhase.pre;

        if (players[idx].type != PlayerType.human) turnEndButton.gameObject.SetActive(false);
        else turnEndButton.gameObject.SetActive(true);
        CURRENT_PLAYER.TakeTurn();
        Manager.Texts.f_PrintScore();
        if (CURRENT_PLAYER.type == PlayerType.gameend) BossCheck();
    }

    IEnumerator PassTurnWaitMIn()
    {
        yield return new WaitForSeconds(3.0f);
        Danger.S.Danger1.SetActive(false);
        Danger.S.Danger2.SetActive(false);
        PassTurn();
    }

    public bool ValidPlay(Card cb)
    {
        if (cb.rank == targetCard.rank) return true;
        if (cb.suit == targetCard.suit) return true;
        if (cb.suit == "J" && cb.rank >= 14) return true;
        if (targetCard.suit == "J" && attack_stack == 0) return true;
        if (targetCard.suit == "J" && targetCard.rank == 14 && cb.suit == "S" && cb.rank == 1) return true;

        return false;
    }

    public bool AttackValidPlay(Card cb)
    {
        if (cb.suit == "J" && cb.rank >= 14) return true;
        if (cb.rank == targetCard.rank && (cb.rank <= 3 || cb.rank >= 14)) return true;
        if (cb.suit == targetCard.suit && (cb.rank <= 3 || cb.rank >= 14)) return true;
        if (targetCard.suit == "J" && targetCard.rank == 14 && cb.suit == "S" && cb.rank == 1) return true;

        return false;
    }

    public bool ValidCombo(Card cb)
    {
        if (cb.rank != 13 && cb.rank == targetCard.rank) return true;

        return false;
    }

    public void CardClicked(Card tCB)
    {
        if (CURRENT_PLAYER.type != PlayerType.human) return;

        if (phase == TurnPhase.waiting) return;

        Card cb;
        List<Card> attackvalidCards = new List<Card>();

        foreach (Card tmp in CURRENT_PLAYER.hand)
        {
            if (AttackValidPlay(tmp))
            {
                attackvalidCards.Add(tmp);
            }
        }

        if (attackvalidCards.Count == 0 && attack_stack > 0)
        {
            Card aCB = null;
            for (int i = 0; i < attack_stack; i++)
            {
                aCB = CURRENT_PLAYER.AddCard(Draw());
            }
            CURRENT_PLAYER.score -= STD_SCORE * attack_stack;
            attack_stack = 0;
            if (aCB != null) aCB.callbackPlayer = CURRENT_PLAYER;
            return;
        }

        List<Card> comboList = new List<Card>();

        List<Player> alonePlayers = new List<Player>();
        foreach (var tmp in players)
        {
            if (checkAlone(tmp))
            {
                alonePlayers.Add(tmp);
            }
        }

        switch (tCB.state)
        {
            case CBState.drawpile:
                if (attack_stack > 0 && tCB.rank > 3)
                {
                    return;
                }
                cb = CURRENT_PLAYER.AddCard(Draw());
                handflag = true;
                //cb.callbackPlayer = CURRENT_PLAYER;
                CURRENT_PLAYER.score -= STD_SCORE;
                phase = TurnPhase.waiting;
                break;
            case CBState.hand:
                if (ValidPlay(tCB) && tCB.faceUP)
                {
                    if (attack_stack > 0 && tCB.rank > 3 && tCB.rank < 14 
                        && (targetCard.callbackPlayer.type == PlayerType.ai || targetCard.callbackPlayer == null))
                    {
                        return;
                    }
                    CURRENT_PLAYER.RemoveCard(tCB);
                    Manager.Sounds.PlayCardSound();
                    MoveToTarget(tCB);
                    switch (tCB.rank)
                    {
                        case 1:
                            if (tCB.suit == "S")
                            {
                                attack_stack += 5;
                            }
                            else attack_stack += 3;
                            break;
                        case 2:
                            attack_stack += 2;
                            break;
                        case 3:
                            if (attack_stack > 0) attack_stack = 0;
                            break;
                        case 7: //7
                            if (combo_turn <= 0)
                            {
                                buttonMenu.ShowMenu(true);
                                sevenflag = true;
                            }
                            break;
                        case 11:
                            if(alonePlayers.Count >= 2) 
                                jack = (players.Count - 1) * queen;
                            else jack = 1 * queen;
                            break;
                        case 12:
                            queen *= -1;
                            break;
                        case 13:
                            king = (players.Count - 1) * queen;
                            combo_stack *= 2;
                            handflag = false;
                            if (combo_turn <= 0)
                            {
                                tCB.callbackPlayer = CURRENT_PLAYER;
                            }
                            break;
                        case 14:
                            attack_stack += 7;
                            break;
                        case 15:
                            attack_stack += 10;
                            break;
                        default:
                            break;
                    }
                    if (tCB.rank != 11) jack = 0;
                    if (tCB.rank != 13) king = 0;
                    if (king == 0) handflag = true;
                    phase = TurnPhase.waiting;
                    CURRENT_PLAYER.score += STD_SCORE * combo_stack;
                    foreach (Card tmp in CURRENT_PLAYER.hand)
                    {
                        if (ValidCombo(tmp))
                        {
                            comboList.Add(tmp);
                        }
                    }
                    if (comboList.Count >= 1)
                    {
                        combo_stack *= 2;
                        if (comboList.Find(tmp => tmp.rank == 11)) jack = 0;
                        //if(comboList.Find(tmp => tmp.rank == 12))queen = 1;
                        combo_turn = (players.Count - 1) * queen;
                        tCB.callbackPlayer = CURRENT_PLAYER; 
                    }
                    else if (comboList.Count <= 0)
                    {
                        combo_turn = 0;
                        combo_stack = 1;
                    }

                    if (combo_stack <= 1) combo_stack = 1;
                }
                else
                {
                    Manager.Sounds.PlayErrorSound();
                    Utils.tr("PointOneCardGame.CardClicked()", "Attempted to Play", tCB.name, targetCard.name + " is target");
                }
                break;
        }

    }

    public void CardChangePattern()
    {
        int rNum = 0;
        List<string> cSprite = new List<string> { "S", "C", "H", "D" };

        if(targetCard.suit != "J")
        {
            cSprite.Remove(targetCard.suit);
            rNum = Random.Range(0, cSprite.Count);

            switch (rNum)
            {
                case 0:
                case 1:
                case 2:
                    deck.ChangeCard(targetCard, cSprite[rNum]);
                    break;
            }
        }
    }

    public void ScoreAttackPattern()
    {
        foreach (var item in players)
        {
            if(item.hand.Count >= 7)
            {
                item.score -= (item.hand.Count - 6) * 10;
            }
        }
    }

    public bool checkHandFull()
    {
        foreach (var item in players)
        {
            if (item.hand.Count >= fullHand)
            {
                return true;
            }
        }
        return false;
    }

    public bool checkAlone(Player player)
    {
        if (player.type == PlayerType.gameend) return true;
        return false;
    }

    public bool CheckGameOver()
    {
        List<Player> alonePlayers = new List<Player>();
        foreach (var tmp in players)
        {
            if (checkAlone(tmp))
            {
                alonePlayers.Add(tmp);
            }
        }

        if (drawPile.Count == 0)
        {
            List<Card> cards = new List<Card>();
            foreach (Card cb in discardPile)
            {
                cards.Add(cb);
            }
            discardPile.Clear();
            Deck.Shuffle(ref cards);
            //drawPile = UpgradeCardList(cards);
            drawPile = cards;
            ArrangeDrawPile();
        }
        if (CURRENT_PLAYER.hand.Count == 0 || checkHandFull())
        {
            if (CURRENT_PLAYER.hand.Count == 0 && CURRENT_PLAYER.type != PlayerType.gameend)
            {
                //endPlayers.Add(CURRENT_PLAYER);
                //players.Remove(CURRENT_PLAYER);
                CURRENT_PLAYER.type = PlayerType.gameend;
                CURRENT_PLAYER.rank = ++rankcount;
                CurrentAddScore -= 100;
                CURRENT_PLAYER.score += CurrentAddScore;
            }
            if (alonePlayers.Count >= 3 || checkHandFull())
            {
                phase = TurnPhase.gameOver;
                //StartCoroutine(Manager.Texts.DelayTime("RestartGame", Manager.RestartGame_Delay));
                var ranks = from n in players orderby n.score descending, n.hand.Count ascending select n;
                players = ranks.ToList();
                for (int i = 0; i < players.Count; i++)
                {
                    if(players[i].type != PlayerType.gameend)
                    {
                        if(CurrentAddScore > 0) CurrentAddScore -= 100;
                        players[i].score += CurrentAddScore;
                    }
                }
                Invoke("RestartGame", 1);
                return true;
            }
        }
        return false;
    }

    public void RestartGame()
    {
        //CURRENT_PLAYER = null;
        //combo_turn = 0;
        //combo_stack = 1;
        Playerpref = players;
        SceneManager.LoadScene("EndGame");
    }

    public void TurnEnd()
    {
        Card cb;
        if (CURRENT_PLAYER.hand.Count == 0) handflag = true;

        if (handflag) 
        {
            if (sevenflag)
            {
                if (combo_stack <= 1) handflag = false;
                return;
            }
            else 
            { 
                if (combo_turn >= 1)
                {
                    combo_turn = 0;
                    combo_stack = 1;
                }
                if (combo_stack <= 1) handflag = false;
                CURRENT_PLAYER.CBCallback(null); 
            }
        }
        else
        {
            if (attack_stack > 0)
            {
                handflag = false;
                Card aCB = null;
                for (int i = 0; i < attack_stack; i++)
                {
                    aCB = CURRENT_PLAYER.AddCard(Draw());
                }
                CURRENT_PLAYER.score -= STD_SCORE * attack_stack;
                attack_stack = 0;
                if (combo_turn >= 1)
                {
                    combo_turn = 0;
                    combo_stack = 1;
                }
                if (aCB != null) aCB.callbackPlayer = CURRENT_PLAYER;
            }
            else
            {
                handflag = false;
                cb = CURRENT_PLAYER.AddCard(Draw());
                CURRENT_PLAYER.score -= STD_SCORE;
                phase = TurnPhase.waiting;
                if (combo_turn >= 1)
                {
                    combo_turn = 0;
                    combo_stack = 1;
                }
                cb.callbackPlayer = CURRENT_PLAYER;
            }
        }
        
    }
}