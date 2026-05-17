using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))] // 确保物体上一定有 AudioSource 组件
public class SoundPlayTime : MonoBehaviour
{

    public bool playOnAwake = false;
    public bool loopCheck = false;       // 是否让脚本逻辑重复触发播放
    public bool audioLoop = false;       // 音频文件本身是否循环

    public float waitTime = 1.0f;        // 第一次播放前的等待时间
    public float loopWaitTime = 0.0f;    // 循环播放的间隔时间

    public AudioClip sound01;

    private float timer;
    private float loopTime = 0.0f;
    private bool hasPlayedOnce = false;  // 代替原本的 flag，逻辑更清晰
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // 同步 Unity 组件的状态
        audioSource.playOnAwake = playOnAwake;
        audioSource.loop = audioLoop; // 显式控制组件的循环状态
        audioSource.clip = sound01;

        if (audioSource.playOnAwake)
        {
            audioSource.Play();
            hasPlayedOnce = true;

            // 如果不需要循环，且已经播放了，直接把这个控制脚本删掉
            if (!loopCheck)
            {
                Destroy(this);
            }
        }

        loopTime = loopWaitTime;
    }

    void Update()
    {
        // 如果不需要循环，且已经播放过一次了，直接销毁脚本，不再执行后续逻辑
        if (!loopCheck && hasPlayedOnce)
        {
            Destroy(this);
            return;
        }

        timer += Time.deltaTime;

        // 第一次播放的倒计时
        if (!hasPlayedOnce)
        {
            if (timer >= waitTime)
            {
                PlayAudio();
                hasPlayedOnce = true;
                timer = 0; // 重置计时器给循环用
            }
        }
        // 之后的循环播放倒计时
        else if (loopCheck)
        {
            loopTime -= Time.deltaTime;
            if (loopTime <= 0)
            {
                PlayAudio();
                loopTime = loopWaitTime; // 重置循环间隔
            }
        }
    }

    void PlayAudio()
    {
        if (sound01 != null)
        {
            audioSource.clip = sound01;
            audioSource.Play();
        }
    }
}