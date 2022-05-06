using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;

public class StoryPreview : MonoBehaviour
{
    public Image PreviewImg = null;
    public Text TitleText = null;
    public GameObject SelectedFlag = null;

    public UnityEvent<string> OnSelected = null;

    public string StoryName
    {
        get;
        private set;
    }

    public bool Selected
    {
        get => SelectedFlag.activeSelf;
        set => SelectedFlag.SetActive(value);
    }

    public void LoadPreview(string storyName)
    {
        StoryName = storyName;
        TitleText.text = storyName;
        Addressables.LoadAssetAsync<Sprite>("stories/" + storyName + "/preview.png").Completed += (AsyncOperationHandle<Sprite> obj) =>
        {
            PreviewImg.sprite = obj.Result;
        };
    }

    public void OnClicked()
    {
        OnSelected?.Invoke(StoryName);
    }
}
