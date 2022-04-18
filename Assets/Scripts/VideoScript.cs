using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VideoScript : MonoBehaviour
{
    public SystemLanguage TargetLang { get; set; }

    readonly Dictionary<SystemLanguage, TextAsset> scriptTextDict = new Dictionary<SystemLanguage, TextAsset>();
    readonly Dictionary<SystemLanguage, Dictionary<double, string>> subTitles = new Dictionary<SystemLanguage, Dictionary<double, string>>();
    
    public Dictionary<double, string> TargetSubTitle
    {
        get => subTitles[TargetLang];
    }

    public void Load()
    {
        scriptTextDict[SystemLanguage.ChineseSimplified] = Resources.Load<TextAsset>("VideoScripts/videoClip_cn-ZH");
        scriptTextDict[SystemLanguage.English] = Resources.Load<TextAsset>("VideoScripts/videoClip_en-US");

        foreach (var lang in scriptTextDict.Keys)
        {
            var scriptText = scriptTextDict[lang].text;
            subTitles[lang] = new Dictionary<double, string>();
            foreach (var line in scriptText.Split("\r\n".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries))
            {
                var timeStampPos = line.IndexOf('-');
                if (timeStampPos < 0)
                    continue;

                var timeStamp = System.TimeSpan.Parse(line.Substring(0, timeStampPos)).TotalSeconds;
                var text = line.Substring(timeStampPos + 1).Trim();

                if (lang == SystemLanguage.ChineseSimplified || lang == SystemLanguage.ChineseSimplified)
                {
                    var chars = text.ToCharArray().Select(c => c + " ");
                    text = "";
                    foreach (var c in chars)
                        text += c;
                }

                subTitles[lang][timeStamp] = text;
            }
        }
    }
}
