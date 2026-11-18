using System;
using UnityEngine;
using WobbleBrawl.Data;
using WobbleBrawl.Gameplay;

namespace WobbleBrawl.Flow
{
    public sealed class RoundController : MonoBehaviour
    {
        [SerializeField] private Fighter player;
        [SerializeField] private Fighter opponent;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private Transform opponentSpawn;

        public event Action<RoundResult> Ended;
        public event Action SuddenDeathStarted;
        public bool Running { get; private set; }
        public int SecondsRemaining => Mathf.Max(0, Mathf.CeilToInt(deadline - Time.time));

        [SerializeField] private ItemSpawner itemSpawner;
        [SerializeField] private HazardController hazard;

        private int roundIndex;
        private float deadline;
        private bool suddenDeath;

        internal void ConfigurePlayer(CharacterDef fighter) => player.ConfigureCharacter(fighter);

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
            if (opponent.TryGetComponent<AIInput>(out var ai))
                ai.ConfigureRound(itemSpawner, hazard, roundIndex);
            if (itemSpawner != null)
                itemSpawner.BeginRound(roundIndex);
            hazard?.BeginRound(roundIndex++);
        }

        private void Update()
        {
            if (Running && Time.time >= deadline)
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
                SuddenDeathStarted?.Invoke();
                return;
            }

            Stop();
            Ended?.Invoke(result);
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
