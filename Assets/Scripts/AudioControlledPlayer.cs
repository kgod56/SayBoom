using System;
using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;


/// <summary>
/// 音频控制玩家 - 整合了操作控制和音频控制模块
/// 现在使用分离的模块化设计，支持马里奥式跳跃系统
/// </summary>
public class AudioControlledPlayer : MonoBehaviour
{
    [Header("模块组件")]
    [Tooltip("玩家操作控制器")]
    public PlayerController playerController;

    [Tooltip("音频控制器")]
    public AudioController audioController;

    [Header("快速设置")]
    [Tooltip("如果为true，将自动添加缺失的组件")]
    public bool autoSetupComponents = true;

    [Tooltip("如果为true，将自动添加必要的物理组件")]
    public bool autoSetupPhysics = true;

    public Rigidbody2D rigidbody;

    [Header("物理特性")]
    public float gravity;

   
    private bool becomeBig;//是否正在变大
    void Awake()
    {
        if (autoSetupComponents)
        {
            SetupComponents();
        }

        if (autoSetupPhysics)
        {
            SetupPhysicsComponents();
        }
    }

    void Start()
    {
        // 验证组件
        ValidateComponents();


        EventCenter.GetInstance().AddEventListener<int>("PlayerSizeChanged", OnPlayerSizeChanged);
        EventCenter.GetInstance().AddEventListener<int>("PlayerSizeChanged", CheckForceJump);
    }

    private void OnPlayerSizeChanged(int arg0)
    {   
        playerController.Size = arg0;
       
            
    }
     
    private Dictionary<GameObject, float> jumpCooldowns = new Dictionary<GameObject, float>();
    
