using UnityEngine;

/// <summary>
/// 音频控制器 - 负责处理音频输入并控制对象的缩放响应
/// </summary>
public class AudioController : MonoBehaviour
{
    [Header("音频设置")]
    public AudioSource audioSource;
    public float scaleSensitivity = 200f;  // 修改默认值为200
    public float minScale = 0.5f;
    public float maxScale = 3f;
    public float scaleSmoothing = 5f;
    
    [Header("音频采样设置")]
    public int sampleSize = 1024;
    public FFTWindow fftWindow = FFTWindow.Rectangular;
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float[] audioSpectrum;

    [Header("大中小阈值设置")]
 
    public float smallThreshold = 1.15f; // 小型上限
    public float largeThreshold = 1.7f;  // 大型下限
    private int lastSizeState = -1; // -1表示初始未判定

    public bool UI_control_flag = true;


    public bool Scale_flag = true;
    
    void Start()
    {
        // 保存原始缩放
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        // 初始化音频频谱数组
        audioSpectrum = new float[sampleSize];
        
        // 如果没有指定音频源，尝试获取组件
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        // 如果仍然没有音频源，添加一个并设置为从麦克风输入
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            SetupMicrophoneInput();
        }
    }
    
    void Update()
    {
        HandleAudioScaling();
    }
    
    /// <summary>
    /// 处理音频响应缩放
    /// </summary>
    void HandleAudioScaling()
    {
        float _scaleMultiplier = 1f;
        
        if (audioSource != null && audioSource.isPlaying && UI_control_flag)
        {
            // 获取音频频谱数据
            AudioListener.GetSpectrumData(audioSpectrum, 0, fftWindow);

            // 计算音频强度（取前几个频段的平均值）
            float audioLevel = 0f;
            int frequencyBands = Mathf.Min(64, sampleSize); // 只取前64个频段

            for (int i = 0; i < frequencyBands; i++)
            {
                audioLevel += audioSpectrum[i];
            }

            audioLevel /= frequencyBands;

            // 将音频级别转换为缩放因子
            float scaleMultiplier = 1f + (audioLevel * scaleSensitivity);
            scaleMultiplier = Mathf.Clamp(scaleMultiplier, minScale, maxScale);
            _scaleMultiplier = scaleMultiplier;

            // 设置目标缩放
            targetScale = originalScale * scaleMultiplier;

            // 体型判定
            int sizeState = 0; // 0=小，1=中，2=大
            if (scaleMultiplier <= smallThreshold)
                sizeState = 0;
            else if (scaleMultiplier >= largeThreshold)
                sizeState = 2;
            else
                sizeState = 1;

            // 状态变化时通过事件中心发送事件
            if (sizeState != lastSizeState)
            {
                EventCenter.GetInstance().EventTrigger<int>("PlayerSizeChanged", sizeState);
                lastSizeState = sizeState;
            }
        }
        else
        {
            // 如果没有音频播放，恢复到原始大小
            targetScale = originalScale;
            // 可选：无音频时重置状态
            if (lastSizeState != 0)
            {
                EventCenter.GetInstance().EventTrigger<int>("PlayerSizeChanged", 0);
                lastSizeState = 0;
            }
        }

        // 平滑过渡到目标缩放
        if (Scale_flag || (!Scale_flag && _scaleMultiplier < 1f))
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSmoothing * Time.deltaTime);

    }
    
    /// <summary>
    /// 设置麦克风输入
    /// </summary>
    void SetupMicrophoneInput()
    {
        // 检查是否有可用的麦克风
        if (Microphone.devices.Length > 0)
        {
            // 使用默认麦克风
            string microphoneName = Microphone.devices[0];
            
            // 创建音频片段从麦克风录制
            audioSource.clip = Microphone.Start(microphoneName, true, 1, AudioSettings.outputSampleRate);
            audioSource.loop = true;
            
            // 等待麦克风开始录制
            while (!(Microphone.GetPosition(microphoneName) > 0)) { }
            
            // 播放麦克风输入
            audioSource.Play();
            
            Debug.Log($"已连接麦克风: {microphoneName}");
        }
        else
        {
            Debug.LogWarning("未检测到可用的麦克风设备！");
        }
    }
    
    /// <summary>
    /// 设置音频源
    /// </summary>
    public void SetAudioSource(AudioSource newAudioSource)
    {
        audioSource = newAudioSource;
    }
    
    /// <summary>
    /// 获取当前音频级别（用于调试）
    /// </summary>
    public float GetCurrentAudioLevel()
    {
        if (audioSpectrum == null) return 0f;
        
        float level = 0f;
        int frequencyBands = Mathf.Min(64, sampleSize);
        
        for (int i = 0; i < frequencyBands; i++)
        {
            level += audioSpectrum[i];
        }
        
        return level / frequencyBands;
    }
    
    /// <summary>
    /// 设置缩放敏感度
    /// </summary>
    public void SetScaleSensitivity(float sensitivity)
    {
        scaleSensitivity = sensitivity;
    }
    
    /// <summary>
    /// 重置到原始大小
    /// </summary>
    public void ResetScale()
    {
        targetScale = originalScale;
        transform.localScale = originalScale;
    }
    
    // 在Inspector中显示缩放范围指示器
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && originalScale != Vector3.zero)
        {
            // 绘制缩放范围指示器
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, originalScale * minScale);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, originalScale * maxScale);
        }
    }
} 