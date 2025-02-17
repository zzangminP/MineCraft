using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;


/*
BGM - 3개의 음악클립 랜덤으로 계속 재생하게 하려고 함

SFX- 음악클립 이름에 키워드 정확하게 적어두고 키워드 2개 찾기( <예시>"Creeper" "Die" )로 상황별 클립 불러오는 
방식으로 생각중   
효과음을 딕셔너리에 저장하고 키워드를 사용하여 키워드에 맞는 효과음 가져와 재생하는 방식


[효과음이 많이 필요할 것 같아서 위의 방식과 같이 생각했고 상황별 클립 하나하나 정해서 필요시 불러와도 될것같음 ]

1. 상황별 오디오 클립을 직접 설정시 
   PlaySFX(AudioClip clip)
   <예시> 
   AudioManager.instance.PlaySFX(jumpSound);

2. 키워드에 맞는 오디오 클립을 찾아와 재생할 때
   PlayRandomSFX(string keyword1, string keyword2)
   <예시>
   AudioManager.instance.PlayRandomSFX("walk", "attack");

※키워드는 [Assets/Resources/SFX] 에서 보고 사용하면 됩니다. 
※PlayerPrefs : 유니티에서 제공하는 내장 클래스:간단한데이터를 영구적으로 저장하고 불러오는데 사용

※마스터볼륨스크롤바는 그렇지 않은데, bgm 볼륨바가 0이랑 가까워지면 소리가 작아지는데 스크롤바를 제일 왼쪽으로
했을때 소리가 다시 켜지는 오류가 있어서 볼륨 0 일시 음소거 되는 부분 추가함


★현재 안되는 부분 : 마스터 볼륨과 bgm 은 title의 스크롤바 움직임에 따라 볼륨이 조절되나 sfx는 조절안되는중ㅜㅜ★

*/
public class AudioManager : MonoBehaviour {
    public static AudioManager instance = null;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    [Range(0f, 1f)]
    public float bgmVolume = 1f;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    public AudioMixer audioMixer;

    [Header("UI Scrollbars")]
    public Scrollbar masterVolumeScrollbar;
    public Scrollbar bgmVolumeScrollbar;
    public Scrollbar sfxVolumeScrollbar;

    [Header("BGM")]
    public AudioClip[] bgmClips;
    private AudioSource bgmSource;
    private AudioSource sfxSource;

    // 키워드와 오디오 클립을 매핑할 딕셔너리
    private Dictionary<string, List<AudioClip>> sfxClips;

    [Header("Mixer Groups")]
    public AudioMixerGroup bgmMixerGroup; // BGM 오디오 믹서 그룹
    public AudioMixerGroup sfxMixerGroup; // SFX 오디오 믹서 그룹

    private void Awake() {
        if (instance == null) {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this) {
            Destroy(gameObject);
            return;
        }

        LoadSettings();

        //스크롤바 값 설정
        if (masterVolumeScrollbar != null)
            masterVolumeScrollbar.value = masterVolume;

        if (bgmVolumeScrollbar != null)
            bgmVolumeScrollbar.value = bgmVolume;

        if (sfxVolumeScrollbar != null)
            sfxVolumeScrollbar.value = sfxVolume;

        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = false; // 루프가 아니라 다음 클립을 재생하기 위해 설정
        bgmSource.outputAudioMixerGroup = bgmMixerGroup; // BGM 믹서 그룹 할당
        // AudioSource가 생성된 후 outputAudioMixerGroup 프로퍼티를 통해 sfxMixerGroup에 할당

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.outputAudioMixerGroup = sfxMixerGroup; // SFX 믹서 그룹 할당

        sfxClips = new Dictionary<string, List<AudioClip>>();

        LoadSFXClips();

        if (bgmClips.Length > 0) {
            StartCoroutine(PlayRandomBGM()); //BGM3가지중 랜덤하게 재생하는 코루틴 시작
        }
    }

    public void SetMasterVolume(float volume) { //마스터볼륨을 설정하고 PlayerPrefs에 저장
        masterVolume = volume;
        if (masterVolume <= 0.0001f) {
            AudioListener.volume = 0f; // 음소거
        }
        else {
            AudioListener.volume = masterVolume;
        }
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
    }

    public void SetBGMVolume(float volume) { //배경음악 볼륨을 설정
        bgmVolume = volume;
        if (bgmVolume <= 0.0001f) {
            audioMixer.SetFloat("BGMVolume", -80f); // 음소거
        }
        else {
            audioMixer.SetFloat("BGMVolume", Mathf.Log10(bgmVolume) * 20);
        }
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
    }

    public void SetSFXVolume(float volume) { //효과음 볼륨을 설정
        sfxVolume = volume;
        if (sfxVolume <= 0.0001f) {
            audioMixer.SetFloat("SFXVolume", -80f); // 음소거
        }
        else {
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfxVolume) * 20);
        }
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
    }

    public void LoadSettings() { //PlayerPrefs에 저장된 볼륨 설정을 불러옴
        if (PlayerPrefs.HasKey("MasterVolume")) {
            SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume"));
        }

        if (PlayerPrefs.HasKey("BGMVolume")) {
            SetBGMVolume(PlayerPrefs.GetFloat("BGMVolume"));
        }

        if (PlayerPrefs.HasKey("SFXVolume")) {
            SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume"));
        }
    }

    private IEnumerator PlayRandomBGM() { //배경음악 클립 중 하나를 랜덤하게 선택하여 재생
        while (true) {
            AudioClip clip = bgmClips[Random.Range(0, bgmClips.Length)];
            bgmSource.clip = clip;
            bgmSource.Play();
            yield return new WaitForSeconds(clip.length);
        }
    }

    public void PlaySFX(AudioClip clip) { //주어진 오디오 클립을 재생
        sfxSource.PlayOneShot(clip);
    }

    // 두 개의 키워드를 모두 포함하는 효과음 클립 중 하나를 랜덤하게 선택하여 재생
    public void PlayRandomSFX(string keyword1, string keyword2) {
        List<AudioClip> combinedClips = new List<AudioClip>();

        foreach (var kvp in sfxClips) {
            // 각 키워드를 분리하여 확인
            string[] keywords = kvp.Key.Split('-');
            if (keywords.Length == 2 && 
                ((keywords[0] == keyword1 && keywords[1] == keyword2) ||
                (keywords[0] == keyword2 && keywords[1] == keyword1))) {
                combinedClips.AddRange(kvp.Value);
            }
        }

        if (combinedClips.Count > 0) {
            AudioClip clip = combinedClips[Random.Range(0, combinedClips.Count)];
            PlaySFX(clip);
        }
    }

    private void LoadSFXClips() {
        //SFX 폴더에 있는 모든 오디오 클립을 로드합니다.
        AudioClip[] clips = Resources.LoadAll<AudioClip>("SFX");
        foreach (AudioClip clip in clips) {
            // 클립 이름을 '-'로 분리하여 각 부분을 키워드로 저장합니다.
            string[] keywords = clip.name.Split('-');
            if (keywords.Length == 2) {
                string combinedKeyword = keywords[0] + "-" + keywords[1];
                AddSFXClip(combinedKeyword, clip);
            }
            }
        }

    private void AddSFXClip(string keyword, AudioClip clip) { //특정 키워드에 해당하는 오디오 클립을 딕셔너리에 추가
            if (!sfxClips.ContainsKey(keyword)) {
                sfxClips[keyword] = new List<AudioClip>();
            }
            sfxClips[keyword].Add(clip);
        }
    }

