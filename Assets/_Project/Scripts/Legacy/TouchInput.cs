using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using WobbleBrawl.Core;

public sealed class TouchInput : MonoBehaviour, IFighterInput
{
  
    public PlayerBehaviour playerBehaviour;
    public Throwables throwables;
    private Throwables heldThrowable;

    public void Hold(Throwables item) => heldThrowable = item;

 
    [SerializeField] private Joystick joystick;
    private FighterIntent pending;
    private bool jumpHeld;

    private void Awake()
    {
        if (playerBehaviour != null)
            playerBehaviour.SetTouchInput(this);
    }

    public FighterIntent ReadIntent()
    {
        FighterIntent intent = pending;
        pending = default;
        Joystick stick = joystick != null ? joystick : playerBehaviour != null ? playerBehaviour.joystick : null;
        if (stick != null)
        {
            intent.Move = stick.Horizontal;
            bool jump = stick.Vertical >= 0.5f;
            intent.Jump = jump && !jumpHeld;
            jumpHeld = jump;
            intent.Block = stick.Vertical <= -0.5f;
        }
        return intent;
    }

    public void Kick() => pending.Kick = true;
    public void Punch() => pending.Punch = true;
    public void SpecialAttack() => pending.Special = true;
    public void Throw() => pending.Throw = true;

    public void PerformThrow()
    {
        if (heldThrowable == null || playerBehaviour == null)
            return;
        heldThrowable.ThrowObject(playerBehaviour.FacingRight);
        heldThrowable = null;
    }

    public void restart()
    {
        SceneManager.LoadScene(0);
    }
}
