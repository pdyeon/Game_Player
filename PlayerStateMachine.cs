using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine.Unity;

public class PlayerStateMachine : MonoBehaviour
{
    // 애니메이션
    Spine.AnimationState state;
    Spine.Skeleton skeleton;

    public enum PlayerState
    {
        Idle = 0,
        Move,
        Jump,
        Climb,
        Faint
    }
    public PlayerState playerState = PlayerState.Idle;

    private bool isLeft = false;
    private bool isAniChange = false;
    private bool isJumping = false;
    [SerializeField]
    private bool isRope = false;
    private bool isRopeTopEnd = false;
    private bool isRopeDownEnd = false;
    [SerializeField]
    private bool isGround = false;

    public float movePower = 1.0f;
    public float jumpPower = 1.0f;
    private float gravityPower;
    //private Vector3 ropePos;
    Rigidbody2D rigid;
    Transform tr;

    private void Start()
    {
        var skeletonAnimation = this.GetComponent<SkeletonAnimation>();
        state = skeletonAnimation.state;
        skeleton = skeletonAnimation.skeleton;

        state.SetAnimation(0, "player1_idle_LR", true);
        rigid = GetComponent<Rigidbody2D>();
        tr = this.gameObject.transform;
        gravityPower = rigid.gravityScale;
    }

    private void FixedUpdate()
    {
        StateMachine();
        Move();
        Climbing();
    }

    private void StateMachine()
    {
        // 특정상태에서 특정키를 눌렀을때 제어값 True
        switch (playerState)
        {
            case PlayerState.Idle:
                //만약 move키를 눌렀을때 = moveState
                if(Input.GetAxisRaw("Horizontal") < 0 || Input.GetAxisRaw("Horizontal") > 0)
                {
                    StateChange(PlayerState.Move);
                }
                //만약 jump키를 눌렀을때 + 땅에 닿을때 = jumpState
                if(Input.GetKey(KeyCode.Space) && isGround)
                {
                    StateChange(PlayerState.Jump);
                    isJumping = true;
                    Jump();
                }
                //만약 climb제어값이 True일 경우 위 화살표를 눌렀을때 = climbstate
                if(Input.GetKey(KeyCode.UpArrow) && isRope)
                {
                    StateChange(PlayerState.Climb);
                }
                break;
            case PlayerState.Move:
                //만약 방향키를 누르지 않을때 = IdleState
                if(Input.GetAxisRaw("Horizontal") == 0)
                {
                    StateChange(PlayerState.Idle);
                }
                //만약 jump키를 눌렀을때 + 땅에 닿을때 = jumpState
                if(Input.GetKey(KeyCode.Space) && isGround)
                {
                    isJumping = true;
                    Jump();
                    StateChange(PlayerState.Jump);
                }
                //만약 climb제어값이 True일 경우 위 화살표를 눌렀을때 = climbstate
                if(Input.GetKey(KeyCode.UpArrow) && isRope)
                {
                    StateChange(PlayerState.Climb);
                }
                break;
            case PlayerState.Jump:
                //만약 땅에 닿을때 = idleState
                if(isGround) //&& rigid.velocity.y < 0)
                {
                    StateChange(PlayerState.Idle);
                }
                if (Input.GetKey(KeyCode.UpArrow) && isRope)
                {
                    rigid.velocity = Vector3.zero;
                    StateChange(PlayerState.Climb);
                }
                break;
            case PlayerState.Climb:
                
                if (Input.GetKey(KeyCode.Space))
                {
                    state.TimeScale = 1;
                    rigid.gravityScale = gravityPower;
                    isJumping = true;
                    Jump();
                    StateChange(PlayerState.Jump);
                }
                //Climb 오브젝트에 닿지 않을때 = IdleState
                break;
            case PlayerState.Faint:
                //일정시간후 스폰포인트로 이동
                break;
        }
    }

    private void AniMuchine()
    {
        if(isAniChange)
        {
            switch (playerState)
            {
                case PlayerState.Idle:
                    state.SetAnimation(0, "player1_idle_LR", true);
                    break;
                case PlayerState.Move:
                    state.SetAnimation(0, "player1_run_LR", true);
                    break;
                case PlayerState.Jump:
                    state.SetAnimation(0, "player1_jump", false);
                    break;
                case PlayerState.Climb:
                    state.SetAnimation(0, "player1_rope_up", true);
                    break;
                case PlayerState.Faint:
                    state.SetAnimation(0, "player1_faint", true);
                    break;
            }
            isAniChange = false;
        }
    }

    // Climb오브젝트에 닿을때 Climb 제어값 true
    // 땅에 닿은상태 체크
    // 이동은 따로작성
    // StateChange
    private void StateChange(PlayerState _playerState)
    {
        playerState = _playerState;
        isAniChange = true;
        AniMuchine();
    }

    private void Move()
    {
        if(playerState != PlayerState.Climb)
        {
            Vector3 moveVelocity = Vector3.zero;

            if (Input.GetAxisRaw("Horizontal") < 0)
            {
                moveVelocity = Vector3.left;
                transform.localScale = new Vector3(-0.5f, 0.5f, 1);
            }
            else if (Input.GetAxisRaw("Horizontal") > 0)
            {
                moveVelocity = Vector3.right;
                transform.localScale = new Vector3(0.5f, 0.5f, 1);
            }

            transform.position += moveVelocity * movePower * Time.deltaTime;
        }  
    }

    private void Jump()
    {
        if (!isJumping)
            return;

        rigid.velocity = Vector2.zero;

        Vector2 jumpVelocity = new Vector2(0, jumpPower);
        rigid.AddForce(jumpVelocity, ForceMode2D.Impulse);

        isJumping = false;
        isGround = false;
    }

    private void Climbing()
    {
        if (playerState != PlayerState.Climb)
            return;

        isGround = false;
        isJumping = false;
        rigid.gravityScale = 0.0f;
        rigid.velocity = Vector3.zero;
        Vector3 moveVelocity = Vector3.zero;
        float ropePower = movePower / 4;
        //tr.position = new Vector3(ropePos.x, tr.position.y, tr.position.z);

        if (Input.GetAxisRaw("Vertical") < 0 && !isRopeDownEnd)
        {
            state.TimeScale = 1;
            moveVelocity = Vector3.down;
            transform.position += moveVelocity * ropePower * Time.deltaTime;
        }
        else if (Input.GetAxisRaw("Vertical") > 0 && !isRopeTopEnd)
        {
            state.TimeScale = 1;
            moveVelocity = Vector3.up;
            transform.position += moveVelocity * ropePower * Time.deltaTime;
        }
        else
            state.TimeScale = 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isGround = true;
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Rope"))
        {
            isRope = true;
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("RopeTopEnd"))
        {
            isRope = true;
            isRopeTopEnd = true;
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("RopeDownEnd"))
        {
            isRope = true;
            isRopeDownEnd = true;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            isGround = true;
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("Rope"))
        {
            isRope = true;
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("RopeTopEnd"))
        {
            isRope = true;
            isRopeTopEnd = true;
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("RopeDownEnd"))
        {
            isRope = true;
            isRopeDownEnd = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Rope"))
        {
            isRope = false;
        }
        
        if (collision.gameObject.layer == LayerMask.NameToLayer("RopeTopEnd"))
        {
            isRope = false;
            isRopeTopEnd = false;
        }
        if (collision.gameObject.layer == LayerMask.NameToLayer("RopeDownEnd"))
        {
            isRope = false;
            isRopeDownEnd = false;
        }
    }
}
