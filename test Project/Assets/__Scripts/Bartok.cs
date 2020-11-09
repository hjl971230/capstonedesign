using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum TurnPhase
{
    idle,
    pre,
    waiting,
    post,
    gameOver
}

public class Bartok : MonoBehaviour
{
    static public Bartok S;
    static public Player CURRENT_PLAYER;

    [Header("Set in Inspector")]
    public TextAsset deckXML;
    public TextAsset layoutXML;
    public Vector3 layoutCenter = Vector3.zero;

    public float handFanDegrees = 10f;
    public int numStartingCards = 7;
    public float drawTimeStagger = 0.1f;

    [Header("Set Dynamically")]
    public Deck deck;
    public List<CardBartok> drawPile;
    public List<CardBartok> discardPile;

    private LayoutBartok layout;
    private Transform layoutAnchor;
    public List<Player> players;
    public CardBartok targetCard;
    public TurnPhase phase = TurnPhase.idle;

    public int jack = 0;
    public int queen = 1;
    public int king = 0;
    public static int attack_stack = 0;
    const int STD_SCORE = 10;
    public int combo_stack = 1;
    public int combo_turn = 0;

    private void Awake()
    {
        S = this;
    }

    void Start()
    {
        deck = GetComponent<Deck>();
        deck.InitDeck(deckXML.text);
        Deck.Shuffle(ref deck.cards);

        layout = GetComponent<LayoutBartok>();
        layout.ReadLayout(layoutXML.text);

        drawPile = UpgradeCardList(deck.cards);
        LayoutGame();
    }

    List<CardBartok> UpgradeCardList(List<Card> lCD)
    {
        List<CardBartok> lCB = new List<CardBartok>();
        foreach (var tCD in lCD)
        {
            lCB.Add(tCD as CardBartok);
        }

        return lCB;
    }

    public void ArrangeDrawPile()
    {
        CardBartok tCB;
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
        foreach (SlotDefBartok tSD in layout.slotDefs)
        {
            pl = new Player();
            pl.handSlotdef = tSD;
            players.Add(pl);
            pl.playerNum = players.Count;
        }
        players[0].type = PlayerType.human;

        CardBartok tCB;
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

    public CardBartok MoveToTarget(CardBartok tCB)
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

    public CardBartok MoveToDiscard(CardBartok tCB)
    {
        tCB.state = CBState.discard;
        discardPile.Add(tCB);
        tCB.SetSortingLayerName(layout.discardpile.layerName);
        tCB.SetSortOrder(discardPile.Count * 4);
        tCB.transform.localPosition = layout.discardpile.pos + Vector3.back / 2;
        return tCB;
    }

    public CardBartok Draw(int i = 0)
    {
        CardBartok cd = drawPile[i];
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

        return cd;
    }

    public void DrawFirstTarget()
    {
        CardBartok tCB = MoveToTarget(Draw());

        tCB.reportFinishTo = this.gameObject;
    }

    public void CBCallback(CardBartok cb)
    {
        //Utils.tr("Bartok.CBCallback()", cb.name);
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
            num = ((ndx + 1 * queen) % 4) ;
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
				idx += 4;
				idx %= players.Count;
			}
		}
        print("attack_stack : " + attack_stack);
        CURRENT_PLAYER = players[idx];
        phase = TurnPhase.pre;

        CURRENT_PLAYER.TakeTurn();

        print("Player[0] " + players[0].score + " Player[1] " + players[1].score + " Player[2] " + players[2].score + " Player[3] " + players[3].score);

