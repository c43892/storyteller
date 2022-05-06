using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectingView : MonoBehaviour
{
    public Transform StoryPreviewContainer;
    public StoryPreview PreviewModel;
    public StoryPreview[] AllPreviews;
    public ReadingView ReadingView;
    public GameObject BtnStartPlaying;

    public SystemLanguage CurrentLanguage { get; private set; }
    public string CurrentStory { get; private set; }

    void Start()
    {
        ReadingView.gameObject.SetActive(false);
        BtnStartPlaying.gameObject.SetActive(false);
    }

    public void SetCurrentLanguage(SystemLanguage lang)
    {
        CurrentLanguage = lang;
    }

    public void SetCurrentStory(string storyName)
    {
        CurrentStory = storyName;
        BtnStartPlaying.gameObject.SetActive(CurrentStory != null);
    }

    public void StartPlaying()
    {
        ReadingView.gameObject.SetActive(true);
        ReadingView.Play(CurrentStory, CurrentLanguage);
        gameObject.SetActive(false);
    }
}
