using Recognissimo.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Recognissimo.Components
{
    /// <summary>
    ///     Voice activity detector component
    /// </summary>
    [AddComponentMenu("Recognissimo/Voice Activity Detector")]
    public class VoiceActivityDetector : MonoBehaviour
    {
        private bool _voiceActiveLastFrame;

        /// <summary>
        ///     Speech recognizer. The value is read when <see cref="VoiceActivityDetector.StartDetection"/> called or when script is enabled if <see cref="VoiceActivityDetector.autoStart"/> is active
        /// </summary>
        public SpeechRecognizer recognizer;

        /// <summary>
        ///     Whether to activate voice activity detector at startup 
        /// </summary>
        public bool autoStart;

        /// <summary>
        ///     Voice became active
        /// </summary>
        public UnityEvent spoke;

        /// <summary>
        ///     Voice became inactive
        /// </summary>
        /// 
        public UnityEvent silenced;

        /// <summary>
        ///     Start voice activity detection
        /// </summary>
        public void StartDetection()
        {
            if (!recognizer)
            {
                return;
            }

            recognizer.allowEmptyPartialResults = true;
            recognizer.partialResultReady.AddListener(OnPartialResult);
            recognizer.StartRecognition();
        }

        /// <summary>
        ///     Stop voice activity detection
        /// </summary>
        public void StopDetection()
        {
            if (!recognizer)
            {
                return;
            }

            recognizer.StopRecognition();
            recognizer.partialResultReady.RemoveListener(OnPartialResult);
        }

        private void OnPartialResult(PartialResult result)
        {
            var voiceActiveThisFrame = !string.IsNullOrEmpty(result.partial);

            if (_voiceActiveLastFrame ^ voiceActiveThisFrame)
            {
                if (_voiceActiveLastFrame)
                {
                    silenced?.Invoke();
                }
                else
                {
                    spoke?.Invoke();
                }
            }

            _voiceActiveLastFrame = voiceActiveThisFrame;
        }
        
        private void Start()
        {
            if (autoStart)
            {
                StartDetection();
            }
        }
    }
}