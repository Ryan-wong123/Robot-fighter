using WobbleBrawl.Core;
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
        [SerializeField] private MatchMode mode;
        [SerializeField] private bool isBossStage;

        public event Action<int> RoundStarted;
        public event Action<RoundResult> RoundEnded;
        public event Action<RoundResult> MatchEnded;

        public int PlayerWins { get; private set; }
        public int OpponentWins { get; private set; }
        public int RoundNumber { get; private set; }
        public bool IsRoundRunning => round.Running;
        private float nextRoundAt = float.PositiveInfinity;
        public MatchRules Rules { get; private set; } = MatchRules.Default;

        internal void ConfigurePlayer(CharacterDef fighter) => round.ConfigurePlayer(fighter);
        internal void ConfigureOpponent(CharacterDef fighter) => round.ConfigureOpponent(fighter);

        /// <summary>Apply setup before round one; rules stay fixed for the match.</summary>
        public void ConfigureRules(MatchRules rules)
        {
            if (RoundNumber != 0)
                throw new InvalidOperationException("Match rules cannot change after round one starts.");
            Rules = rules.Resolve(mode, isBossStage);
            round.ConfigureRules(Rules);
        }

        private void OnEnable() => round.Ended += OnRoundEnded;
        private void OnDisable()
        {
            round.Ended -= OnRoundEnded;
            nextRoundAt = float.PositiveInfinity;
        }

        private void Start()
        {
            ConfigureRules(MatchRules.Default);
            AppStateMachine.AttachMatch(this);
            BeginRound();
        }

        private void Update()
        {
            if (!MatchPause.IsPaused && Time.unscaledTime >= nextRoundAt)
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
            if (round.IsTraining)
            {
                RoundEnded?.Invoke(result);
                nextRoundAt = Time.unscaledTime + betweenRoundsSeconds;
                return;
            }
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
