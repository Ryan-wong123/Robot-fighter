using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    public GameObject Crate;
    public GameObject SpawnEffect;

    private GameObject Player;
    private GameObject Enemy;
    private PlayerBehaviour playerBehaviour;
    private EnemyBehaviour enemyBehaviour;

    public Image YouWin;
    public Image YouLose;
    public Button restart;

    //count down time to spawn the throwable
    private float CountDownSpawnTime = 36f;

    //timer for 1 round
    private int timer = 60;
    private bool timesUp = false;
    private float lastTimeUpdate = 0;
    private Text TimerCountDown;

    // Start is called before the first frame update
    void Start()
    {
        TimerCountDown = GameObject.Find("Timer").GetComponent<Text>();
        //YouWin = GameObject.Find("winscreen").GetComponent<Image>();
        //YouLose = GameObject.Find("losescreen").GetComponent<Image>();
        //restart = GameObject.Find("restart").GetComponent<Button>();

        YouWin.enabled = false;
        YouLose.enabled = false;
        restart.gameObject.SetActive(false);

        Player = GameObject.Find("Player");
        Enemy = GameObject.Find("Opponent");
        enemyBehaviour = Enemy.GetComponent<EnemyBehaviour>();
        playerBehaviour = Player.GetComponent<PlayerBehaviour>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        SpawnThrowable();
        Timer(); 
    }

    //throwable spawner
    private void SpawnThrowable()
    {
        CountDownSpawnTime -= Time.deltaTime;
        if (CountDownSpawnTime < 5f && CountDownSpawnTime > 4.99f)
        {
            transform.rotation = Quaternion.Euler(-180f, 0f, 0f);
            Instantiate(Crate, new Vector3(0, 0.3f, 0), transform.rotation);
            Instantiate(SpawnEffect, new Vector3(0, 0.3f, 0), transform.rotation);
        }
    } 

    //timer count down for each round
    private void Timer()
    {
        if(timer > 0 && Time.time - lastTimeUpdate > 1)
        {
            timer--;
            lastTimeUpdate = Time.time;
            TimerCountDown.text = timer.ToString();

            if(playerBehaviour.MaxHealth == 0)
            {
                YouLose.enabled = true;
                timesUp = true;
                playerBehaviour.Die();
                enemyBehaviour.Win();
                //disable the script
                playerBehaviour.enabled = false;
                enemyBehaviour.enabled = false;
                restart.gameObject.SetActive(true);
                timer = 0;

            }
            else if (enemyBehaviour.isEnemyDead)
            {
                YouWin.enabled = true;
                timesUp = true;

                playerBehaviour.Win();
                //disable the script
                playerBehaviour.enabled = false;
                enemyBehaviour.enabled = false;
                restart.gameObject.SetActive(true);
                timer = 0;

            }
        }

        else if(timer == 0)
        {
            timer = 0;

            if(timesUp == false)
            {
                if (playerBehaviour.MaxHealth > enemyBehaviour.MaxHealth)
                {
                    YouWin.enabled = true;
                    timesUp = true;

                    playerBehaviour.Win();
                    enemyBehaviour.EnemyDieAnim();
                    //disable the script
                    playerBehaviour.enabled = false;
                    enemyBehaviour.enabled = false;
                    restart.gameObject.SetActive(true);

                }

                else if (playerBehaviour.MaxHealth < enemyBehaviour.MaxHealth)
                {
                    YouLose.enabled = true;
                    timesUp = true;

                    enemyBehaviour.Win();
                    playerBehaviour.PlayerDieAnim();
                    //disable the script
                    playerBehaviour.enabled = false;
                    enemyBehaviour.enabled = false;
                    restart.gameObject.SetActive(true);

                }
            } 
        }
    }  
}
