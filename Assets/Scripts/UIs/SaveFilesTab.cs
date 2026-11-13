using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveFilesTab : MonoBehaviour
{
    [SerializeField] Transform filesAnchor;
    [SerializeField] SaveFileUI prefab;
    [SerializeField] TMP_Dropdown handlerMode;
    [SerializeField] TMP_InputField serverHandlerUsername;
    List<SaveFileUI> files = new();
    public void Initialize()
    {
        SaveFileUI tmp = Instantiate(prefab, filesAnchor);
        files.Add(tmp);
        tmp.Set("AutoSave");
        for(int i = 0; i < GlobalManager.Instance.fileCount; i++)
        {
            SaveFileUI tmp2 = Instantiate(prefab, filesAnchor);
            tmp2.Set($"Save{i+1}");
        }
        handlerMode.onValueChanged.AddListener(value =>
        {
            SaveHandlerMode mode = (SaveHandlerMode)value;
            GlobalManager.Instance.SwitchHandler(mode);
        });
        serverHandlerUsername.text = GlobalManager.Instance.serverUsername;
        serverHandlerUsername.onEndEdit.AddListener(value =>
        {
            GlobalManager.Instance.serverUsername = value;
        });
    }
    private void OnEnable()
    {
        GlobalManager.Instance.onHandlerChange += OnHandlerChange;
        OnHandlerChange();
    }
    private void OnDisable()
    {
        GlobalManager.Instance.onHandlerChange -= OnHandlerChange;
    }
    void OnHandlerChange()
    {
        SaveHandlerMode mode = GlobalManager.Instance.saveMode;
        serverHandlerUsername.gameObject.SetActive(mode == SaveHandlerMode.Server);
    }
}