    void LetOtherJump(GameObject target)
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("NetworkManager.Instance 为空，请确保场景中有 NetworkManager！");
            return;
        }
        PhotonView pv = target.GetComponent<PhotonView>();
        if (pv != null)
        {
            pv.RPC("RemoteJump", pv.Owner);
        }
    }
    private void CheckForceJump(int arg0)
    {
        if (playerController == null || playerController._virtual_head == null) return;
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerController._virtual_head.position, 0.5f, 1 << 8);
        float now = Time.time;
        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;
            if (!jumpCooldowns.ContainsKey(hit.gameObject) || now - jumpCooldowns[hit.gameObject] > 0.5f)
            {
                LetOtherJump(hit.gameObject);
                jumpCooldowns[hit.gameObject] = now;
            }
        }
    }
    void OnDestroy()
    {
        // 取消订阅，防止内存泄漏
        EventCenter.GetInstance().RemoveEventListener<int>("PlayerSizeChanged", OnPlayerSizeChanged);
        EventCenter.GetInstance().RemoveEventListener<int>("PlayerSizeChanged", CheckForceJump);
    }

    /// <summary>
    /// 自动设置组件
    /// </summary>
    void SetupComponents()
    {
        // 设置玩家控制器
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = gameObject.AddComponent<PlayerController>();
            }
        }

        // 设置音频控制器
        if (audioController == null)
        {
            audioController = GetComponent<AudioController>();
            if (audioController == null)
            {
                audioController = gameObject.AddComponent<AudioController>();
            }
        }
    }

    /// <summary>
    /// 自动设置物理组件
    /// </summary>
    void SetupPhysicsComponents()
    {
        // 确保有Rigidbody2D组件（PlayerController会自动添加）
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        // 确保有Collider2D组件
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            // 添加BoxCollider2D作为默认碰撞器
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            Debug.Log("已自动添加BoxCollider2D组件");
        }
    }

    /// <summary>
    /// 验证组件是否正确设置
    /// </summary>
    void ValidateComponents()
    {
        if (playerController == null)
        {
            Debug.LogWarning("PlayerController组件未找到！玩家将无法移动。");
        }

        if (audioController == null)
        {
            Debug.LogWarning("AudioController组件未找到！音频响应将不工作。");
        }

        // 验证物理组件
        if (GetComponent<Rigidbody2D>() == null)
        {
            Debug.LogWarning("Rigidbody2D组件未找到！跳跃系统可能无法正常工作。");
        }

        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning("Collider2D组件未找到！碰撞检测可能无法正常工作。");
        }

        if (playerController != null && audioController != null)
        {
            Debug.Log("音频控制玩家初始化完成！可以使用A/D移动，空格跳跃，对着麦克风说话控制缩放。");
        }

        rigidbody = transform.GetComponent<Rigidbody2D>();
        rigidbody.gravityScale = gravity;
    }

    // ===================
    // 移动控制接口
    // ===================

    /// <summary>
    /// 设置移动速度
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        if (playerController != null)
        {
            playerController.SetMoveSpeed(speed);
        }
    }

    /// <summary>
    /// 获取移动速度
    /// </summary>
    public float GetMoveSpeed()
    {
        return playerController != null ? playerController.GetMoveSpeed() : 0f;
    }

    /// <summary>
    /// 设置跳跃力度
    /// </summary>
    public void SetJumpForce(float force)
    {
        if (playerController != null)
        {
            playerController.SetJumpForce(force);
        }
    }

    /// <summary>
    /// 获取是否在地面
    /// </summary>
    public bool IsGrounded()
    {
        return playerController != null ? playerController.IsGrounded() : false;
    }

    /// <summary>
    /// 强制跳跃
    /// </summary>
    public void ForceJump()
    {
        if (playerController != null)
        {
            playerController.ForceJump();
        }
    }

    /// <summary>
    /// 获取当前移动速度
    /// </summary>
    public Vector2 GetVelocity()
    {
        return playerController != null ? playerController.GetVelocity() : Vector2.zero;
    }

    // ===================
    // 音频控制接口
    // ===================

    /// <summary>
    /// 设置音频源
    /// </summary>
    public void SetAudioSource(AudioSource audioSource)
    {
        if (audioController != null)
        {
            audioController.SetAudioSource(audioSource);
        }
    }

    /// <summary>
    /// 设置缩放敏感度
    /// </summary>
    public void SetScaleSensitivity(float sensitivity)
    {
        if (audioController != null)
        {
            audioController.SetScaleSensitivity(sensitivity);
        }
    }

    /// <summary>
    /// 获取当前音频级别
    /// </summary>
    public float GetCurrentAudioLevel()
    {
        return audioController != null ? audioController.GetCurrentAudioLevel() : 0f;
    }

    /// <summary>
    /// 重置缩放到原始大小
    /// </summary>
    public void ResetScale()
    {
        if (audioController != null)
        {
            audioController.ResetScale();
        }
    }

    // ===================
    // 模块控制接口
    // ===================

    /// <summary>
    /// 启用/禁用玩家控制
    /// </summary>
    public void SetPlayerControlEnabled(bool enabled)
    {
        if (playerController != null)
        {
            playerController.enabled = enabled;
        }
    }

    /// <summary>
    /// 启用/禁用音频控制
    /// </summary>
    public void SetAudioControlEnabled(bool enabled)
    {
        if (audioController != null)
        {
            audioController.enabled = enabled;
        }
    }

    /// <summary>
    /// 启用/禁用物理
    /// </summary>
    public void SetPhysicsEnabled(bool enabled)
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = enabled;
        }
    }

    // ===================
    // 特殊功能接口
    // ===================

    /// <summary>
    /// 音频触发跳跃（当音频达到某个阈值时自动跳跃）
    /// </summary>
    public void EnableAudioJump(float audioThreshold = 0.1f)
    {
        if (audioController != null && playerController != null)
        {
            // 这里可以添加一个协程来监控音频级别并触发跳跃
            StartCoroutine(AudioJumpCoroutine(audioThreshold));
        }
    }

    /// <summary>
    /// 音频跳跃协程
    /// </summary>
    private System.Collections.IEnumerator AudioJumpCoroutine(float threshold)
    {
        while (true)
        {
            if (audioController.GetCurrentAudioLevel() > threshold && playerController.IsGrounded())
            {
                playerController.ForceJump();
                yield return new WaitForSeconds(0.5f); // 防止连续跳跃
            }
            yield return new WaitForFixedUpdate();
        }
    }

    // ===================
    // 调试和可视化
    // ===================

    void OnDrawGizmosSelected()
    {
        // 绘制组件状态指示器
        Gizmos.color = Color.white;
        Vector3 pos = transform.position + Vector3.up * 2f;

        // 玩家控制器指示器（蓝色）
        if (playerController != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(pos + Vector3.left * 0.5f, 0.1f);
        }

        // 音频控制器指示器（绿色）
        if (audioController != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(pos + Vector3.right * 0.5f, 0.1f);
        }

        // 物理组件指示器（黄色）
        if (GetComponent<Rigidbody2D>() != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(pos + Vector3.back * 0.5f, 0.1f);
        }

        // 碰撞器指示器（红色）
        if (GetComponent<Collider2D>() != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(pos + Vector3.forward * 0.5f, 0.1f);
        }
    }
}