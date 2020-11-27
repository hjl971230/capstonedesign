using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class Manager : MonoBehaviour
{
    public Text PrintScore;
    public Text PrintScore2;
    public Text PrintScore3;
    public Text PrintScore4;

    AudioSource audioSource;

    public AudioClip audioCardShuffle;
    public AudioClip audioCardError;
    public AudioClip audioCardDraw;
    public AudioClip audioCardPlay;
    public AudioClip audioCardAttak;

    public static float RestartGame_Delay = 3.00f;

    public static Manager Texts;
    public static Manager Sounds;

    void Start()
    {
        SetScore();
        audioSource = GetComponent<AudioSource>();
    }
    void Awake()
    {
        if(Texts == null) { Texts = this; }
        if(Sounds == null) { Sounds = this; }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetScore()
    {
        PrintScore = GameObject.Find("Score").GetComponent<Text>();
        PrintScore2 = GameObject.Find("AI_Score1").GetComponent<Text>();
        PrintScore3 = GameObject.Find("AI_Score2").GetComponent<Text>();
        PrintScore4 = GameObject.Find("AI_Score3").GetComponent<Text>();
    }
    public void f_PrintScore()
    {
        print("Player[0] " + PointOneCardGame.S.players[0].score + " Player[1] " + PointOneCardGame.S.players[1].score + 
            " Player[2] " + PointOneCardGame.S.players[2].score + " Player[3] " + PointOneCardGame.S.players[3].score);
        PrintScore.text = "Player[0] : " + PointOneCardGame.S.players[0].score.ToString();
        PrintScore2.text = "Player[1] : " + PointOneCardGame.S.players[1].score.ToString();
        PrintScore3.text = "Player[2] : " + PointOneCardGame.S.players[2].score.ToString();
        PrintScore4.text = "Player[3] : " + PointOneCardGame.S.players[3].score.ToString();
    }
    public IEnumerator DelayTime(float time)
    {
        yield return new WaitForSeconds(time);
        print(Time.time);
    }
    public IEnumerator DelayTime(string str,float time)
    {
        yield return new WaitForSeconds(time);
        Invoke(str, 0);
    }
    public void RestartGame()
    {
        PointOneCardGame.CURRENT_PLAYER = null;
        PointOneCardGame.S.combo_turn = 0;
        PointOneCardGame.S.combo_stack = 1;
        SceneManager.LoadScene("_PointOneCardGame_Scene_0");
    }

    public void PlayShuffleSound() { audioSource.PlayOneShot(audioCardShuffle); }
    public void PlayCardSound() { audioSource.PlayOneShot(audioCardPlay,0.25f); }
    public void PlayAttackSound() { audioSource.PlayOneShot(audioCardAttak); }
    public void PlayErrorSound() { audioSource.PlayOneShot(audioCardError,0.1f); }
    public void PlayDrawSound(){
        if(!audioSource.isPlaying)
        audioSource.PlayOneShot(audioCardDraw, 0.25f);
    }
    public IEnumerator StartSound(float startTime)
    {
        while (Time.time - startTime < 3)
        {
            yield return new WaitForSeconds(0.01f);
            if (!audioSource.isPlaying)
            { 
                PlayShuffleSound();
            }
        }
    }
}
