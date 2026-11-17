using UnityEngine;
using WobbleBrawl.Core;

public sealed class SandboxInput : MonoBehaviour, IFighterInput
{
    private readonly KeyboardInput keyboard = new KeyboardInput();
    private FighterIntent pending;

    private void Update()
    {
        FighterIntent current = keyboard.ReadIntent();
        pending.Move = current.Move;
        pending.Block = current.Block;
        pending.Jump |= current.Jump;
        pending.Punch |= current.Punch;
        pending.Kick |= current.Kick;
        pending.Special |= current.Special;
    }

    public FighterIntent ReadIntent()
    {
        FighterIntent result = pending;
        pending.Jump = pending.Punch = pending.Kick = pending.Special = false;
        return result;
    }
}
