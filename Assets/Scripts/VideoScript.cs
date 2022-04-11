using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VideoScript : MonoBehaviour
{
    public readonly Dictionary<double, string> subTitles = new Dictionary<double, string>();
    public TextAsset scriptText;

    // Chinese script needs this
    public bool insertSpaceIntoCharacters = false;

    public void Load()
    {
        foreach (var line in scriptText.text.Split("\r\n".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries))
        {
            var timeStampPos = line.IndexOf('-');
            if (timeStampPos < 0)
                continue;

            var timeStamp = System.TimeSpan.Parse(line.Substring(0, timeStampPos)).TotalSeconds;
            var text = line.Substring(timeStampPos + 1).Trim();
            if (insertSpaceIntoCharacters)
            {
                var chars = text.ToCharArray().Select(c => c + " ");
                text = "";
                foreach (var c in chars)
                    text += c;
            }

            subTitles[timeStamp] = text;
        }
    }
}
