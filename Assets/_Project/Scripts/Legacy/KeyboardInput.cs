using UnityEngine;
using WobbleBrawl.Core;

public sealed class KeyboardInput : IFighterInput
{
    public FighterIntent ReadIntent() => new FighterIntent
    {
        Move = Input.GetAxisRaw("Horizontal"),
        Jump = Input.GetKeyDown(KeyCode.W),
        Block = Input.GetKey(KeyCode.S),
        Punch = Input.GetKeyDown(KeyCode.J),
        Kick = Input.GetKeyDown(KeyCode.K),
        Special = Input.GetKeyDown(KeyCode.L),
        Throw = Input.GetKeyDown(KeyCode.U)
    };
}
