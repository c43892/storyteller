using UnityEngine;
using UnityEngine.UI;

namespace Recognissimo.Components.Examples
{
    [AddComponentMenu("Recognissimo/Examples/Voice Activity Detector Example")]
    public class VoiceActivityDetectorExample : MonoBehaviour
    {
        [SerializeField] private VoiceActivityDetector activityDetector;
        [SerializeField] private Image image;
        
        void Start()
        {
            image.color = Color.gray;
            
            activityDetector.spoke.AddListener(() =>
            {
                image.color = Color.green;
            });
            activityDetector.silenced.AddListener(() =>
            {
                image.color = Color.red;
            });
        }
    }
}