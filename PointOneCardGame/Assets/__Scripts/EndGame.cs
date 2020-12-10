using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class EndGame : MonoBehaviour
{
    List<int> Scores = new List<int>();
    List<string> playerNames = new List<string>();
    public List<Text> PrintScore;
    const int AddScore = 300;
    int CurrentAddScore = 0;

    private void Start()
    {
        PrintScore.Add(GameObject.Find("Score1").GetComponent<Text>());
        PrintScore.Add(GameObject.Find("Score2").GetComponent<Text>());
        PrintScore.Add(GameObject.Find("Score3").GetComponent<Text>());
        PrintScore.Add(GameObject.Find("Score4").GetComponent<Text>());
        CaculateScore();
        DrawScore();
    }

    void CaculateScore()
    {
        var ranks = from n in PointOneCardGame.Playerpref orderby n.score descending, n.hand.Count ascending select n;

        foreach (var rankPlayers in ranks)
        {
            Scores.Add(rankPlayers.score);
            playerNames.Add(rankPlayers.PlayerName);
        }

        //for (int i = 0; i < 4; i++)
        //{
        //    CurrentAddScore = AddScore - (100 * i);
        //    Scores[i] += CurrentAddScore;
        //}
    }

    void DrawScore()
    {
        for (int i = 0; i < PrintScore.Count; i++)
        {
            switch (i)
            {
                case 0:
                    PrintScore[i].text = "1st<";
                    break;
                case 1:
                    PrintScore[i].text = "2nd<";
                    break;
                case 2:
                    PrintScore[i].text = "3rd<";
                    break;
                case 3:
                    PrintScore[i].text = "4th<";
                    break;
            }
            PrintScore[i].text += playerNames[i] + ">" + " : " + Scores[i].ToString();
        }
        print("Player[0] " + PrintScore[0].text + " Player[1] " + PrintScore[1].text +
            " Player[2] " + PrintScore[2].text + " Player[3] " + PrintScore[3].text);
    }

    public void ChangeGameScene()
    {
        SceneManager.LoadScene("Title");
    }
}
