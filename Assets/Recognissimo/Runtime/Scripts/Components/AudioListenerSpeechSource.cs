using System;
using UnityEngine;
using Recognissimo.Components;
using Recognissimo.Utils;

namespace Recognissimo.Components
{
    /// <summary>
    ///     Sets up Unity AudioListener as speech source for the <see cref="SpeechRecognizer"/>
    /// </summary>
    [AddComponentMenu("Recognissimo/Speech Sources/AudioListener Speech Source")]
    public class AudioListenerSpeechSource : SpeechSource
    {
        private static readonly ArrayPool<float> Pool = ArrayPool<float>.Shared;

        private const int MinSamples = 4096;

        private bool _isProducing;
        private int _samplesLeft;

        /// <inheritdoc />
        public override int SampleRate => AudioSettings.outputSampleRate;

        /// <summary>
        ///     AudioListener channel for receiving data
        /// </summary>
        public int channel;

        /// <inheritdoc />
        public override void StartProduce()
        {
            _isProducing = true;
        }

        /// <inheritdoc />
        public override void StopProduce()
        {
            _isProducing = false;
            _samplesLeft = 0;
        }

        private void Update()
        {
            if (!_isProducing)
            {
                return;
            }

            _samplesLeft += (int) Math.Floor(SampleRate * Time.deltaTime);

            if (_samplesLeft < MinSamples)
            {
                return;
            }

            var availableSamples = Math.Min(_samplesLeft, ArrayPool<float>.MaxArraySize);
            var samples = Pool.Rent(availableSamples);
            AudioListener.GetOutputData(samples, channel);

            for (var i = 0; i < availableSamples; i++)
            {
                samples[i] = (short) (short.MaxValue * samples[i]);
            }

            OnSamplesReady(new SamplesReadyEvent(samples, availableSamples));
            Pool.Return(samples);
            _samplesLeft -= availableSamples;
        }
    }
}