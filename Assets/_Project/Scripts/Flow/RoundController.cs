using System;
using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Gameplay;
using WobbleBrawl.Core;

namespace WobbleBrawl.Flow
{
    public sealed class RoundController : MonoBehaviour
    {
        [SerializeField] private Fighter player;
        [SerializeField] private Fighter opponent;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private Transform opponentSpawn;
        [SerializeField] private bool trainingMode;
        private bool trainingEnabled;
        public bool IsTraining => trainingEnabled;
        private void Awake() => trainingEnabled = trainingMode && TrainingSandboxRules.AllowsShortcuts(gameObject.scene.name);

        public event Action<RoundResult> Ended;
        public event Action SuddenDeathStarted;
        public event Action<int> TimeChanged;
        public Fighter Player => player;
        public Fighter Opponent => opponent;
        public bool Running { get; private set; }
        public int SecondsRemaining => IsTraining ? RoundRules.RoundSeconds : Mathf.Max(0, Mathf.CeilToInt(deadline - Time.time));

        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private HazardController hazard;

        private int roundIndex;
        private float deadline;
        private bool suddenDeath;
        private int displayedSeconds = -1;
        private MatchRules rules = MatchRules.Default;

        internal void ConfigureRules(MatchRules matchRules) => rules = matchRules;

        internal void ConfigurePlayer(CharacterDef fighter) => player.ConfigureCharacter(fighter);
        internal void ConfigureOpponent(CharacterDef fighter) => opponent.ConfigureCharacter(fighter);

        public void Begin()
        {
            if (Running)
                throw new InvalidOperationException("The round is already running.");

            player.SetItemOpponent(opponent);
            opponent.SetItemOpponent(player);
            player.ResetRound(playerSpawn.position, 1);
            opponent.ResetRound(opponentSpawn.position, -1);
            player.KnockedOut += OnKnockout;
            opponent.KnockedOut += OnKnockout;
            suddenDeath = false;
            deadline = Time.time + RoundRules.RoundSeconds;
            Running = true;
            PublishTime();
            if (opponent.TryGetComponent<AIInput>(out var ai))
                ai.ConfigureRound(itemSpawner, hazard, roundIndex);
            if (itemSpawner != null)
                itemSpawner.BeginRound(roundIndex, rules);
            hazard?.BeginRound(roundIndex, rules);
            roundIndex++;
        }

        private void Update()
        {
            if (!Running)
                return;
            PublishTime();
            if (!IsTraining && Time.time >= deadline)
                Resolve(true);
        }

        private void OnKnockout(Fighter fighter) => Resolve(false);

        private void Resolve(bool expired)
        {
            var result = RoundRules.Evaluate(player.Health, opponent.Health, expired, suddenDeath);
            if (result == RoundResult.Continue)
                return;
            if (result == RoundResult.SuddenDeath)
            {
                suddenDeath = true;
                player.SetSuddenDeath();
                opponent.SetSuddenDeath();
                hazard?.BeginSuddenDeath();
                deadline = Time.time + RoundRules.SuddenDeathSeconds;
                PublishTime();
                SuddenDeathStarted?.Invoke();
                return;
            }

            Stop();
            if (result == RoundResult.PlayerWins)
                player.CelebrateVictory();
            else if (result == RoundResult.OpponentWins)
                opponent.CelebrateVictory();
            Ended?.Invoke(result);
        }

        private void PublishTime()
        {
            int seconds = SecondsRemaining;
            if (seconds == displayedSeconds)
                return;
            displayedSeconds = seconds;
            TimeChanged?.Invoke(seconds);
        }

        private void Stop()
        {
            if (!Running)
                return;

            Running = false;
            itemSpawner?.EndRound();
            hazard?.EndRound();
            player.KnockedOut -= OnKnockout;
            opponent.KnockedOut -= OnKnockout;
            player.StopRound();
            opponent.StopRound();
        }

        private void OnDisable() => Stop();
    }
}
