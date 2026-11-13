using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throwables : MonoBehaviour
{
    //boolean for whether the throwable is being picked up or not
    private bool PickedUp = false;
    //boolean for whether the throwable is able to damage the opponent or not
    private bool AttackMode = false;
    //Damage amount to the opponent
    private float Damage;
    public bool flyRightbool = false;
    public bool flyLeftbool = false;

    public float rightfly = 0;
    public float leftfly = 0;

    public Collider[] AttackHitBox;
    public LayerMask HitLayer;

    private Rigidbody rb;
    
    public GameObject Explosion;

    private void Awake() => rb = GetComponent<Rigidbody>();

    public void ThrowObject(bool facingRight)
    {
        if (!PickedUp)
            return;

        transform.SetParent(null);
        rb.isKinematic = false;
        rb.linearVelocity = new Vector3(facingRight ? 4f : -4f, -1f, 0f);
        AttackMode = true;
        PickedUp = false;
    }

    //hit box 
    public void ActivateAttackHitBox(Collider col)
    {
        Collider[] cols = Physics.OverlapBox(col.bounds.center, col.bounds.extents, col.transform.rotation, HitLayer);

        if (cols.Length > 0)
        {
            if (AttackMode)
            {
                //check which part of the opponent did the player hit
                foreach (Collider c in cols)
                {
                    //if the player hit the opponent body then deal more damage
                    if (c.name == "Opponent_Body")
                    {
                        Damage = 10f;
                        Instantiate(Explosion, transform.position, transform.rotation);
                        Destroy(gameObject);
                    }
                    else if (c.name == "Opponent")
                    {
                        Damage = 10f;
                        Instantiate(Explosion, transform.position, transform.rotation);
                        Destroy(gameObject);
                    }

                    //send the damage dealt to the opponent
                    c.GetComponentInParent<EnemyBehaviour>()?.TakeHitOpponent(Damage);
                }
            } 
        }
    }

    //allow player to pickup the throwable
    private void OnTriggerEnter(Collider other)
    {
        //check if the object that collide with the throwable is the player
        if (!PickedUp && !AttackMode && other.TryGetComponent(out PlayerBehaviour player))
        {
            //float newZScale = Mathf.Abs(gameObject.transform.localScale.z);
            //float newYScale = Mathf.Abs(gameObject.transform.localScale.y);
            //float newXScale = Mathf.Abs(gameObject.transform.localScale.x);
            //gameObject.transform.localScale = new Vector3(newXScale, gameObject.transform.localScale.y, newZScale);

            //attach the throwable as a child to the player's right hand
            gameObject.transform.parent = PlayerBehaviour.RightHand.transform;
            //make the position of the throwable to the player's right hand
            gameObject.transform.position = PlayerBehaviour.RightHand.transform.position;

            //set the pickedup boolean to true
            rb.isKinematic = true;
            PickedUp = true;
            player.HoldThrowable(this);
        }
        ActivateAttackHitBox(AttackHitBox[0]);
    }
}
