using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 玩家操作控制器 - 马里奥式跳跃系统
/// 负责处理玩家的水平移动、跳跃和重力
/// </summary>
public class PlayerController : MonoBehaviourPun
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    public float acceleration = 10f;
    public float deceleration = 10f;

    [Header("跳跃设置")]
    public float jumpForce = 12f;
    public float jumpForcePlayer = 24f;//玩家踩在别的玩家身上时跳跃的高度
    public float gravity = 25f;
    public float maxFallSpeed = 20f;
    public float jumpBufferTime = 0.2f;  // 跳跃缓冲时间
    public float coyoteTime = 0.2f;      // 土狼时间（离开地面后仍可跳跃的时间）

    [Header("地面检测")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayerMask = 1<<6 | 1<<7; 

    [Header("调试")]
    public bool showDebugInfo = true;

    // 物理组件
    private Rigidbody2D rb;

    // 移动状态
    private float horizontalInput;
    private float currentVelocityX;

    // 跳跃状态
    private bool isGrounded;
    private bool wasGrounded;
    public float jumpBufferTimer;
    public float coyoteTimer;
    private bool jumpPressed;


    [Header("脚部检测：用于防止多次跳跃")]
    public Transform _virtual_foot;
    public Transform _virtual_head;
    [SerializeField] private bool isJumping;


    public BoxCollider2D _collider;
    void Awake()
    {
        // 获取或添加Rigidbody2D组件
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        // 设置Rigidbody2D参数
        rb.freezeRotation = true;  // 防止旋转
                                   // rb.gravityScale = 0;       // 使用自定义重力

        // 如果没有地面检测点，创建一个
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }

    private void Start() {
        _virtual_foot = transform.Find("Virtual foot");
        _virtual_head = transform.Find("Virtual head");
        _collider = transform.GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        HandleInput();
        CheckGrounded();
        HandleJumpBuffer();
        HandleCoyoteTime();


        // 跳跃中检测是否着陆
        checkingJumpingFlag();
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine && PhotonNetwork.IsConnected) return;
        HandleMovement();
        HandleJump();
        ApplyGravity();
        ClampVelocity();
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        // 水平移动输入
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // 跳跃输入
        if (Input.GetButtonDown("Jump"))
        {
            jumpPressed = true;
            jumpBufferTimer = jumpBufferTime;
        }
    }

    /// <summary>
    /// 检测是否在地面
    /// </summary>
    void CheckGrounded()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);

        // 如果刚着地，重置土狼时间
        if (isGrounded && !wasGrounded)
        {
            coyoteTimer = coyoteTime;
        }
    }

    /// <summary>
    /// 处理跳跃缓冲
    /// </summary>
    void HandleJumpBuffer()
    {
        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.deltaTime;
            jumpBufferTimer = jumpBufferTimer < 0 ? 0 : jumpBufferTimer;
        }
    }

    /// <summary>
    /// 处理土狼时间
    /// </summary>
    void HandleCoyoteTime()
    {
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else if (coyoteTimer > 0)
        {
            coyoteTimer -= Time.deltaTime;
            coyoteTimer = coyoteTimer < 0 ? 0 : coyoteTimer;
        }
    }

    /// <summary>
    /// 处理水平移动
    /// </summary>
    void HandleMovement()
    {
        float targetVelocity = horizontalInput * moveSpeed;

        // 平滑加速和减速
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            // 加速
            currentVelocityX = Mathf.MoveTowards(currentVelocityX, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            // 减速
            currentVelocityX = Mathf.MoveTowards(currentVelocityX, 0, deceleration * Time.fixedDeltaTime);
        }

        // 应用水平速度
        rb.velocity = new Vector2(currentVelocityX, rb.velocity.y);


        // 根据速度向量 检测当前物体是否贴墙，如果贴墙则将摩擦设置为0
        // rb.velocity * 0.1f

        RaycastHit2D hit = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y), rb.velocity.normalized, 1f, 1 << 6);

        _collider.sharedMaterial.friction = hit ? 0f : 0.1f;
    }

    /// <summary>
    /// 处理跳跃
    /// </summary>
    void HandleJump()
    {
        // 检查是否可以跳跃（有跳跃输入缓冲且在地面或土狼时间内）
        if (jumpBufferTimer > 0 && coyoteTimer > 0 && !isJumping)
        {
            // 执行跳跃
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);

            // 清除缓冲和土狼时间
            jumpBufferTimer = 0;
            coyoteTimer = 0;

            jumpPressed = false;
            isJumping = true;
        }

        // 可变跳跃高度（松开跳跃键时减少向上速度）
        if (!Input.GetButton("Jump") && rb.velocity.y > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }
    }

    /// <summary>
    /// 应用重力
    /// </summary>
    void ApplyGravity()
    {
        if (!isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y - gravity * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// 限制速度
    /// </summary>
    void ClampVelocity()
    {
        // 限制下落速度
        if (rb.velocity.y < -maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
        }
    }

    // ===================
    // 公共接口方法
    // ===================

    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    /// <summary>
    /// 获取当前移动速度
    /// </summary>
    public float GetMoveSpeed()
    {
        return moveSpeed;
    }

    /// <summary>
    /// 设置跳跃力度
    /// </summary>
    public void SetJumpForce(float force)
    {
        jumpForce = force;
    }
    [PunRPC]
    public void RemoteJump()
    {
        ForceJump();
    }
    /// <summary>
    /// 获取是否在地面
    /// </summary>
    public bool IsGrounded()
    {
        return isGrounded;
    }

    /// <summary>
    /// 强制跳跃
    /// </summary>
    public void ForceJump()
    {
        if (isGrounded || coyoteTimer > 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    /// <summary>
    /// 获取当前速度
    /// </summary>
    public Vector2 GetVelocity()
    {
        return rb != null ? rb.velocity : Vector2.zero;
    }

    // ===================
    // 调试和可视化
    // ===================

    void OnDrawGizmosSelected()
    {
        // 绘制地面检测区域
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // 绘制移动范围指示器
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

        // 绘制速度向量
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, rb.velocity * 0.1f);
        }
    }

    void OnGUI()
    {
        if (showDebugInfo && Application.isPlaying)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 10, 200, 20), $"速度: {rb.velocity:F2}");
            GUI.Label(new Rect(10, 30, 200, 20), $"在地面: {isGrounded}");
            GUI.Label(new Rect(10, 50, 200, 20), $"土狼时间: {coyoteTimer:F2}");
            GUI.Label(new Rect(10, 70, 200, 20), $"跳跃缓冲: {jumpBufferTimer:F2}");
            GUI.Label(new Rect(10, 90, 200, 20), $"控制说明: A/D移动, 空格跳跃");
        }
    }


    private void checkingJumpingFlag()
    {
        if (isJumping)
        {
            Collider2D ground =Physics2D.OverlapCircle(_virtual_foot.transform.position, 0.1f, (1 << 6 )| (1 << 7)|(1<<8));
            if (ground)
            {
                isJumping = false;
                
                Debug.Log("解除状态");
            }

        }
    }
} 