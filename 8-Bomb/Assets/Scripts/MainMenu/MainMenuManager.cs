using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("References")]
    public GameObject ResolutionSettingsObj;
    public TMP_Dropdown ResolutionDropdown;
    public Toggle FullScreenToggle;
    public TMP_Dropdown GraphicDropdown;
    public Slider MusicValueSlider;
    public Slider SoundEffectValueSlider;
    public AudioMixer audioMixer;
    public GameObject QuitButtonObj;
    
    [Header("Setting")]
    [SerializeField] string nextSceneName = "GameScene";

    Resolution[] resolutions;

    void Awake()
    {
#if UNITY_STANDALONE
        bool fullscreenStatus = PlayerPrefs.GetInt("FullScreen", 1) == 1;
        Screen.SetResolution(PlayerPrefs.GetInt("ResolutionWidth", 1920), PlayerPrefs.GetInt("ResolutionHeight", 1080), fullscreenStatus);
#endif
        QualitySettings.SetQualityLevel(PlayerPrefs.GetInt("Graphic", 2)); // '2' is the "High" level
        audioMixer.SetFloat("MusicVolume", PlayerPrefs.GetFloat("MusicVolume", 0));
        audioMixer.SetFloat("SoundEffectVolume", PlayerPrefs.GetFloat("SoundEffectVolume", 0));
    }

    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;

#if UNITY_WEBGL || UNITY_IOS || UNITY_ANDROID
        // 隱藏退出按鈕與解析度設定
        if (QuitButtonObj != null) QuitButtonObj.SetActive(false);
        if (ResolutionSettingsObj != null) ResolutionSettingsObj.SetActive(false);
#else
        // PC 平台：處理解析度選單
        SetupResolutionDropdown();
#endif

        GraphicDropdown.value = QualitySettings.GetQualityLevel();
        GraphicDropdown.RefreshShownValue();

        bool musicVolLevelResult = audioMixer.GetFloat("MusicVolume", out float musicVolLevel);
        if (musicVolLevelResult) MusicValueSlider.value = musicVolLevel;

        bool soundEffectVolLevelResult = audioMixer.GetFloat("SoundEffectVolume", out float soundEffectVolLevel);
        if (soundEffectVolLevelResult) SoundEffectValueSlider.value = soundEffectVolLevel;
    }

    private void SetupResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        ResolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + "x" + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && 
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        ResolutionDropdown.AddOptions(options);
        ResolutionDropdown.value = currentResolutionIndex;
        ResolutionDropdown.RefreshShownValue();
        FullScreenToggle.isOn = Screen.fullScreen;
    }

    public void PlayButton()
    {
        SceneManager.Instance.PlayTransitionAnimation(true);
        SceneManager.Instance.LoadToScene(nextSceneName);
    }

    public void QuitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
        Application.Quit();
#endif
    }

    public void SetMusicVolume (float volume)
    {
        audioMixer.SetFloat("MusicVolume", volume);
    }

    public void SetSoundEffectVolume (float volume)
    {
        audioMixer.SetFloat("SoundEffectVolume", volume);
    }

    public void SetQuality (int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetFullScreen (bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }

    public void SetResolution (int resolutionindex)
    {
        Resolution resolution = resolutions[resolutionindex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SaveOptionPref()
    {
        PlayerPrefs.SetInt("ResolutionWidth", Screen.currentResolution.width);
        PlayerPrefs.SetInt("ResolutionHeight", Screen.currentResolution.height);

        int fullscreenStatus = Screen.fullScreen ? 1 : 0;
        PlayerPrefs.SetInt("FullScreen", fullscreenStatus);

        PlayerPrefs.SetInt("Graphic", QualitySettings.GetQualityLevel());

        bool musicVolLevelResult = audioMixer.GetFloat("MusicVolume", out float musicVolLevel);
        if (musicVolLevelResult) PlayerPrefs.SetFloat("MusicVolume", musicVolLevel);

        bool soundEffectVolLevelResult = audioMixer.GetFloat("SoundEffectVolume", out float soundEffectVolLevel);
        if (soundEffectVolLevelResult) PlayerPrefs.SetFloat("SoundEffectVolume", soundEffectVolLevel);
    }
}
