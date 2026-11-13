using WobbleBrawl.Flow;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    public GameObject Crate;
    public GameObject SpawnEffect;

    [SerializeField] private PlayerBehaviour playerBehaviour;
    [SerializeField] private EnemyBehaviour enemyBehaviour;

    public Image YouWin;
    public Image YouLose;
    public Button restart;

    //count down time to spawn the throwable
    [SerializeField, Min(0f)] private float crateSpawnDelay = 31f;
    private float crateSpawnTime;
    private bool crateSpawned;

    private float roundEndTime;
    private int shownSeconds = -1;
    private bool timesUp;
    private bool suddenDeath;
    private float shownGuard = -1f;
    private Text guardLabel;
    [SerializeField] private Text TimerCountDown;

    // Start is called before the first frame update
    void Start()
    {

        roundEndTime = Time.time + RoundRules.RoundSeconds;
        crateSpawnTime = Time.time + crateSpawnDelay;
        YouWin.enabled = false;
        YouLose.enabled = false;
        restart.gameObject.SetActive(false);

        guardLabel = Instantiate(TimerCountDown, TimerCountDown.transform.parent);
        guardLabel.name = "Guard";
        guardLabel.rectTransform.anchoredPosition += Vector2.down * TimerCountDown.rectTransform.rect.height;
        guardLabel.raycastTarget = false;
        playerBehaviour.KnockedOut += OnKnockout;
        enemyBehaviour.KnockedOut += OnKnockout;
    }

    // Update is called once per frame
    void Update()
    {
        float guard = Mathf.Ceil(playerBehaviour.Guard);
        if (guard != shownGuard)
        {
            shownGuard = guard;
            guardLabel.text = $"Guard {guard:0}";
        }
        SpawnThrowable();
        Timer(); 
    }

    //throwable spawner
    private void SpawnThrowable()
    {
        if (!crateSpawned && !timesUp && Time.time >= crateSpawnTime)
        {
            crateSpawned = true;
            var spawnRotation = Quaternion.Euler(-180f, 0f, 0f);
            Instantiate(Crate, new Vector3(0, 0.3f, 0), spawnRotation);
            Instantiate(SpawnEffect, new Vector3(0, 0.3f, 0), spawnRotation);
        }
    } 

    private void OnDestroy()
    {
        if (playerBehaviour != null)
            playerBehaviour.KnockedOut -= OnKnockout;
        if (enemyBehaviour != null)
            enemyBehaviour.KnockedOut -= OnKnockout;
    }

    private void OnKnockout() => EvaluateRound(false);

    private void Timer()
    {
        if (timesUp)
            return;

        int seconds = Mathf.Max(0, Mathf.CeilToInt(roundEndTime - Time.time));
        if (seconds != shownSeconds)
        {
            shownSeconds = seconds;
            TimerCountDown.text = seconds.ToString();
        }
        if (seconds == 0)
            EvaluateRound(true);
    }

    private void EvaluateRound(bool expired)
    {
        if (timesUp)
            return;

        var result = RoundRules.Evaluate(playerBehaviour.MaxHealth, enemyBehaviour.MaxHealth, expired, suddenDeath);
        switch (result)
        {
            case RoundResult.Continue:
                return;
            case RoundResult.SuddenDeath:
                suddenDeath = true;
                roundEndTime = Time.time + RoundRules.SuddenDeathSeconds;
                playerBehaviour.SetSuddenDeathHealth(RoundRules.SuddenDeathHealth);
                enemyBehaviour.SetSuddenDeathHealth(RoundRules.SuddenDeathHealth);
                return;
            case RoundResult.PlayerWins:
                YouWin.enabled = true;
                playerBehaviour.Win();
                enemyBehaviour.EnemyDieAnim();
                break;
            case RoundResult.OpponentWins:
                YouLose.enabled = true;
                enemyBehaviour.Win();
                playerBehaviour.PlayerDieAnim();
                break;
            case RoundResult.Draw:
                TimerCountDown.text = "Draw";
                break;
        }

        timesUp = true;
        playerBehaviour.enabled = false;
        enemyBehaviour.enabled = false;
        restart.gameObject.SetActive(true);
    }
}
