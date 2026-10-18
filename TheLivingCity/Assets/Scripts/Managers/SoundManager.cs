using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : SingletonMonoBehaviour<SoundManager>
{
    [System.Serializable]
    private class SoundData
    {
        public SoundData(AudioClip audioClip)
        {
            this.audioClip = audioClip;
            playedTime = Time.realtimeSinceStartup;
        }

        public AudioClip audioClip;
        public float playedTime; //再生時間
    }

    //一度再生してから、次再生出来るまでの間隔(秒)
    private float INTERVAL = 0.2f;

    //AudioSource（スピーカー）を同時に鳴らしたい音の数だけ用意
    private AudioSource[] _seSources = new AudioSource[20];
    private AudioSource[] _bgmSources = new AudioSource[3];

    [SerializeField] private AudioClip[] seClips;
    [SerializeField] private AudioClip[] bgmClips;

    private Dictionary<string, SoundData> _seData = new Dictionary<string, SoundData>();
    private Dictionary<string, SoundData> _bgmData = new Dictionary<string, SoundData>();

    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup masterGroup;
    [SerializeField] private AudioMixerGroup bgmGroup;
    [SerializeField] private AudioMixerGroup seGroup;

    private bool _initialized = false;

    private void Awake()
    {
        Init();

        if (_initialized) return;
        _initialized = true;

        for (int i = 0; i < _seSources.Length; i++)
        {
            _seSources[i] = gameObject.AddComponent<AudioSource>();
            _seSources[i].outputAudioMixerGroup = seGroup;
        }

        for (int i = 0; i < _bgmSources.Length; i++)
        {
            _bgmSources[i] = gameObject.AddComponent<AudioSource>();
            _bgmSources[i].outputAudioMixerGroup = bgmGroup;
            _bgmSources[i].loop = true;
        }

        foreach (var seClip in seClips)
        {
            var soundData = new SoundData(seClip);
            _seData.Add(seClip.name, soundData);
        }

        foreach (var bgmClip in bgmClips)
        {
            var soundData = new SoundData(bgmClip);
            _bgmData.Add(bgmClip.name, soundData);
        }
    }
    private void Start()
    {
        SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (value <= 0)
        {
            audioMixer.SetFloat("MasterVolume", -80f);
        }
        else
        {
            audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
        }

        PlayerPrefs.SetFloat("MasterVolume", value);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 未使用のAudioSource(SE)を検索し、取得する関数
    /// </summary>
    /// <returns>未使用のAudioSource(SE)。全て使用中の場合はnullを返却</returns>
    private AudioSource GetUnusedSourceSE()
    {
        for (var i = 0; i < _seSources.Length; ++i)
        {
            if (!_seSources[i].isPlaying) return _seSources[i];
        }

        return null;
    }

    /// <summary>
    /// 未使用のAudioSource(BGM)を検索し、取得する関数
    /// </summary>
    /// <returns>未使用のAudioSource(BGM)。全て使用中の場合はnullを返却</returns>
    private AudioSource GetUnusedSourceBGM()
    {
        for (var i = 0; i < _bgmSources.Length; ++i)
        {
            if (!_bgmSources[i].isPlaying) return _bgmSources[i];
        }

        return null;
    }

    /// <summary>
    /// 名前で指定したSEを再生する関数
    /// </summary>
    /// <param seName="seName">SE名</param>
    public void PlaySE(string seName)
    {
        if (_seData.ContainsKey(seName))
        {
            if (Time.realtimeSinceStartup - _seData[seName].playedTime > INTERVAL)
            {
                var audioSource = GetUnusedSourceSE();

                if (audioSource)
                {
                    audioSource.clip = _seData[seName].audioClip;
                    audioSource.Play();
                    _seData[seName].playedTime = Time.realtimeSinceStartup;
                }
            }
        }
    }

    public void PlayAudioClip(AudioClip clip)
    {
        var audioSource = GetUnusedSourceSE();

        if (audioSource)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    /// <summary>
    /// 名前で指定したBGMを再生する関数
    /// </summary>
    /// <param bgmName="bgmName">BGM名</param>
    public void PlayBGM(string bgmName)
    {
        if (_bgmData.ContainsKey(bgmName))
        {
            if (Time.realtimeSinceStartup - _bgmData[bgmName].playedTime > INTERVAL)
            {
                var audioSource = GetUnusedSourceBGM();

                if (audioSource)
                {
                    audioSource.clip = _bgmData[bgmName].audioClip;
                    audioSource.Play();
                    _bgmData[bgmName].playedTime = Time.realtimeSinceStartup;
                }
            }
        }
    }

    /// <summary>
    /// BGMを停止する関数
    /// </summary>
    public void StopBGM(string bgmName)
    {
        foreach (var bgmSource in _bgmSources)
        {
            if (bgmSource.clip && bgmSource.clip.name == bgmName)
            {
                bgmSource.Stop();
            }
        }
    }

    /// <summary>
    /// 流れているBGMを止めて、指定したBGMに切り替える関数
    /// </summary>
    /// <param name="bgmClip">BGM。nullのときは止めるだけ</param>
    public void ChangeBGM(AudioClip bgmClip)
    {
        foreach (var bgmSource in _bgmSources)
        {
            //同じBGMが流れているときはそのまま
            if (bgmClip && bgmSource.isPlaying && bgmSource.clip == bgmClip) return;
        }

        foreach (var bgmSource in _bgmSources)
        {
            bgmSource.Stop();
        }

        if (!bgmClip) return;

        _bgmSources[0].clip = bgmClip;
        _bgmSources[0].Play();
    }
}