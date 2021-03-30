using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBehaviour : MonoBehaviour
{

    public enum AttackComboState {
        NONE,
        Punch1,
        Punch2,
        Punch3,
        Punch4,
        Kick1,
        Kick2
    }

    //get the object of the right hand of the player
    public static GameObject RightHand
    {
        get
        {
            return FindObjectOfType<RightHandPickup>().gameObject;
        }
    }

    //declare the health of player
    public float MaxHealth = 200f;
    private bool isPlayerDead = false;
    public float deadCount = 0;

    //declare the walking speed for player
    private float WalkingSpeed = 3f;

    //declare the rotation of player
    private float RotationValue = 90f;

    //declare the jump force of the player
    private float JumpForce = 10f;
    private float JumpCount;

    //declare combo gauge
    private float SpecialAttack = 0f;
    private Image AttackGauge;

    //revive 
    private float reviveBtnPressed = 0;

    //combo attack attributes
    private bool TimerToReset;
    private float DefaultComboCountdown = 0.5f;
    private float CurrentComboCountDown;
    private AttackComboState CurrentComboState;

    //hitbox attributes
    public Collider[] AttackHitBox;
    public LayerMask HitLayer;
    private Text Health;
    private float HighDamage = 10f;
    private float LowDamage = 5f;
    private float SpecialAttackDamage = 15f;
    private bool SpecialAttackReady = false;
    private bool ActivateSpecialAttack = false;

    //Create instance
    private Animator animator;
    private Rigidbody rb;

    public GameObject HitEffect;

    public Joystick joystick;

    // Start is called before the first frame update
    public void Start()
    {

        Health = GameObject.Find("PlayerHealth").GetComponent<Text>();
        AttackGauge = GameObject.Find("PlayerAttackGauge").GetComponent<Image>();
        AttackGauge.fillAmount = 0f;

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        JumpCount = 0f;

        //combo
        CurrentComboCountDown = DefaultComboCountdown;
        CurrentComboState = AttackComboState.NONE;
        CurrentComboState = (AttackComboState)1;

    }

    // Update is called once per frame
    public void Update()
    {
        //Attack();
        ResetAttackComboState();
        //Revive();
        //Punch();
        //Kick();
        //specialAttack();

        if(MaxHealth <= 50)
        {
            Health.color = Color.red;
        }
    }

    public void FixedUpdate()
    {
        Movement();
    }

    //movement for player (left or right or jump)
    public void Movement()
    {

        //player jump
        if (Input.GetKeyDown(KeyCode.W) || joystick.Vertical >= .5f && JumpCount != 1)
        {
           
            animator.SetTrigger("Jump");
            rb.velocity = (Vector3.up * JumpForce);
            //prevent double jump
            JumpCount += 1;

        }

        //do blocking for the player
        if (Input.GetKey(KeyCode.S) || joystick.Vertical <= -.5f)
        {
           
            //play the blocking animation
            animator.SetTrigger("Block");
        }

        //check if the direction that the player is moving towards the right and rotate the player to face right 
        if (Input.GetAxisRaw("Horizontal") > 0 || joystick.Horizontal > 0)
        {
            //rotate the player to facing right side
            transform.rotation = Quaternion.Euler(0f, Mathf.Abs(RotationValue), 0f);
            //player movement direction is corresponding to the direction it is facing
            transform.Translate(Vector3.forward * WalkingSpeed * Time.deltaTime);
            //set the boolean to true and play the walking animation
            animator.SetBool("Walk", true);
        }

        //check if the direction that the player is moving towards the left and rotate the player to face left
        else if (Input.GetAxisRaw("Horizontal") < 0 || joystick.Horizontal < 0)
        {
            //rotate the player to facing left side
            transform.rotation = Quaternion.Euler(0f, -Mathf.Abs(RotationValue), 0f);
            //player movement direction is corresponding to the direction it is facing
            transform.Translate(Vector3.forward * WalkingSpeed * Time.deltaTime);
            //set the boolean to true and play the walking animation
            animator.SetBool("Walk", true);
        }

        else
        {
            //set the boolean to false and stop the walking animation
            animator.SetBool("Walk", false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Floor")
        {
            //reset jump count to 0
            JumpCount = 0;
        }
    }

    //dieing animation
    public void Die()
    {
        animator.SetTrigger("Knockdown");
    }

    //basic attack for the player
    public void Attack()
    {
        //punching
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (CurrentComboState == AttackComboState.Punch4 || CurrentComboState == AttackComboState.Kick1 || CurrentComboState == AttackComboState.Kick2)
                return;

            CurrentComboState++;
            TimerToReset = true;
            CurrentComboCountDown = DefaultComboCountdown;

            if(CurrentComboState == AttackComboState.Punch1)
            {
                //perform a punch
                animator.SetTrigger("RightUpperCut");
                
            }

            if (CurrentComboState == AttackComboState.Punch2)
            {
                //perform a lower left cut punch
                animator.SetTrigger("LeftLowCut");
               
            }

            if (CurrentComboState == AttackComboState.Punch3)
            {
                //perform a lower right cut punch
                animator.SetTrigger("RightLowCut");
               
            }

            if (CurrentComboState == AttackComboState.Punch4)
            {
                //perform a left hook punch
                animator.SetTrigger("LeftHook");
                
            }
        }
        
        //kicking
        else if (Input.GetKeyDown(KeyCode.E))
        {
            //if current combo is punch 4 or kick2, exit the code as no combos to activate
            if (CurrentComboState == AttackComboState.Kick2 || CurrentComboState == AttackComboState.Punch4)
            {
                return;
            }
            
            //if current combo state is none or punch1 or punch2 or punch3, set attackcombostate to kick 1
            if(CurrentComboState == AttackComboState.NONE || CurrentComboState == AttackComboState.Punch1 || CurrentComboState == AttackComboState.Punch2 || CurrentComboState == AttackComboState.Punch3)
            {
                CurrentComboState = AttackComboState.Kick1;
            }

            else if (CurrentComboState == AttackComboState.Kick1)
            {
                //move to attackcombostate kick2
                CurrentComboState++;
            }

            TimerToReset = true;
            CurrentComboCountDown = DefaultComboCountdown;

            if(CurrentComboState == AttackComboState.Kick1)
            {
                //perform left kick
                animator.SetTrigger("LeftKick");
                
            }

            if (CurrentComboState == AttackComboState.Kick2)
            {
                //perform Right Upper Kick 
                animator.SetTrigger("RightUpperKick");
              
            }
        }

        //special attack
        else if (Input.GetKeyDown(KeyCode.Q) && SpecialAttackReady)
        {
            
            //perform special attack
            animator.SetTrigger("SpecialAttack");
            ActivateSpecialAttack = true;
            SpecialAttack = 0;
        }
    }

    public void Punch()
    {
        //punching
        //if (Input.GetKeyDown(KeyCode.R))
        //{
            Debug.Log("Punch");

            if (CurrentComboState == AttackComboState.Punch4 || CurrentComboState == AttackComboState.Kick1 || CurrentComboState == AttackComboState.Kick2)
                return;

            CurrentComboState++;
            TimerToReset = true;
            CurrentComboCountDown = DefaultComboCountdown;

            if (CurrentComboState == AttackComboState.Punch1)
            {
                //perform a punch
                animator.SetTrigger("RightUpperCut");

            }

            if (CurrentComboState == AttackComboState.Punch2)
            {
                //perform a lower left cut punch
                animator.SetTrigger("LeftLowCut");

            }

            if (CurrentComboState == AttackComboState.Punch3)
            {
                //perform a lower right cut punch
                animator.SetTrigger("RightLowCut");

            }

            if (CurrentComboState == AttackComboState.Punch4)
            {
                //perform a left hook punch
                animator.SetTrigger("LeftHook");

            }
        //}
    }

    public void specialAttack()
    {
        //special attack
        if (SpecialAttackReady)
        {
            Debug.Log("sp");

            //perform special attack
            animator.SetTrigger("SpecialAttack");
            ActivateSpecialAttack = true;
            SpecialAttack = 0;
        }
    }

    public void Kick()
    {
        //kicking
        //if (Input.GetKeyDown(KeyCode.E))
        //{

            Debug.Log("kick");
            //if current combo is punch 4 or kick2, exit the code as no combos to activate
            if (CurrentComboState == AttackComboState.Kick2 || CurrentComboState == AttackComboState.Punch4)
            {
                return;
            }

            //if current combo state is none or punch1 or punch2 or punch3, set attackcombostate to kick 1
            if (CurrentComboState == AttackComboState.NONE || CurrentComboState == AttackComboState.Punch1 || CurrentComboState == AttackComboState.Punch2 || CurrentComboState == AttackComboState.Punch3)
            {
                CurrentComboState = AttackComboState.Kick1;
            }

            else if (CurrentComboState == AttackComboState.Kick1)
            {
                //move to attackcombostate kick2
                CurrentComboState++;
            }

            TimerToReset = true;
            CurrentComboCountDown = DefaultComboCountdown;

            if (CurrentComboState == AttackComboState.Kick1)
            {
                //perform left kick
                animator.SetTrigger("LeftKick");

            }

            if (CurrentComboState == AttackComboState.Kick2)
            {
                //perform Right Upper Kick 
                animator.SetTrigger("RightUpperKick");

            }
        //}
    }

    public void Revive()
    {
        //revive and stand up
        if (Input.GetKeyDown(KeyCode.G) && deadCount < 1 && isPlayerDead)
        {
            reviveBtnPressed++;

            if (reviveBtnPressed == 4)
            {
                animator.SetTrigger("StandUp");
                MaxHealth = 200f;
                Health.text = MaxHealth.ToString("") + "%";
                isPlayerDead = false;
                deadCount++;
            }
        }
    }

   public void PlayerDieAnim()
    {
        animator.SetTrigger("Knockdown");
    }

    public void ActivateLeftPunchHitBox()
    {
        ActivateAttackHitBox(AttackHitBox[0]);
    }

    public void ActivateRightPunchHitBox()
    {
        ActivateAttackHitBox(AttackHitBox[1]);
    }

    public void ActivateLeftKickHitBox()
    {
        ActivateAttackHitBox(AttackHitBox[2]);
    }

    public void ActivateRightKickHitBox()
    {
        ActivateAttackHitBox(AttackHitBox[3]);
    }

    public void ActivateSpecialAttackHitBox()
    {
        ActivateAttackHitBox(AttackHitBox[4]);
    }

    //setting the comboState to None after completing a combo
    public void ResetAttackComboState()
    {
        if (TimerToReset)
        {
            CurrentComboCountDown -= Time.deltaTime;
            
            if(CurrentComboCountDown <= 0f)
            {
                CurrentComboState = AttackComboState.NONE;

                TimerToReset = false;
                CurrentComboCountDown = DefaultComboCountdown;
                
            }
        }
    }

    //hit box 
    public void ActivateAttackHitBox(Collider col)
    {
        Collider[] cols = Physics.OverlapSphere(col.bounds.center, 1f, HitLayer);

        if (cols.Length > 0)
        {
            //Debug.Log(cols[0].gameObject.name);

            //check which part of the opponent did the player hit
            foreach(Collider c in cols)
            {
                float EnemyDamage = 0f;

                //if the player hit the opponent body then deal more damage
                if(c.name == "Opponent_Body")
                {
                    
                    if (ActivateSpecialAttack)
                    {
                        EnemyDamage = SpecialAttackDamage;
                        SpecialAttackReady = false;
                        ActivateSpecialAttack = false;
                        Instantiate(HitEffect, col.transform.position, col.transform.rotation);
                    }

                    else
                    {
                        EnemyDamage = HighDamage;
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
                else if(c.name == "Opponent")
                {

                    if (ActivateSpecialAttack)
                    {
                        EnemyDamage = SpecialAttackDamage;
                        SpecialAttackReady = false;
                        ActivateSpecialAttack = false;
                        Instantiate(HitEffect, col.transform.position, col.transform.rotation);
                    }

                    else
                    {
                        EnemyDamage = LowDamage;
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
                else
                {
                    Debug.Log("Not Hitting opponent");
                }

                //send the damage dealt to the opponent
                c.SendMessageUpwards("TakeHitOpponent", EnemyDamage);

            }
        }
    }

    //opponent gets hit and minus health
    public void TakeHitPlayer(float Damage)
    {
        //recieve the damage dealt from player and minus it from the health
        MaxHealth -= Damage;
      
        if (MaxHealth < 0)
        {
                MaxHealth = 0;
                animator.SetTrigger("Knockdown");
                isPlayerDead = true;
        }

        if (isPlayerDead)
        {
            MaxHealth = 0;
            animator.SetTrigger("LayDown");
        }

        else if (MaxHealth > 0)
        {
            animator.SetTrigger("TakingHit");
        }

        Health.text = MaxHealth.ToString("") + "%";
    }

    public void Win()
    {
        animator.SetTrigger("Win");
    }

}
