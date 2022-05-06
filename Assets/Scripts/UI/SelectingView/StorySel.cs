using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using System.Linq;
using UnityEngine.AddressableAssets.ResourceLocators;

public class StorySel : MonoBehaviour
{
    public StoryPreview PreviewModel;
    public Transform PreviewContainer;

    readonly List<StoryPreview> previewObjs = new List<StoryPreview>();

    // Start is called before the first frame update
    void Start()
    {
        Addressables.LoadAssetAsync<TextAsset>("stories/storyList").Completed += (AsyncOperationHandle<TextAsset> obj) =>
        {
            var storyList = obj.Result.text;
            var stroies = storyList.Split("\r\n".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries);

            ClearAllPreviews();
            GeneratePrevious(stroies);
        };
    }
    void ClearAllPreviews()
    {
        foreach (var p in previewObjs)
            Destroy(p.gameObject);

        previewObjs.Clear();
    }

    void GeneratePrevious(string[] stories)
    {
        foreach (var story in stories)
        {
            var previewObj = Instantiate(PreviewModel);

            previewObj.transform.SetParent(PreviewContainer);
            previewObj.gameObject.SetActive(true);
            previewObj.transform.localScale = Vector3.one;

            previewObj.LoadPreview(story);

            previewObjs.Add(previewObj);
        }
    }

    public void OnStorySelected(string storyName)
    {
        foreach (StoryPreview story in previewObjs)
            story.Selected = story.StoryName == storyName;
    }
}
