using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频管理器 - 支持单次播放 & 循环播放
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频源设置")]
    [SerializeField] private AudioSource _musicSource;      // 背景音乐

    [Header("背景音乐")]
    [SerializeField] private AudioClip _buildingBGM;        // 建设阶段BGM
    [SerializeField] private AudioClip _combatBGM;          // 战斗阶段BGM

    [Header("游戏流程音效")]
    [SerializeField] private AudioClip _victorySound;       // 游戏胜利
    [SerializeField] private AudioClip _gameOverSound;      // 游戏失败

    [Header("UI音效")]
    [SerializeField] private AudioClip _buttonClickSound;   // 按键点击

    [Header("战斗系统音效")]
    [SerializeField] private AudioClip _attackSound;        // 攻击音效（循环）
    [SerializeField] private AudioClip _damageSound;        // 塔受击音效（循环）
    [SerializeField] private AudioClip _brokenSound;        // 塔摧毁音效（单次）
    [SerializeField] private AudioClip _footstepSound;      // 敌人行走音效（循环）

    [Header("建造系统音效")]
    [SerializeField] private AudioClip _buildSound;         // 塔建造音效
    [SerializeField] private AudioClip _levelUpSound;       // 塔升级音效
    [SerializeField] private AudioClip _replaceSound;       // 塔替换音效

    [Header("音量设置")]
    [Range(0f, 1f)] public float MasterVolume = 1f;
    [Range(0f, 1f)] public float MusicVolume = 0.7f;
    [Range(0f, 1f)] public float SfxVolume = 1f;
    
    // 上次音量值，用于检测变化
    private float _lastMasterVolume = 1f;
    private float _lastMusicVolume = 0.7f;
    private float _lastSfxVolume = 1f;

    // 音频库字典
    private Dictionary<SoundType, AudioClip> _soundLibrary;

    // 正在循环的音效
    private Dictionary<SoundType, AudioSource> _loopingSources = new Dictionary<SoundType, AudioSource>();
    
    // 当前播放的BGM类型
    private GamePhase _currentBGMPhase = GamePhase.BuildingPhase;

    // 音效类型枚举
    public enum SoundType
    {
        // 游戏流程
        Victory,
        GameOver,

        // UI
        ButtonClick,

        // 战斗系统
        Attack,   // 循环
        Damage,   // 循环
        Broken,   // 单次
        Footstep, // 循环

        // 建造系统
        Build,
        LevelUp,
        Replace
    }

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudioManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioManager()
    {
        CreateAudioSources();
        InitializeSoundLibrary();
        UpdateAllVolumes();
        Debug.Log("音频管理器初始化完成");
    }

    private void CreateAudioSources()
    {
        if (_musicSource == null)
        {
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            _musicSource = musicObj.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        }
    }

    private void InitializeSoundLibrary()
    {
        _soundLibrary = new Dictionary<SoundType, AudioClip>
        {
            { SoundType.Victory, _victorySound },
            { SoundType.GameOver, _gameOverSound },

            { SoundType.ButtonClick, _buttonClickSound },

            { SoundType.Attack, _attackSound },
            { SoundType.Damage, _damageSound },
            { SoundType.Broken, _brokenSound },
            { SoundType.Footstep, _footstepSound },

            { SoundType.Build, _buildSound },
            { SoundType.LevelUp, _levelUpSound },
            { SoundType.Replace, _replaceSound }
        };

        CheckMissingSounds();
    }

    private void CheckMissingSounds()
    {
        foreach (var sound in _soundLibrary)
        {
            if (sound.Value == null)
            {
                Debug.LogWarning($"音效缺失: {sound.Key}");
            }
        }
    }

    #region 单次播放
    private void CreateTemporaryAudioSource(AudioClip clip)
    {
        GameObject tempAudioObject = new GameObject("TempAudioSource");
        tempAudioObject.transform.SetParent(transform);
        AudioSource tempSource = tempAudioObject.AddComponent<AudioSource>();

        tempSource.clip = clip;
        tempSource.volume = SfxVolume * MasterVolume;
        tempSource.loop = false;
        tempSource.Play();

        Destroy(tempAudioObject, clip.length + 0.1f);
    }

    public void PlaySound(SoundType soundType)
    {
        if (!_soundLibrary.ContainsKey(soundType) || _soundLibrary[soundType] == null)
        {
            Debug.LogWarning($"音效未配置: {soundType}");
            return;
        }

        AudioClip clip = _soundLibrary[soundType];
        CreateTemporaryAudioSource(clip);
    }
    #endregion

    #region 循环播放
    public void PlayLoopingSound(SoundType soundType, GameObject owner = null)
    {
        if (!_soundLibrary.ContainsKey(soundType) || _soundLibrary[soundType] == null)
        {
            Debug.LogWarning($"音效未配置: {soundType}");
            return;
        }

        if (_loopingSources.ContainsKey(soundType)) return; // 已经在播放

        GameObject loopObj = new GameObject($"{soundType}_LoopSource");
        if (owner != null) loopObj.transform.SetParent(owner.transform);
        else loopObj.transform.SetParent(transform);

        AudioSource loopSource = loopObj.AddComponent<AudioSource>();
        loopSource.clip = _soundLibrary[soundType];
        loopSource.volume = SfxVolume * MasterVolume;
        loopSource.loop = true;
        loopSource.Play();

        _loopingSources[soundType] = loopSource;
    }

    public void StopLoopingSound(SoundType soundType)
    {
        if (_loopingSources.ContainsKey(soundType))
        {
            AudioSource source = _loopingSources[soundType];
            if (source != null) Destroy(source.gameObject);
            _loopingSources.Remove(soundType);
        }
    }
    #endregion

    #region 背景音乐
    public void PlayMusic(AudioClip musicClip)
    {
        if (_musicSource == null || musicClip == null) return;

        _musicSource.clip = musicClip;
        _musicSource.Play();
    }

    public void StopMusic()
    {
        if (_musicSource != null) _musicSource.Stop();
    }

    public void PauseMusic()
    {
        if (_musicSource != null) _musicSource.Pause();
    }

    public void ResumeMusic()
    {
        if (_musicSource != null) _musicSource.UnPause();
    }
    
    /// <summary>
    /// 根据游戏阶段播放对应的BGM
    /// </summary>
    /// <param name="gamePhase">游戏阶段</param>
    public void PlayBGMForPhase(GamePhase gamePhase)
    {
        // 如果当前已经播放对应阶段的BGM，则不重复播放
        if (_currentBGMPhase == gamePhase) return;
        
        _currentBGMPhase = gamePhase;
        
        switch (gamePhase)
        {
            case GamePhase.BuildingPhase:
            case GamePhase.PassPhase:
                if (_buildingBGM != null)
                {
                    PlayMusic(_buildingBGM);
                }
                break;
            case GamePhase.CombatPhase:
                if (_combatBGM != null)
                {
                    PlayMusic(_combatBGM);
                }
                break;
            case GamePhase.VictoryPhase:
                StopMusic();
                PlayVictorySound();
                break;
            case GamePhase.DefeatPhase:
                StopMusic();
                PlayGameOverSound();
                break;
        }
    }
    #endregion

    #region 便捷方法
    // 一次性播放
    public void PlayVictorySound() { PlaySound(SoundType.Victory); }
    public void PlayGameOverSound() { PlaySound(SoundType.GameOver); }
    public void PlayButtonClickSound() { PlaySound(SoundType.ButtonClick); }
    public void PlayBrokenSound() { PlaySound(SoundType.Broken); }
    public void PlayBuildSound() { PlaySound(SoundType.Build); }
    public void PlayLevelUpSound() { PlaySound(SoundType.LevelUp); }
    public void PlayReplaceSound() { PlaySound(SoundType.Replace); }
    public void PlayAttackSound() { PlaySound(SoundType.Attack); }
    public void PlayDamageSound() { PlaySound(SoundType.Damage); }

    // 循环播放（手动停止）
    public void PlayAttackSound(GameObject owner) { PlayLoopingSound(SoundType.Attack, owner); }
    public void StopAttackSound() { StopLoopingSound(SoundType.Attack); }

    public void PlayDamageSound(GameObject owner) { PlayLoopingSound(SoundType.Damage, owner); }
    public void StopDamageSound() { StopLoopingSound(SoundType.Damage); }

    public void PlayFootstepSound(GameObject owner) { PlayLoopingSound(SoundType.Footstep, owner); }
    public void StopFootstepSound() { StopLoopingSound(SoundType.Footstep); }
    #endregion

    #region 音量控制
    public void SetMasterVolume(float volume)
    {
        MasterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Mathf.Clamp01(volume);
    }

    private void UpdateAllVolumes()
    {
        if (_musicSource != null)
            _musicSource.volume = MusicVolume * MasterVolume;

        foreach (var kvp in _loopingSources)
        {
            if (kvp.Value != null)
                kvp.Value.volume = SfxVolume * MasterVolume;
        }
    }
    #endregion

    private void Update()
    {
        // 检查音量是否发生变化，如果变化则更新所有音量
        if (_lastMasterVolume != MasterVolume || 
            _lastMusicVolume != MusicVolume || 
            _lastSfxVolume != SfxVolume)
        {
            _lastMasterVolume = MasterVolume;
            _lastMusicVolume = MusicVolume;
            _lastSfxVolume = SfxVolume;
            UpdateAllVolumes();
        }
    }
}