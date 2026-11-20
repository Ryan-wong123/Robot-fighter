using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Flow;
using WobbleBrawl.Gameplay;

namespace WobbleBrawl.UI
{
    public sealed class MatchAudio : MonoBehaviour
    {
        private RoundController round;
        private MatchController match;
        private string playerVoice, opponentVoice;
        private float playerReady, opponentReady;

        private void Awake()
        {
            AudioManager.Ensure(transform);
            round = GetComponent<RoundController>();
            match = GetComponent<MatchController>();
        }

        private void OnEnable()
        {
            round.Player.HitResolved += PlayerHit;
            round.Opponent.HitResolved += OpponentHit;
            round.Player.VoiceRequested += Bark;
            round.Opponent.VoiceRequested += Bark;
            round.SuddenDeathStarted += SuddenDeath;
            round.Opponent.BossPhaseChanged += BossPhase;
            match.RoundStarted += RoundStarted;
            match.MatchEnded += MatchEnded;
        }

        private void OnDisable()
        {
            if (round != null)
            {
                if (round.Player != null)
                {
                    round.Player.HitResolved -= PlayerHit;
                    round.Player.VoiceRequested -= Bark;
                }
                if (round.Opponent != null)
                {
                    round.Opponent.HitResolved -= OpponentHit;
                    round.Opponent.VoiceRequested -= Bark;
                    round.Opponent.BossPhaseChanged -= BossPhase;
                }
                round.SuddenDeathStarted -= SuddenDeath;
            }
            if (match != null)
            {
                match.RoundStarted -= RoundStarted;
                match.MatchEnded -= MatchEnded;
            }
        }

        private void PlayerHit(HitInfo hit, DamageResult result) => Hit(round.Player, hit, result);
        private void OpponentHit(HitInfo hit, DamageResult result) => Hit(round.Opponent, hit, result);

        private void Bark(Fighter fighter, string cue)
        {
            bool player = fighter == round.Player;
            string id = player ? playerVoice : opponentVoice;
            float now = Time.unscaledTime;
            if (id == null || now < (player ? playerReady : opponentReady)) return;
            if (!AudioManager.PlayVoice(id + "_" + cue)) return;
            if (player) playerReady = now + .4f;
            else opponentReady = now + .4f;
        }

        private void Hit(Fighter defender, HitInfo hit, DamageResult result)
        {
            bool knockout = (round.Player.Health == 0 || round.Opponent.Health == 0);
            var cue = knockout ? AudioCue.Knockout : result.GuardBroken ? AudioCue.GuardBreak :
                result.Blocked ? AudioCue.Block : hit.Attacker != null && hit.Attacker.IsSpecialMove(hit.Move)
                ? AudioCue.Special : result.Damage >= 12 ? AudioCue.Heavy : AudioCue.Hit;
            AudioManager.Play(cue);
            if (knockout)
            {
                if (!AudioManager.PlayVoice("announcer_ko", true)) Bark(defender, "ko");
            }
            else if (!result.Blocked && result.Damage > 0)
                Bark(defender, result.Damage >= 12 ? "hit_heavy" : "hit_light");
        }

        private void RoundStarted(int number)
        {
            playerVoice = round.Player.VoiceId;
            opponentVoice = round.Opponent.VoiceId;
            playerReady = opponentReady = 0f;
            AudioManager.Play(AudioCue.Round);
            if (number == 1 || !AudioManager.PlayVoice(number == 2 ? "announcer_round_two" : "announcer_round_three", true))
            {
                if (!AudioManager.PlayVoice("announcer_fight", true)) Bark(round.Player, "round_start");
            }
        }

        private void BossPhase() => Bark(round.Opponent, "taunt");

        private void SuddenDeath() => AudioManager.PlayVoice("announcer_sudden_death", true);

        private void MatchEnded(RoundResult result)
        {
            AudioManager.Play(AudioCue.Win);
            if (result == RoundResult.PlayerWins) AudioManager.PlayVoice("announcer_win", true);
            if (result == RoundResult.Draw) AudioManager.PlayVoice("announcer_draw", true);
        }
    }
}
