using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System;
using UnityEngine.Events;
using UnityEngine.Playables;
public class GlobalManager : MonoBehaviour
{
    public static GlobalManager Instance { get; private set; }
    SaveFileHandler handler;
    LocalSaveFileHandler localHandler;
    ServerSaveFileHandler serverHandler;

    [SerializeField] RectTransform sceneLoadBlack;
    [SerializeField] Text sceneLoadingText;
    [SerializeField] Image brightnessImage;

    [Header("Save Cache")]
    [SerializeField] List<ItemData> m_itemDatas;
    [SerializeField] List<PlayableAsset> m_cutscenes;
    [SerializeField] List<Enemy> m_enemyPrefabs;
    public List<ItemData> itemDatas => m_itemDatas;
    public List<PlayableAsset> cutscenes => m_cutscenes;
    public List<Enemy> enemyPrefabs => m_enemyPrefabs;

    [Header("Saving")]
    [SerializeField] bool prettyPrint = false;
    [SerializeField] int m_fileCount = 10;
    [SerializeField] SaveHandlerMode m_saveMode = SaveHandlerMode.Local;
    public SaveHandlerMode saveMode => m_saveMode;

    [Header("LocalSave")]
    [SerializeField] string fileExtension;
    [SerializeField] string saveRoute;

    [Header("ServerSave")]
    [SerializeField] string gameID = "DadsLetter";
    [SerializeField] string baseURL = "survivalgameserver.azurewebsites.net";
    public int fileCount => m_fileCount;
    SaveData data;
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        localHandler = new LocalSaveFileHandler(saveRoute, fileExtension);
        serverHandler = new ServerSaveFileHandler(gameID, baseURL, this);
        serverUsername = PlayerPrefs.GetString("ServerUsername", string.Empty);
        SwitchHandler(saveMode);
        Settings.onBrightnessChange += () => brightnessImage.color = new Color(0, 0, 0, (1.0f - Settings.brightness) / 2.0f);
    }
    public event Action onHandlerChange;
    public event Action onServerUsernameChange;
    public void SwitchHandler(SaveHandlerMode mode)
    {
        m_saveMode = mode;
        switch (mode)
        {
            case SaveHandlerMode.Server: handler = serverHandler; break;
            default: handler = localHandler; break;
        }
        onHandlerChange?.Invoke();
    }
    public string serverUsername 
    { 
        get => serverHandler.username; 
        set
        {
            serverHandler.username = value;
            PlayerPrefs.SetString("ServerUsername", value);
            onServerUsernameChange?.Invoke();
        } 
    }
    private void Start()
    {
        LoadSettings();
    }
    public void SaveSettings()
    {
        localHandler.Save(JsonUtility.ToJson(Settings.Save(), true), "Settings");
    }
    public void LoadSettings()
    {
        localHandler.Load("Settings", (data) =>
        {
            SettingsSaveData tmp;
            if (data == null) tmp = new();
            else tmp = JsonUtility.FromJson<SettingsSaveData>(data);
            Settings.Load(tmp);
        });
    }
    public void Save(string fileName, Action<SaveData> onSave = null)
    {
        if (GameManager.isInGame == false) return;
        SaveData data = new();
        foreach (var i in GetSavables()) i.Save(data);
        string tmp = JsonUtility.ToJson(data, prettyPrint);
        handler.Save(tmp, fileName, () => onSave?.Invoke(data));
    }
    public void GetSaveData(string fileName, Action<SaveData> onLoad) => handler.Load(fileName, (data) =>
    {
        onLoad?.Invoke(JsonUtility.FromJson<SaveData>(data));
    });
    public void LoadScene(string sceneName)
    {
        sceneLoadBlack.DOPause();
        sceneLoadBlack.pivot = new Vector2(1.0f, 0.5f);
        sceneLoadBlack.DOScaleX(1.0f, 0.5f).SetUpdate(true).SetEase(Ease.InCirc).OnComplete(() =>
        {
            sceneLoadingText.gameObject.SetActive(true);
            UnityAction<Scene, LoadSceneMode> onLoad = (a, b) =>
            {
                sceneLoadingText.gameObject.SetActive(false);
                sceneLoadBlack.pivot = new Vector2(0.0f, 0.5f);
                sceneLoadBlack.DOScaleX(0.0f, 0.5f).SetUpdate(true).SetEase(Ease.InCirc);
            };
            onLoad += (a, b) => SceneManager.sceneLoaded -= onLoad;
            SceneManager.sceneLoaded += onLoad;
            SceneManager.LoadScene(sceneName);
        });
    }
    public void LoadGame(string loadingFileName)
    {
        sceneLoadBlack.DOPause();
        sceneLoadBlack.pivot = new Vector2(1.0f, 0.5f);
        sceneLoadBlack.DOScaleX(1.0f, 0.5f).SetUpdate(true).SetEase(Ease.InCirc).OnComplete(() =>
        {
            sceneLoadingText.gameObject.SetActive(true);
            handler.Load(loadingFileName, (data) =>
            {
                Debug.Log(data);
                SaveData tmp = JsonUtility.FromJson<SaveData>(data);
                UnityAction<Scene, LoadSceneMode> onLoad = (a, b) =>
                {
                    if (tmp != null) foreach (var i in GetSavables()) i.Load(tmp);
                    sceneLoadingText.gameObject.SetActive(false);
                    sceneLoadBlack.pivot = new Vector2(0.0f, 0.5f);
                    sceneLoadBlack.DOScaleX(0.0f, 0.5f).SetUpdate(true).SetEase(Ease.InCirc);
                };
                onLoad += (a, b) => SceneManager.sceneLoaded -= onLoad;
                SceneManager.sceneLoaded += onLoad;
                SceneManager.LoadScene("Game");
            });
        });
    }
    public void NewGame()
    {
        LoadGame(null);
    }
    private void OnApplicationQuit()
    {
        if(GameManager.isInGame && GameManager.Instance.CanSave()) Save("AutoSave");
    }
    IEnumerable<ISavable> GetSavables() => FindObjectsOfType<MonoBehaviour>(true).OfType<ISavable>();
}
[System.Serializable]
public enum SaveHandlerMode
{
    Local = 0,
    Server = 1
}