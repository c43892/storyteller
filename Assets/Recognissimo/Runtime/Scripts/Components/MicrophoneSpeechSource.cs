using System;
using Recognissimo.Utils;
using UnityEngine;

namespace Recognissimo.Components
{
    /// <summary>
    ///     Sets up an microphone as speech source for the <see cref="SpeechRecognizer"/>
    /// </summary>
    [AddComponentMenu("Recognissimo/Speech Sources/Microphone Speech Source")]
    public class MicrophoneSpeechSource : SpeechSource
    {
        private MicrophoneRecorder _microphoneRecorder;

        /// <summary>
        ///     Microphone initialization settings. This settings will be used when recording starts
        /// </summary>
        public MicrophoneSettings microphoneSettings;

        /// <summary>
        ///     Whether to start capturing as soon as the component awakes
        /// </summary>
        public bool recordOnAwake;

        /// <inheritdoc />
        public override int SampleRate => microphoneSettings.sampleRate;

        /// <summary>
        ///     Start voice capture
        /// </summary>
        public void StartMicrophone()
        {
            _microphoneRecorder.StartRecording(microphoneSettings);
        }

        /// <summary>
        ///     Stop voice capture
        /// </summary>
        public void StopMicrophone()
        {
            _microphoneRecorder.StopRecording();
            OnDried();
        }

        /// <inheritdoc />
        public override void StartProduce()
        {
            _microphoneRecorder.samplesReady = OnSamplesReady;
        }

        /// <inheritdoc />
        public override void StopProduce()
        {
            _microphoneRecorder.samplesReady = null;
        }

        private void Awake()
        {
            _microphoneRecorder = gameObject.AddComponent<MicrophoneRecorder>();
            
            if (recordOnAwake)
            {
                StartMicrophone();
            }
        }

        /// <summary>
        ///     Microphone settings
        /// </summary>
        [Serializable]
        public struct MicrophoneSettings
        {
            /// <summary>
            ///     Microphone index from UnityEngine.Microphone.devices list
            /// </summary>
            public int deviceIndex;

            /// <summary>
            ///     Sampling frequency of the device (Hz).
            ///     Use smaller values to reduce memory consumption.
            ///     Recommended value is 16000 Hz
            /// </summary>
            public int sampleRate;

            /// <summary>
            ///     Max length of recording before overlapping (seconds).
            ///     Use smaller values to reduce the delay at the start of recording.
            ///     Recommended value is 1
            /// </summary>
            public int maxRecordingTime;

            /// <summary>
            ///     How often audio frames should be submitted to the recognizer (seconds)
            ///     Use smaller values to submit audio samples more often.
            ///     Recommended value is 0.25
            /// </summary>
            public float timeSensitivity;
        }

        private class MicrophoneRecorder : MonoBehaviour
        {
            private static readonly ArrayPool<float> Pool = ArrayPool<float>.Shared;

            private AudioClip _clip;

            private int _clipSamplesLength;

            private string _deviceName;
            private int _lastPos;
            private int _maxSamplesNum;

            private int _minSamplesNum;

            private int _pos;

            public Action<SamplesReadyEvent> samplesReady;

            public bool IsRecording { get; private set; }

            private void Update()
            {
                if (!IsRecording)
                {
                    return;
                }

                _pos = Microphone.GetPosition(_deviceName);

                if (samplesReady == null)
                {
                    _lastPos = _pos;
                    return;
                }

                var availableSamples = (_pos - _lastPos + _clipSamplesLength) % _clipSamplesLength;

                while (availableSamples > _minSamplesNum)
                {
                    var samplesLength = Math.Min(availableSamples, _maxSamplesNum);
                    var samples = Pool.Rent(samplesLength);

                    if (!_clip.GetData(samples, _lastPos))
                    {
                        Debug.LogError("Cannot access microphone data. Make sure you are not using the microphone elsewhere in your project");
                    }

                    for (var i = 0; i < samplesLength; i++)
                    {
                        samples[i] = (short) (short.MaxValue * samples[i]);
                    }

                    samplesReady(new SamplesReadyEvent(samples, samplesLength));
                    Pool.Return(samples);

                    availableSamples -= samplesLength;
                    _lastPos = (_lastPos + samplesLength) % _clipSamplesLength;
                }
            }

            private void OnDestroy()
            {
                if (IsRecording)
                {
                    StopRecording();
                }
            }

            public void StartRecording(MicrophoneSettings microphoneSettings)
            {
                _deviceName = Microphone.devices[microphoneSettings.deviceIndex];
                _clip = Microphone.Start(_deviceName, true, microphoneSettings.maxRecordingTime,
                    microphoneSettings.sampleRate);

                _lastPos = 0;
                _clipSamplesLength = _clip.samples;

                _minSamplesNum = Math.Max(ArrayPool<float>.MinArraySize,
                    (int) (microphoneSettings.timeSensitivity * microphoneSettings.sampleRate));

                _maxSamplesNum = Math.Min(ArrayPool<float>.MaxArraySize, _clipSamplesLength);

                IsRecording = true;
            }

            public void StopRecording()
            {
                Microphone.End(_deviceName);
                IsRecording = false;
            }
        }
    }
}