using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using UnityEngine.Networking;
using System.Reflection;
using UnityEngine.EventSystems;
using System.IO;

public class ServerSaveFileHandler : SaveFileHandler
{
    public string username;
    readonly string gameID, baseUrl;
    readonly MonoBehaviour coroutinePlayer;
    public ServerSaveFileHandler(string gameID, string baseUrl, MonoBehaviour coroutinePlayer)
    {
        this.gameID = gameID;
        this.baseUrl = baseUrl;
        this.coroutinePlayer = coroutinePlayer;
    }
    public override void Load(string fileName, Action<string> onLoad = null)
    {
        coroutinePlayer.StartCoroutine(LoadDataFromServer(fileName, onLoad));
    }
    public override void Save(string data, string fileName, Action onSave = null)
    {
        coroutinePlayer.StartCoroutine(SaveDataToServer(fileName, data, onSave));
    }
    public override void Delete(string fileName, Action onDelete = null)
    {
        coroutinePlayer.StartCoroutine(DeleteDataFromServer(fileName, onDelete));
    }

    private IEnumerator LoadDataFromServer(string fileName, Action<string> onLoad)
    {
        UnityWebRequest request = new UnityWebRequest($"{baseUrl}/{username}/{gameID}/{fileName}", "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json; indent=4");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            if(request.downloadHandler.text != string.Empty)
            {
                SaveFile file = new();
                try
                {
                    file = JsonUtility.FromJson<SaveFile>(request.downloadHandler.text);
                }
                catch(Exception e)
                {
                    Debug.LogError(e);
                }
                onLoad?.Invoke(file.saveData);
                yield break;
            }
        }
        else Debug.LogError("Error fetching movies" + request.downloadHandler.text);
        onLoad?.Invoke(null);
    }
    public IEnumerator SaveDataToServer(string fileName, string content, Action onSave)
    {
        UnityWebRequest request = new UnityWebRequest($"{baseUrl}/", "POST");
        SaveFile file = new()
        {
            username = username,
            gameID = gameID,
            fileName = fileName,
            saveData = content
        };
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(file));
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSave?.Invoke();
        }
        else
        {
            Debug.LogError("Error fetching movies");
        }
    }
    private IEnumerator DeleteDataFromServer(string fileName, Action onDelete)
    {
        UnityWebRequest request = new UnityWebRequest($"{baseUrl}/{username}/{gameID}/{fileName}", "DELETE");
        SaveFile file = new()
        {
            username = username,
            gameID = gameID,
            fileName = fileName,
        };
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(file));
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onDelete?.Invoke();
        }
        else
        {
            Debug.LogError("Error fetching movies");
        }
    }
    [System.Serializable]
    public struct SaveFile
    {
        public string username, gameID, fileName, saveData;
    }
}