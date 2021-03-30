using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Controller : MonoBehaviour
{
  
    public PlayerBehaviour playerBehaviour;
    public Throwables throwables;
    public bool throwing = false;

 
    public void Kick()
    {
        playerBehaviour.Kick();
    }

    public void Punch()
    {
        playerBehaviour.Punch();
    }

    public void SpecialAttack()
    {
        playerBehaviour.specialAttack();
    }

    public void Throw()
    {
        throwing = true;
        throwables.ThrowObject();
    }

    public void restart()
    {
        SceneManager.LoadScene(0);
    }
}
