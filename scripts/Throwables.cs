using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Throwables : MonoBehaviour
{
    //player gameobject
    private GameObject Player;
    //boolean for whether the throwable is being picked up or not
    private bool PickedUp = false;
    //boolean for whether the throwable is able to damage the opponent or not
    private bool AttackMode = false;
    //Damage amount to the opponent
    private float Damage;
    public bool flyRightbool = false;
    public bool flyLeftbool = false;
    private float speed = 5;

    public float rightfly = 0;
    public float leftfly = 0;

    public Collider[] AttackHitBox;
    public LayerMask HitLayer;

    private Rigidbody rb;
    
    public GameObject Explosion;

    private Controller controller;
    private GameObject ControllerGamobject;

    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.Find("Player");
        rb = GetComponent<Rigidbody>();
        ControllerGamobject = GameObject.Find("Controller");
        controller = ControllerGamobject.GetComponent<Controller>();
       
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ThrowObject();
        
     
    }

    public void Update()
    {
        flyRight();
        flyleft();
        
    }

    public void ThrowObject()
    {
        Debug.Log(Player);
        Player = GameObject.Find("Player");
        rb = GetComponent<Rigidbody>();
   
        if (controller.throwing)
        {
        if (Player.transform.rotation.eulerAngles.y == 90)
            {
                Debug.Log("right");
                //unparent the throwable
                gameObject.transform.parent = null;

                rightfly = 1;
                //rb.velocity = new Vector3(-4 * transform.localScale.x, -1,0);
                //rb.AddForce ( new Vector3(-4 * transform.localScale.x, -1,0), ForceMode.Impulse);
                //rb.velocity = new Vector3(-3.1f, 0, 0);
                Debug.Log(rb.velocity);
                //gameObject.transform.Translate(new Vector3(-4 * speed * transform.localScale.x, -1, 5));
                Debug.Log("true right");
               AttackMode = true;
                    PickedUp = false;
            }

            else if (Player.transform.rotation.eulerAngles.y == 270)
            {
                Debug.Log("left");
                //unparent the throwable
                gameObject.transform.parent = null;
            //rb.AddForce(new Vector3(-4 * transform.localScale.x, -1, 0), ForceMode.Impulse);
            //add force to make it fly left
            //rb.velocity = new Vector3(4 * transform.localScale.x, -1,0);
            leftfly = 1;
                
                //rb.velocity = new Vector3(3.1f, 0,0);
                Debug.Log(rb.velocity);
                //gameObject.transform.Translate(new Vector3(4 * speed * transform.localScale.x, -1, 5));
              AttackMode = true;
                PickedUp = false;
            }
       }

       
        
    }


    public void flyleft()
    {
      if(leftfly == 1)
        {
  Debug.Log("timer right");

            rb.velocity = new Vector3(4 * transform.localScale.x, -1, 0);
        }
          
        
       
    }

    public void flyRight()
    {
        if (rightfly == 1)
        {
Debug.Log("timer right");
            rb.velocity = new Vector3(-4 * transform.localScale.x, -1, 0);
        }
            
        
    
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
                    c.SendMessageUpwards("TakeHitOpponent", Damage);
                }
            } 
        }
    }

    //allow player to pickup the throwable
    private void OnTriggerEnter(Collider other)
    {
        //check if the object that collide with the throwable is the player
        if(other.gameObject.name == "Player")
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
            PickedUp = true;
        }
        ActivateAttackHitBox(AttackHitBox[0]);
    }
}
