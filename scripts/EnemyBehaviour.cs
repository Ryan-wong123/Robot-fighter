using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyBehaviour : MonoBehaviour
{

    private Animator animator;
    private Rigidbody rb;
    private Transform TargetPlayer;
    private Text Health;
    private Image AttackGauge;

    //hitbox attributes
    public Collider[] AttackHitBoxOpponent;
    public LayerMask HitLayer;

    private float WalkingSpeed = 1f;
    private float AttackDistance = 1f;
    private float MoveToPlayer = 0.2f;
    private float CurrentAttackTime;
    private float DefaultAttackTime = 2f;
    private bool FollowPlayer;
    private bool AttackPlayer;
    private float AttackType;
    public float MaxHealth = 200f;
    public bool isEnemyDead = false;
    private float HighDamage = 10f;
    private float LowDamage = 5f;
    private float Damage = 0f;
    private float SpecialAttack = 0f;
    private float SpecialAttackDamage = 15f;
    private bool SpecialAttackReady = false;
    private bool ActivateSpecialAttack = false;

    //particle effect
    public GameObject HitEffect;

    // Start is called before the first frame update
    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        TargetPlayer = GameObject.FindWithTag("Player").transform;
        Health = GameObject.Find("OpponentHealth").GetComponent<Text>();
        AttackGauge = GameObject.Find("EnemyAttackGauge").GetComponent<Image>();
        AttackGauge.fillAmount = 0f;
        FollowPlayer = true;
        CurrentAttackTime = DefaultAttackTime;
        
    }

    // Update is called once per frame
    public void Update()
    {
        FollowTarget();

        if(MaxHealth <= 50)
        {
            Health.color = Color.red;
        }
       
    }

    public void FixedUpdate()
    {
        BasicAttack();
    }

    public void FollowTarget()
    {
        //check if not supposed to follow player
        if (!FollowPlayer)      
            return;
        
        if(isEnemyDead == false)
        {
            //check if the distance between the enemy and target player is more than the attack distance
            if (Vector3.Distance(transform.position, TargetPlayer.position) > AttackDistance)
            {
                //calculate the position of the player
                Vector3 TargetPosition = new Vector3(TargetPlayer.position.x, transform.position.y, TargetPlayer.transform.position.z);
                //rotate the enemy to face the player
                transform.LookAt(TargetPosition);
                //move the enemy in the direction of the player
                rb.velocity = transform.forward * WalkingSpeed;

                if (rb.velocity.sqrMagnitude != 0)
                {
                    animator.SetBool("WalkingToPlayer", true);
                }
            }

            //check if the distance between the enemy and target player is less than or equal to the attack distance
            else if (Vector3.Distance(transform.position, TargetPlayer.position) <= AttackDistance)
            {
                //stop walking
                rb.velocity = Vector3.zero;
                animator.SetBool("WalkingToPlayer", false);

                FollowPlayer = false;
                AttackPlayer = true;
                
            }
        }
        
    }


    public void AttackHitBoxPunchleft()
    {
        ActivateAttackHitBoxOpponent(AttackHitBoxOpponent[0]);
    }
    public void AttackHitBoxPunchRight()
    {
        ActivateAttackHitBoxOpponent(AttackHitBoxOpponent[1]);
    }
    public void AttackHitBoxKickRight()
    {
        ActivateAttackHitBoxOpponent(AttackHitBoxOpponent[2]);
    }
    public void AttackHitBoxKickLeft()
    {
        ActivateAttackHitBoxOpponent(AttackHitBoxOpponent[3]);
    }

    public void AttackHitBoxSpecialAttack()
    {
        ActivateAttackHitBoxOpponent(AttackHitBoxOpponent[4]);
    }

    public void BasicAttack()
    {
       
        //check if not supposed to attack player
        if (!AttackPlayer)
            return;
        
        CurrentAttackTime += Time.deltaTime;

        if(CurrentAttackTime > DefaultAttackTime)
        {

            if(isEnemyDead == false)
            {

                if (SpecialAttackReady)
                {
                    animator.SetTrigger("Head_Ram");
                    ActivateSpecialAttack = true;
                    SpecialAttack = 0;
                    AttackGauge.fillAmount = 0;
                }

                else
                {
                    //random generate a number and trigger the attack corresponding to the number
                    AttackType = Random.Range(0, 3);

                    if (AttackType == 0)
                    {
                        animator.SetTrigger("Punch_Left");
                    }
                    else if (AttackType == 1)
                    {
                        animator.SetTrigger("Punch_Right");
                    }
                    else if (AttackType == 2)
                    {
                        animator.SetTrigger("Kick_Right");
                    }
                    else if (AttackType == 3)
                    {
                        animator.SetTrigger("Kick_Left");
                    }

                    
                }
                CurrentAttackTime = 0f;

            }

            if (Vector3.Distance(transform.position, TargetPlayer.position) > AttackDistance + MoveToPlayer)
            {
                AttackPlayer = false;
                FollowPlayer = true;
            }
        }
          
    }
    

    public void EnemyDieAnim()
    {
        animator.SetTrigger("Fall_Down");
    }
    //opponent gets hit and minus health
    public void TakeHitOpponent(float EnemyDamage)
    {
        //recieve the damage dealt from player and minus it from the health
        MaxHealth -= EnemyDamage;

        if (MaxHealth <= 0)
        {
            if (isEnemyDead == false)
            {
                MaxHealth = 0;
                animator.SetTrigger("Fall_Down");
                isEnemyDead = true;
            }
        }

        if (isEnemyDead)
        {
            MaxHealth = 0;
            animator.SetBool("LieDown", true);

        }

        else if (MaxHealth > 0)
        {
            animator.SetTrigger("Getting_Hit");
        }

        Health.text = MaxHealth.ToString("") + "%";
    }

    //hit box 
    public void ActivateAttackHitBoxOpponent(Collider col)
    {
        Collider[] cols = Physics.OverlapSphere(col.bounds.center, 0.5f, HitLayer);

        if (cols.Length > 0)
        {
           

            //check which part of the opponent did the player hit
            foreach (Collider c in cols)
            {
                
                //if the player hit the opponent body then deal more damage
                if (c.name == "Player_Body")
                {

                    if (ActivateSpecialAttack)
                    {
                        Damage = SpecialAttackDamage;
                        SpecialAttackReady = false;
                        ActivateSpecialAttack = false;
                        Instantiate(HitEffect, col.transform.position, col.transform.rotation);
                    }
                    else
                    {
                        Damage = HighDamage;
                        SpecialAttack += 0.1f;
                        Instantiate(HitEffect, col.transform.position, col.transform.rotation);
                        if (SpecialAttack >= 1)
                        {
                            SpecialAttackReady = true;
                            SpecialAttack = 1;
                        }
                        AttackGauge.fillAmount = SpecialAttack;
                    }   
                }

                //if the player hit the opponent legs then deal lesser damage
                else if (c.name == "Player")
                {

                    if (ActivateSpecialAttack)
                    {
                        Damage = SpecialAttackDamage;
                        SpecialAttackReady = false;
                        ActivateSpecialAttack = false;
                        Instantiate(HitEffect, col.transform.position, col.transform.rotation);
                    }
                    else
                    {
                        Damage = LowDamage;
                        SpecialAttack += 0.1f;
                        Instantiate(HitEffect, col.transform.position, col.transform.rotation);
                        if (SpecialAttack >= 1)
                        {
                            SpecialAttackReady = true;
                            SpecialAttack = 1;
                        }
                        AttackGauge.fillAmount = SpecialAttack;
                    }               
                }  

                //send the damage dealt to the opponent
                c.SendMessageUpwards("TakeHitPlayer", Damage);

            }
        }
    }

    public void Win()
    {
        animator.SetTrigger("Win");
    }
}
