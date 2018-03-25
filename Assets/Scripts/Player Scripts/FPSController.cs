﻿using UnityEngine;


//-----------------------------------------------------------------------------------------------------------------------------------------------------
//
//  FPS CONTROLLER
//
//  Contains:   Three floats for movement speed, a float for jump speed, and a float for gravity;
//              A float to keep track of Y Velocity.
//              A float for the force you will exert on colliding with other objects;
//              A float for crouch height, a float for how far the player should pop up on the Y axis upon standing up;
//              Two floats, one to multiply gravity when Y velocity is negative, and another for when it is positive;
//              Two floats providing a buffer for detecting objects above and below the Player, respectively. Higher amounts detect objects 
//              further away;
//              The booleans corresponding to the  above buffers;
//              A private CharacterController, InputManager and ActionManager;
//              A float to remember character height, to set the CC height back to normal upon standing up;
//
//  Use:        Place on the Player/Controlled Character;
//              Create and attach a Keyset to the InputManager;
//
//  Purpose:    To provide controls and movement for the Player/Controlled Character;
//
//-----------------------------------------------------------------------------------------------------------------------------------------------------
//
//  Notes:      N/A;
//
//-----------------------------------------------------------------------------------------------------------------------------------------------------


[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(ActionManager))]
[RequireComponent(typeof(InputManager))]
[RequireComponent(typeof(Animator))]
public class FPSController : MonoBehaviour
{
    [Space(10)]

    //Speeds, Forces and Gravity
    public float walkSpeed      = 10.0f;
    public float runSpeed       = 14.0f;
    public float crouchSpeed    = 2.00f;
    public float jumpSpeed      = 4.00f;
    public float gravity        = 9.81f;
    public float pushPower      = 8.00f;
    public float liftPower      = 5.00f;                           
    private float YVelocity;                                            [Space(16)]


    //Variables effecting Actions
    public float crouchHeight = 0;
    public float popOutOfGround = 0.60f;                                [Space(6)]

    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 1.5f;                              [Space(6)]

    public float ceilingDetectionBuffer = 0.40f;
    public float groundDetectionBuffer = 0.10f;                         [Space(16)]

    public bool canStand;
    public bool isCloseToGround;


    //Managers and Controllers
    private CharacterController cc;
    private ActionManager am;
    private InputManager im;
    private Animator a;

    //Character Height
    private float characterHeight;




    //FUNCTIONS

    void Start()
    {
        cc = GetComponent<CharacterController>();
        am = GetComponent<ActionManager>();
        im = GetComponent<InputManager>();
        a = GetComponent<Animator>();
        characterHeight = cc.height;
    }



    private void Update()
    {
        doActions();
        a.SetFloat("Speed", Input.GetAxis("Vertical") * 80 * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        cc.Move((getInputXZ() + getInputY()) * Time.deltaTime);
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        if (body == null || body.isKinematic)
            return;
        if (hit.moveDirection.y < -0.3F)
            return;

        float mag = cc.velocity.magnitude;

        if (mag < 0)
            mag *= -1;

        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        body.velocity = pushDir * (mag + pushPower - body.mass);
    }



    void doActions()
    {
        canStand = !Physics.SphereCast(new Ray(transform.position + Vector3.up * characterHeight, Vector3.up), cc.radius, ceilingDetectionBuffer);
        isCloseToGround = Physics.SphereCast(new Ray(transform.position + (cc.radius * 1.1f) * Vector3.up, Vector3.down), cc.radius, groundDetectionBuffer);


        //If we collide with the ground our velocity should be 0, and we should not be jumping
        if (cc.isGrounded)
        {
            YVelocity = 0;

            if (am.doing(eActions.JUMP))
                am.nowNotDoing(eActions.JUMP);
        }


        //If we crouch, our height should be reduced by a specified amount, else if we want to stand up and have the space to do so, our height should go back to normal
        if (im.getKey(eActions.CROUCH))
        {
            am.nowDoing(eActions.CROUCH);
            cc.height = crouchHeight;
        }
        else if (am.doing(eActions.CROUCH) && canStand)
        {
            am.nowStopping(eActions.CROUCH);
            cc.height = characterHeight;
            transform.position += new Vector3(0, popOutOfGround, 0);
        }
        else if (am.stopping(eActions.CROUCH))
            am.nowNotDoing(eActions.CROUCH);


        if (im.getKeyDown(eActions.JUMP) && isCloseToGround && am.notDoing(eActions.CROUCH))
        {
            am.nowDoing(eActions.JUMP);
            YVelocity = jumpSpeed;
        }


        if (im.getKey(eActions.RUN) && am.notDoing(eActions.CROUCH))
            am.nowDoing(eActions.RUN);
        else
            am.nowNotDoing(eActions.RUN);
    }



    Vector3 getInputXZ()
    {
        return transform.TransformDirection(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * getMoveSpeed();
    }

    Vector3 getInputY()
    {
        YVelocity -= gravity * Time.deltaTime;

        //Allows "Mario Jumping": Fall faster, jump higher the longer you press the jump key.
        if (YVelocity < 0)
            YVelocity -= gravity * (fallMultiplier - 1) * Time.deltaTime;
        else if (YVelocity > 0 && !im.getKey(eActions.JUMP))
            YVelocity -= gravity * (lowJumpMultiplier - 1) * Time.deltaTime;

        return new Vector3(0, YVelocity, 0);
    }



    //Return movement speed based on whether the player is walking, running, etc
    float getMoveSpeed()
    {
        if (am.doing(eActions.RUN))
            return runSpeed;
        if (am.doing(eActions.CROUCH))
            return crouchSpeed;

        return walkSpeed;
    }
}