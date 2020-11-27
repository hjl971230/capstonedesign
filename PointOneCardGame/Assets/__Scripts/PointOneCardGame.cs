using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public Vector3 layoutCenter = Vector3.zero;

    public float handFanDegrees = 10f;
    public int numStartingCards = 3;
    public float drawTimeStagger = 0.1f;

    public Button turnEndButton;

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
    const int fullHand = 18;

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

        turnEndButton = GameObject.Find("TurnEndButton").GetComponent<Button>();
        turnEndButton.gameObject.SetActive(false);
        float StartTime = Time.time;
        StartCoroutine(Manager.Sounds.StartSound(StartTime));
        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text);
        Deck.Shuffle(ref deck.cards);

        layout = GetComponent<LayoutPointOneCardGame>();
        layout.ReadLayout(layoutXML.text);

        //drawPile = UpgradeCardList(deck.cards);
        drawPile = deck.cards;
        LayoutGame();
    }

    //List<Card> UpgradeCardList(List<Card> lCD)
    //{
    //    List<Card> lCB = new List<Card>();
    //    foreach (var tCD in lCD)
    //    {
    //        lCB.Add(tCD as Card);
    //    }

    //    return lCB;
    //}

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
        PassTurn(1);
    }

    public void PassTurn(int num = -1)
    {
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
        print("idx : " + idx);
        CURRENT_PLAYER = players[idx];
        phase = TurnPhase.pre;

        if (players[idx].type != PlayerType.human) turnEndButton.gameObject.SetActive(false);
        else turnEndButton.gameObject.SetActive(true);
        CURRENT_PLAYER.TakeTurn();
        Manager.Texts.f_PrintScore();
        if (CURRENT_PLAYER.type == PlayerType.gameend) PassTurn();
        //print("Player[0] " + players[0].score + " Player[1] " + players[1].score + " Player[2] " + players[2].score + " Player[3] " + players[3].score);

        //Utils.tr("PointOneCardGame.PassTurn()", "Old: " + lastPlayerNum, " New: " + CURRENT_PLAYER.playerNum);
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
        if (cb.rank == targetCard.rank && (cb.rank <= 3 || cb.rank >= 14)) return true;
        if (cb.suit == targetCard.suit && (cb.rank <= 3 || cb.rank >= 14)) return true;
        if (cb.suit == "J" && cb.rank >= 14) return true;
        if (targetCard.suit == "J" && targetCard.rank == 14 && cb.suit == "S" && cb.rank == 1) return true;

        return false;
    }

    public bool ValidCombo(Card cb)
    {
        if (cb.rank != 13 && cb.rank == targetCard.rank && cb.suit != targetCard.suit) return true;

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

        //foreach (Card tmp in CURRENT_PLAYER.hand)
        //{
        //    if (ValidCombo(tmp))
        //    {
        //        comboList.Add(tmp);
        //    }
        //}

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
                Utils.tr("PointOneCardGame.CardClicked()", "Draw", cb.name);
                CURRENT_PLAYER.score -= STD_SCORE;
                phase = TurnPhase.waiting;
                break;
            case CBState.hand:
                if (ValidPlay(tCB) && tCB.faceUP)
                {
                    if (attack_stack > 0 && tCB.rank > 3)
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
                        case 7:
                            break;
                        case 11:
                            jack = 1 * queen;
                            break;
                        case 12:
                            queen *= -1;
                            break;
                        case 13:
                            king = (players.Count - 1) * queen;
                            combo_stack *= 2;
                            if (combo_turn <= 0)
                            {
                                handflag = false;
                                tCB.callbackPlayer = CURRENT_PLAYER;
                            }
                            //PassTurn();
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
                    //tCB.callbackPlayer = CURRENT_PLAYER;
                    handflag = true;
                    //Utils.tr("PointOneCardGame.CardClicked()", "Play", tCB.name, targetCard.name + " is target");
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
                        if(comboList.Find(tmp => tmp.rank == 11))jack = 0;
                        //if(comboList.Find(tmp => tmp.rank == 12))queen = 1;
                        //if (comboList.Count >= 2)
                        {
                            combo_turn = (players.Count - 1) * queen;
                            tCB.callbackPlayer = CURRENT_PLAYER;
                            //CURRENT_PLAYER.CBCallback(null);
                        }
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

    public bool checkHandFull()
    {
        foreach (var item in players)
        {
            if(item.hand.Count >= fullHand)
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
            if(checkAlone(tmp))
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
            if (CURRENT_PLAYER.hand.Count == 0)
			{
                //endPlayers.Add(CURRENT_PLAYER);
                //players.Remove(CURRENT_PLAYER);
                CURRENT_PLAYER.type = PlayerType.gameend;
            }
            if(alonePlayers.Count >= 3 || checkHandFull())
			{
                phase = TurnPhase.gameOver;
                StartCoroutine(Manager.Texts.DelayTime("RestartGame", Manager.RestartGame_Delay));
                //Invoke("RestartGame", 1);
                return true;
            }
        }
        return false;
    }

    public void RestartGame()
    {
        CURRENT_PLAYER = null;
        combo_turn = 0;
        combo_stack = 1;
        SceneManager.LoadScene("_PointOneCardGame_Scene_0");
    }

    public void TurnEnd()
    {
        Card cb;
        List<Card> attackvalidCards = new List<Card>();

        if (handflag) CURRENT_PLAYER.CBCallback(null);
        else
        {

            if (attack_stack > 0)
            {
                Card aCB = null;
                for (int i = 0; i < attack_stack; i++)
                {
                    aCB = CURRENT_PLAYER.AddCard(Draw());
                }
                CURRENT_PLAYER.score -= STD_SCORE * attack_stack;
                attack_stack = 0;
                if (aCB != null) aCB.callbackPlayer = CURRENT_PLAYER;
            }
            else
            {
                cb = CURRENT_PLAYER.AddCard(Draw());
                CURRENT_PLAYER.score -= STD_SCORE;
                phase = TurnPhase.waiting;
                cb.callbackPlayer = CURRENT_PLAYER;
            }
        }
        handflag = false;
    }
}
