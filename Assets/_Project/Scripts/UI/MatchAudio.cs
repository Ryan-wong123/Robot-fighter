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

        private void Awake()
        {
            AudioManager.Ensure(transform);
            round = GetComponent<RoundController>();
            match = GetComponent<MatchController>();
        }

        private void OnEnable()
        {
            round.Player.HitResolved += Hit;
            round.Opponent.HitResolved += Hit;
            match.RoundStarted += RoundStarted;
            match.MatchEnded += MatchEnded;
        }

        private void OnDisable()
        {
            if (round != null)
            {
                if (round.Player != null) round.Player.HitResolved -= Hit;
                if (round.Opponent != null) round.Opponent.HitResolved -= Hit;
            }
            if (match != null)
            {
                match.RoundStarted -= RoundStarted;
                match.MatchEnded -= MatchEnded;
            }
        }

        private void Hit(HitInfo hit, DamageResult result)
        {
            bool knockout = (round.Player.Health == 0 || round.Opponent.Health == 0);
            var cue = knockout ? AudioCue.Knockout : result.GuardBroken ? AudioCue.GuardBreak :
                result.Blocked ? AudioCue.Block : hit.Attacker != null && hit.Attacker.IsSpecialMove(hit.Move)
                ? AudioCue.Special : result.Damage >= 12 ? AudioCue.Heavy : AudioCue.Hit;
            AudioManager.Play(cue);
        }

        private void RoundStarted(int _) => AudioManager.Play(AudioCue.Round);
        private void MatchEnded(RoundResult _) => AudioManager.Play(AudioCue.Win);
    }
}
