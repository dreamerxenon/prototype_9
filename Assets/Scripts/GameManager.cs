using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.ComponentModel;
using System.Net.Http.Headers;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;


    public GameObject victoryPanel;
    public GameObject losePanel;

    public int moves;
    public int goal;
    public int points;

    public bool isGameEnded;

    public TMP_Text pointTxt;
    public TMP_Text goalTxt;
    public TMP_Text movesTxt, coinTxt, heartTxt, heartRefillTxt, boosterInfoTxt;

    public int heartScore = 5; 
    public int coinScore = 0;
    public int timeToRefill = 59;
    public Button loseButton;
    

    private void Awake()
    {
        Instance = this;
        heartTxt.text = PlayerPrefs.GetInt("HeartScore").ToString();
        coinTxt.text = PlayerPrefs.GetInt("CoinScore").ToString();
        
    }

    public void Initialize(int _moves, int _goal)
    {
        moves = _moves;
        goal = _goal;

    }
    // Start is called before the first frame update
    void Start()
    {
       
        pointTxt.text = "Points:" + points.ToString();
        goalTxt.text = "goal:" + goal.ToString();
        movesTxt.text = "moves:" + moves.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        pointTxt.text = "Points:" + points.ToString();
        goalTxt.text = "goal:" + goal.ToString();
        movesTxt.text = "moves:" + moves.ToString();
    }

    public void ProcessTurn(int _pointsToGain, bool _subtractMoves)
    {
        points += _pointsToGain;


        if (_subtractMoves)
            moves--;

        if (points >= goal)
        {
            isGameEnded = true;
            PotionBoard.Instance.potionParent.SetActive(false);
            victoryPanel.SetActive(true);
            return;

        }
        if (moves == 0)
        {
           
            isGameEnded = true;
            PotionBoard.Instance.potionParent.SetActive(false);
            losePanel.SetActive(true);
            return;
        }
    }


    public void WinGame()
    {
        coinScore = PlayerPrefs.GetInt("CoinScore");
        coinScore += 2;
        PlayerPrefs.SetInt("CoinScore", coinScore);
        coinTxt.text = PlayerPrefs.GetInt("CoinScore").ToString();
        Debug.Log("you won");
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1);
    }

    public void LoseGame()
    {

        heartScore = PlayerPrefs.GetInt("HeartScore");
        if (heartScore == 0)
        {
            Debug.Log("Cant restart wait for one minute for the hrarts to refill");
            InvokeRepeating("CountDown", 0f, 1f);
            loseButton.interactable = false;
            heartRefillTxt.gameObject.SetActive(true);
        }
        else
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex);
            heartScore--;
            Debug.Log(heartScore);
            PlayerPrefs.SetInt("HeartScore", heartScore);
            heartTxt.text = PlayerPrefs.GetInt("HeartScore").ToString();
        }
    }

    public void CountDown()
    {
        heartRefillTxt.text = "Hearts not enough  " + timeToRefill;
        timeToRefill--;
        

        if (timeToRefill == 0)
        {
            
            heartScore = PlayerPrefs.GetInt("HeartScore");
            heartScore += 5;
            PlayerPrefs.SetInt("HeartScore", heartScore);
            heartTxt.text = PlayerPrefs.GetInt("HeartScore").ToString();
            timeToRefill = 59;
            heartRefillTxt.gameObject.SetActive(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            loseButton.interactable = true;
        }
    }

    public void PurchaseMoves()
    {
        if(PlayerPrefs.GetInt("CoinScore") >= 10)
        {
            moves += 5;
            movesTxt.text = "moves:" + moves.ToString();
            int x = PlayerPrefs.GetInt("CoinScore") - 10;
            PlayerPrefs.SetInt("CoinScore", x);
            coinTxt.text = PlayerPrefs.GetInt("CoinScore").ToString();
        }
        else
        {
            boosterInfoTxt.text = "Not Enough coins";
            boosterInfoTxt.gameObject.SetActive(true);
            Invoke("DisableInfoText", 1f);
        }
    }

    public void  purchaseBooster()
    {
        if (PlayerPrefs.GetInt("CoinScore") >= 1)
        {
            boosterInfoTxt.text = "matching 3 booster gets you 15 points";
            boosterInfoTxt.gameObject.SetActive(true);
            PotionBoard.Instance.InitializeBoard(true);
            Invoke("DisableInfoText", 2f);
            int x = PlayerPrefs.GetInt("CoinScore") - 1;
            PlayerPrefs.SetInt("CoinScore", x);
            coinTxt.text = PlayerPrefs.GetInt("CoinScore").ToString();
        }else
        {
            boosterInfoTxt.text = "Not Enough coins";
            boosterInfoTxt.gameObject.SetActive(true);
            Invoke("DisableInfoText", 1f);
        }
        

    }

    private void DisableInfoText()
    {
        boosterInfoTxt.gameObject.SetActive(false);
    }

}