        //Utils.tr("Bartok.PassTurn()", "Old: " + lastPlayerNum, " New: " + CURRENT_PLAYER.playerNum);
    }

    public bool ValidPlay(CardBartok cb)
    {
        if (cb.rank == targetCard.rank) return true; 
        if (cb.suit == targetCard.suit) return true;

        return false;
    }

    public bool AttackValidPlay(CardBartok cb)
    {
        if (cb.rank == targetCard.rank && cb.rank <= 3) return true;
        if (cb.suit == targetCard.suit && cb.rank <= 3) return true;

        return false;
    }

    public bool ValidCombo(CardBartok cb)
    {
        if (cb.rank == targetCard.rank && cb.suit != targetCard.suit) return true;

        return false;
    }

    public void CardClicked(CardBartok tCB)
    {
        if (CURRENT_PLAYER.type != PlayerType.human) return;

        if (phase == TurnPhase.waiting) return;

        CardBartok cb;
        List<CardBartok> attackvalidCards = new List<CardBartok>();

        foreach (CardBartok tmp in CURRENT_PLAYER.hand)
        {
            if (AttackValidPlay(tmp))
            {
                attackvalidCards.Add(tmp);
            }
        }

        if (attackvalidCards.Count == 0 && attack_stack > 0)
        {
            CardBartok aCB = null;
            for (int i = 0; i < attack_stack; i++)
            {
                aCB = CURRENT_PLAYER.AddCard(Draw());
            }
            CURRENT_PLAYER.score -= STD_SCORE * attack_stack;
            attack_stack = 0;
            if(aCB != null) aCB.callbackPlayer = CURRENT_PLAYER;
            return;
        }

        List<CardBartok> comboList = new List<CardBartok>();

        foreach (CardBartok tmp in CURRENT_PLAYER.hand)
        {
            if (ValidCombo(tmp))
            {
                comboList.Add(tmp);
            }
        }

        if (comboList.Count > 0)
        {
            if (comboList.Count > 1)
            {
                combo_turn += 3 * queen;
            }
            //cb = comboList[Random.Range(0, comboList.Count)];
            combo_stack *= 2;
        }
        else if (comboList.Count <= 0)
        {
            combo_turn = 0;
            combo_stack = 1;
        }

        if (combo_stack <= 1) combo_stack = 1;

        switch (tCB.state)
        {
            case CBState.drawpile:
                cb = CURRENT_PLAYER.AddCard(Draw());
                cb.callbackPlayer = CURRENT_PLAYER;
                Utils.tr("Bartok.CardClicked()", "Draw", cb.name);

                CURRENT_PLAYER.score -= STD_SCORE;
                phase = TurnPhase.waiting;
                break;
            case CBState.hand:
                if (ValidPlay(tCB) && tCB.faceUP)
                {
                    CURRENT_PLAYER.RemoveCard(tCB);
                    MoveToTarget(tCB);
                    switch (tCB.rank)
                    {
                        case 1:
                            if(tCB.suit == "S")
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
                        case 11:
                            jack = 1 * queen;
                            break;
                        case 12:
                            queen *= -1;
                            break;
                        case 13:
                            king = 3 * queen;
                            break;
                        default:
                            break;
                    }
                    tCB.callbackPlayer = CURRENT_PLAYER;
                    Utils.tr("Bartok.CardClicked()", "Play", tCB.name, targetCard.name + " is target");
                    phase = TurnPhase.waiting;
                    CURRENT_PLAYER.score += STD_SCORE * combo_stack;
                    
                }
                else
                {
                    Utils.tr("Bartok.CardClicked()", "Attempted to Play", tCB.name, targetCard.name + " is target");
                }
                break;
        }
    }

    public bool CheckGameOver()
    {
        if (drawPile.Count == 0)
        {
            List<Card> cards = new List<Card>();
            foreach (CardBartok cb in discardPile)
            {
                cards.Add(cb);
            }
            discardPile.Clear();
            Deck.Shuffle(ref cards);
            drawPile = UpgradeCardList(cards);
            ArrangeDrawPile();
        }
        if (CURRENT_PLAYER.hand.Count == 0)
        {
            phase = TurnPhase.gameOver;
            Invoke("RestartGame", 1);
            return true;
        }
        return false;
    }
    public void RestartGame()
    {
        CURRENT_PLAYER = null;
        combo_turn = 0;
        combo_stack = 1;
        SceneManager.LoadScene("__Bartok_Scene_0");
    }

    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Alpha1))
    //    {
    //        players[0].AddCard(Draw());
    //    }
    //    if (Input.GetKeyDown(KeyCode.Alpha2))
    //    {
    //        players[1].AddCard(Draw());
    //    }
    //    if (Input.GetKeyDown(KeyCode.Alpha3))
    //    {
    //        players[2].AddCard(Draw());
    //    }
    //    if (Input.GetKeyDown(KeyCode.Alpha4))
    //    {
    //        players[3].AddCard(Draw());
    //    }
    //}
}
