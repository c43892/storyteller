using Recognissimo.Components;
using Recognissimo.Core;
using spkl.Diffs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public SpeechRecognizer recognizer;
    public VideoScript script;
    public float PassThreshold = 0.6f;

    [Serializable]
    public struct RecognitionTarget
    {
        public string text;
    }

    [Serializable]
    public class RecognitionStartEvent : UnityEvent<RecognitionTarget>
    {
    }

    public RecognitionStartEvent onRecognitionStarted = null;
    public UnityEvent onRecognitionStopped = null;

    double[] timeStamps = null;
    int nextTimePoint = 0;

    // Start is called before the first frame update
    void Start()
    {
        script.Load();
        timeStamps = script.subTitles.Keys.ToArray().OrderBy((k) => k).ToArray();
        nextTimePoint = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (videoPlayer.isPlaying && nextTimePoint < timeStamps.Length && videoPlayer.clockTime >= timeStamps[nextTimePoint])
        {
            videoPlayer.Pause();
            var targetText = script.subTitles[timeStamps[nextTimePoint]];
            recognizer.vocabulary.wordList = 
                new List<string>(targetText.Split(" ".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries));
            recognizer.StartRecognition();
            onRecognitionStarted?.Invoke(new RecognitionTarget() { text = targetText });
            Debug.Log("recognition started for: " + targetText);
        }
    }

    public void OnResult(Result result)
    {
        Debug.Log($"<color=green>{result.text}</color>");
        if (videoPlayer.isPaused && CompareSentences(
                result.text.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries),
                script.subTitles[timeStamps[nextTimePoint]].Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) >= PassThreshold)
        {
            Debug.Log("recognition stopped");
            recognizer.StopRecognition();

            nextTimePoint++;
            onRecognitionStopped?.Invoke();
            videoPlayer.Play();
        }
    }

    public float CompareSentences(string[] recognitionResult, string[] targetSentence)
    {
        MyersDiff<string> diff = new MyersDiff<string>(recognitionResult, targetSentence, StringComparer.OrdinalIgnoreCase);
        var matchCount = diff.GetResult().Count(rt => rt.ResultType == ResultType.Both);
        var matchRate = (float)matchCount / targetSentence.Length;
        Debug.Log($"<color=" + (matchRate >= PassThreshold ? "green" : "red") + $">{matchRate}</color>");
        return matchRate;
    }
}
