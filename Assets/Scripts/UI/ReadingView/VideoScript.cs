using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class VideoScript : MonoBehaviour
{
    public SystemLanguage TargetLang { get; set; }

    readonly Dictionary<double, string> subTitles = new Dictionary<double, string>();
    public bool AsianLanguage { get; private set; }

    public Dictionary<double, string> TargetSubTitle
    {
        get => subTitles;
    }

    public void LoadScript(string storyName, bool asianLanguage, Action onCompleted)
    {
        subTitles.Clear();

        Addressables.LoadAssetAsync<TextAsset>("stories/" + storyName + "/" + TargetLang.ToString() + ".txt").Completed += (AsyncOperationHandle<TextAsset> obj) =>
        {
            AsianLanguage = asianLanguage;
            var scriptText = obj.Result.text;
            foreach (var line in scriptText.Split("\r\n".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries))
            {
                var timeStampPos = line.IndexOf('-');
                if (timeStampPos < 0)
                    continue;

                var timeStamp = System.TimeSpan.Parse(line.Substring(0, timeStampPos)).TotalSeconds;
                var text = line.Substring(timeStampPos + 1).Trim();

                if (asianLanguage)
                {
                    var chars = text.ToCharArray().Select(c => c + " ");
                    text = "";
                    foreach (var c in chars)
                        text += c;
                }

                subTitles[timeStamp] = text;
            }

            onCompleted?.Invoke();
        };
    }
}
