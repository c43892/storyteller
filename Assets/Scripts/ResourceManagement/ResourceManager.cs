using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using System;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Video;

public class ResourceManager : MonoBehaviour
{
    // public readonly static string wordPronUrl = "https://ssl.gstatic.com/dictionary/static/sounds/oxford/{0}--_us_1.mp3";
    public readonly static string wordPronUrl = "https://dict.youdao.com/dictvoice?type=0&audio={0}";
    public readonly static string wordPronSavingPath = Path.Combine(Application.persistentDataPath, "{0}.mp3");
    public readonly static string wordPronLoaingPath = Path.Combine("file://" + Application.persistentDataPath, "{0}.mp3");

    public static IEnumerator DownloadWordPronounciation(string word)
    {
        var url = string.Format(wordPronUrl, word);
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            byte[] bytes = www.downloadHandler.data;
            var savingPath = string.Format(wordPronSavingPath, word);
            System.IO.File.WriteAllBytes(savingPath, bytes);
        }
    }

    public static IEnumerator LoadWordPronounciation(string word, Action<AudioClip> onLoaded)
    {
        var mp3Path = string.Format(wordPronLoaingPath, word);
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(mp3Path, AudioType.MPEG);
        yield return www.SendWebRequest();
        onLoaded?.Invoke((www.result == UnityWebRequest.Result.Success) ? DownloadHandlerAudioClip.GetContent(www) : null);
    }
}

