using System;
using UnityEngine;
using WobbleBrawl.Data;

namespace WobbleBrawl.Flow
{
    public sealed class MatchController : MonoBehaviour
    {
        private const int WinsRequired = 2;
        private const int MaximumRounds = 3;

        [SerializeField] private RoundController round;
        [SerializeField, Min(0f)] private float betweenRoundsSeconds = 2f;

        public event Action<int> RoundStarted;
        public event Action<RoundResult> RoundEnded;
        public event Action<RoundResult> MatchEnded;

        public int PlayerWins { get; private set; }
        public int OpponentWins { get; private set; }
        public int RoundNumber { get; private set; }
        private float nextRoundAt = float.PositiveInfinity;

        internal void ConfigurePlayer(CharacterDef fighter) => round.ConfigurePlayer(fighter);

        private void OnEnable() => round.Ended += OnRoundEnded;
        private void OnDisable()
        {
            round.Ended -= OnRoundEnded;
            nextRoundAt = float.PositiveInfinity;
        }

        private void Start()
        {
            AppStateMachine.AttachMatch(this);
            BeginRound();
        }

        private void Update()
        {
            if (Time.unscaledTime >= nextRoundAt)
                BeginRound();
        }

        private void BeginRound()
        {
            nextRoundAt = float.PositiveInfinity;
            round.Begin();
            RoundNumber++;
            RoundStarted?.Invoke(RoundNumber);
        }

        private void OnRoundEnded(RoundResult result)
        {
            if (result == RoundResult.PlayerWins)
                PlayerWins++;
            else if (result == RoundResult.OpponentWins)
                OpponentWins++;

            RoundEnded?.Invoke(result);
            if (PlayerWins >= WinsRequired || OpponentWins >= WinsRequired || RoundNumber >= MaximumRounds)
            {
                MatchEnded?.Invoke(PlayerWins == OpponentWins ? RoundResult.Draw :
                    PlayerWins > OpponentWins ? RoundResult.PlayerWins : RoundResult.OpponentWins);
                return;
            }
            nextRoundAt = Time.unscaledTime + betweenRoundsSeconds;
        }
    }
}
