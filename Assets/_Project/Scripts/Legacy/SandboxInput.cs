using UnityEngine;
using WobbleBrawl.Core;
using WobbleBrawl.Gameplay;

public sealed class SandboxInput : MonoBehaviour, IFighterInput
{
    private readonly KeyboardInput keyboard = new KeyboardInput();
    private FighterIntent pending;
    private Fighter player;

    private void Awake() => player = GetComponent<Fighter>();

    private void Update()
    {
        if (MatchPause.IsPaused)
        {
            pending = default;
            return;
        }
        if (player != null && player.ItemOpponent != null &&
            player.ItemOpponent.TryGetComponent<AIInput>(out var opponent))
        {
            if (Input.GetKeyDown(KeyCode.Space))
                opponent.TogglePause();
            if (Input.GetKeyDown(KeyCode.Alpha1))
                opponent.SelectDifficulty("Easy");
            if (Input.GetKeyDown(KeyCode.Alpha2))
                opponent.SelectDifficulty("Normal");
            if (Input.GetKeyDown(KeyCode.Alpha3))
                opponent.SelectDifficulty("Hard");
        }

        FighterIntent current = keyboard.ReadIntent();
        pending.Move = current.Move;
        pending.Block = current.Block;
        pending.Jump |= current.Jump;
        pending.Punch |= current.Punch;
        pending.Kick |= current.Kick;
        pending.Special |= current.Special;
        pending.Throw |= current.Throw;
    }

    public FighterIntent ReadIntent()
    {
        FighterIntent result = pending;
        pending.Jump = pending.Punch = pending.Kick = pending.Special = pending.Throw = false;
        return result;
    }
}
