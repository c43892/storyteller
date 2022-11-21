using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class VideoScript : MonoBehaviour
{
    public SystemLanguage TargetLang { get; set; }

    readonly Dictionary<double, string> subTitlesEndTime = new Dictionary<double, string>();
    readonly Dictionary<double, string> subTitlesStartTime = new Dictionary<double, string>();
    public bool AsianLanguage { get; private set; }

    public Dictionary<double, string> SubTitleEndtime
    {
        get => subTitlesEndTime;
    }

    public Dictionary<double, string> SubTitleStarttime
    {
        get => subTitlesStartTime;
    }

    public void ParseSRTSubtitle(string scriptText, bool asianLanguage, Action onCompleted)
    {
        subTitlesEndTime.Clear();
        subTitlesStartTime.Clear();

        var startTimeOffset = 0.0;

        // Addressables.LoadAssetAsync<TextAsset>("stories/" + storyName + "/" + TargetLang.ToString() + ".srt").Completed += (AsyncOperationHandle<TextAsset> obj) =>
        {
            AsianLanguage = asianLanguage;
            var combine2Lines = false;

            // var scriptText = obj.Result.text;
            using (StreamReader r = new(new MemoryStream(Encoding.UTF8.GetBytes(scriptText))))
            {
                while (!r.EndOfStream)
                {
                    var line = r.ReadLine().Trim(" \r\n".ToCharArray());

                    if(line.StartsWith("start_offset -"))
                    {
                        var offsetStr = line.Split("-".ToArray(), StringSplitOptions.RemoveEmptyEntries)[1];
                        offsetStr = offsetStr.Replace(",", ".").Trim();
                        startTimeOffset = System.TimeSpan.Parse(offsetStr).TotalSeconds;
                    }
                    else if (line == "combine 2 lines")
                    {
                        combine2Lines = true;
                    }
                    else if (line.Contains("-->")) // starts with timestamp
                    {
                        var es = line.Split("-->".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                        var startTime = es[0].Replace(",", ".").Trim();
                        var endTime = es[1].Replace(",", ".").Trim();

                        var dstLang = r.ReadLine().Trim();
                        if (dstLang == "") continue;
                        var srcLang = r.ReadLine().Trim();

                        if (combine2Lines)
                        {
                            srcLang = dstLang + " " + srcLang;
                            dstLang = "";
                        }

                        var text = srcLang.Replace(',', ' ')
                            .Replace('.', ' ').Replace('!', ' ')
                            .Replace(':', ' ').Replace('%', ' ')
                            .Replace('#', ' ').Replace('$', ' ')
                            .Replace('@', ' ').Replace('*', ' ')
                            .Replace('(', ' ').Replace(')', ' ')
                            .Replace('[', ' ').Replace(']', ' ')
                            .Replace('?', ' ').Replace('-', ' ')
                            .Replace('—', ' ').Replace('\'', ' ')
                            .Replace('“', ' ').Replace('”', ' ')
                            .Replace('"', ' ').Replace(';', ' ');

                        if (asianLanguage)
                        {
                            var chars = text.ToCharArray().Select(c => c + " ");
                            text = "";
                            foreach (var c in chars)
                                text += c;
                        }

                        subTitlesStartTime[System.TimeSpan.Parse(startTime).TotalSeconds - startTimeOffset] = text;
                        subTitlesEndTime[System.TimeSpan.Parse(endTime).TotalSeconds - startTimeOffset] = text;
                    }
                }

                onCompleted?.Invoke();
            };
        }
    }

    public void ParseRawTextSubtitle(string scriptText, bool asianLanguage, Action onCompleted)
    {
        subTitlesEndTime.Clear();
        subTitlesStartTime.Clear();

        //  Addressables.LoadAssetAsync<TextAsset>("stories/" + storyName + "/" + TargetLang.ToString() + ".txt").Completed += (AsyncOperationHandle<TextAsset> obj) =>
        {
            AsianLanguage = asianLanguage;
            // var scriptText = obj.Result.text;
            var lastEndTime = 0.0;
            foreach (var line in scriptText.Split("\r\n".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries))
            {
                var timeStampPos = line.IndexOf('-');
                if (timeStampPos < 0)
                    continue;

                var timeStamp = System.TimeSpan.Parse(line.Substring(0, timeStampPos)).TotalSeconds;
                var text = line[(timeStampPos + 1)..].Trim();

                text = text.Replace(',', ' ').Replace('.', ' ').Replace('!', ' ').Replace('?', ' ');
                if (asianLanguage)
                {
                    var chars = text.ToCharArray().Select(c => c + " ");
                    text = "";
                    foreach (var c in chars)
                        text += c;
                }

                lastEndTime = timeStamp;
                subTitlesEndTime[timeStamp] = text;
                if (subTitlesStartTime.Count == 0)
                    subTitlesStartTime[0] = text;
                else
                    subTitlesStartTime[lastEndTime] = text;
            }

            onCompleted?.Invoke();
        };
    }

    public void LoadSubtitle(string storyName, bool asianLanguage, Action onCompleted)
    {
        var srtPath = "stories/" + storyName + "/" + TargetLang.ToString() + ".srt.txt";
        Addressables.LoadAssetAsync<TextAsset>(srtPath).Completed += (AsyncOperationHandle<TextAsset> obj) =>
        {
            if (obj.Status == AsyncOperationStatus.Succeeded)
                ParseSRTSubtitle(obj.Result.text, asianLanguage, onCompleted);
            else
            {
                var rawTextPath = "stories/" + storyName + "/" + TargetLang.ToString() + ".txt";
                Addressables.LoadAssetAsync<TextAsset>(rawTextPath).Completed += (AsyncOperationHandle<TextAsset> obj) =>
                {
                    if (obj.Status == AsyncOperationStatus.Succeeded)
                        ParseSRTSubtitle(obj.Result.text, asianLanguage, onCompleted);
                    else
                        onCompleted?.Invoke();
                };
            }
        };
    }
}
