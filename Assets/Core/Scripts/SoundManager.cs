using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private float initBGMVolume = 0.7f;
    [SerializeField] private float initSFXVolume = 0.7f;
    [SerializeField] private EventInstance BGM;
    [SerializeField] private EventInstance SFX;
    private Bus masterBus;
    private VCA BGMVCA;
    private VCA SFXVCA;
    private string currentBGM = "";

    private static SoundManager instance = null;
    public static SoundManager Instance
    {
        get
        {
            if(instance==null) return null;
            return instance;
        }
    }
    void Awake()
    {
        if(null==instance)
        {
            instance = this;
            DontDestroyOnLoad(Instance);
        }
        else Destroy(this.gameObject);
    }
    void Start()
    {
        currentBGM="";
        InitVolume();
    }
    /// <summary>
    /// FMOD 프로젝트 내 BGM 뱅크에서 경로로 BGM을 찾아 플레이함.
    /// 각 BGM마다 BGM 이름 + State 형태의 Parameter를 만들어 놓음. (ex : MainState)
    /// 변수가 바뀔 때마다 같은 BGM 내에서 음악의 상태가 변경됨.
    /// </summary>
    /// <param name="name">음악의 제목</param>
    /// <param name="state">BGM State 값, 기본값 0</param>
    public void PlayBGM(string name, int state = 0)
    {
        if(currentBGM != name)
        {
            BGM.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            BGM.release();
            string path = $"event:/BGM/{name}";
            BGM = RuntimeManager.CreateInstance(path);
            BGM.start();
            currentBGM = name;
        }
        string param = $"{name}State";
        BGM.setParameterByName(param, state);
    }

    public void StopBGM()
    {
        if(BGM.isValid())
        {
            BGM.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            BGM.release();
            BGM.clearHandle();
            currentBGM = "";
        }
        else Debug.LogWarning("FMOD Error: BGM Stop failed!");
    }

    public void PauseBGM()
    {
        BGM.setPaused(true);
    }

    public void ResumeBGM()
    {
        BGM.setPaused(false);
    }
    /// <summary>
    /// SFX를 재생함.
    /// </summary>
    /// <param name="name">재생할 SFX 이름.
    /// Sounds 폴더의 .fspro 파일에서 찾을 수 있음.</param>
    public void PlaySFX(string name)
    {
        var instance = RuntimeManager.CreateInstance("event:/SFX/" + name);
        instance.start();
        instance.release();
    }

    /// <summary>
    /// SFX 루프 재생 기능. 안 쓰면 말고.
    /// </summary>
    private Dictionary<string, EventInstance> loopsPlaying = new();
    public void LoopSFX(string name)
    {
        if(loopsPlaying.ContainsKey(name)) return;

        var instance = RuntimeManager.CreateInstance("event:/SFX/" + name);
        instance.start();
        loopsPlaying[name] = instance;
    }
    public void StopLoop(string name)
    {
        if(!loopsPlaying.ContainsKey(name)) return;
        EventInstance instance = loopsPlaying[name];
        instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        instance.release();
        loopsPlaying.Remove(name);
    }
    public void SetMasterVolume(float volume)
    {
        masterBus.setVolume(volume);
    }
    public void SetBGMVolume(float volume)
    {
        BGMVCA.setVolume(volume);
    }
    public float GetBGMVolume()
    {
        float volume = 0f;
        BGMVCA.getVolume(out volume);
        return volume;
    }
    public void SetSFXVolume(float volume)
    {
        SFXVCA.setVolume(volume);
    }
    public float GetSFXVolume()
    {
        float volume = 0f;
        SFXVCA.getVolume(out volume);
        return volume;
    }
    public void InitVolume()
    {
        masterBus = RuntimeManager.GetBus("bus:/");
        BGMVCA = RuntimeManager.GetVCA("vca:/BGM");
        SFXVCA = RuntimeManager.GetVCA("vca:/SFX");

        SetMasterVolume(1f);
        SetBGMVolume(initBGMVolume);
        SetSFXVolume(initSFXVolume);
    }
    public void Mute()
    {
        SetMasterVolume(0f);
    }
}
