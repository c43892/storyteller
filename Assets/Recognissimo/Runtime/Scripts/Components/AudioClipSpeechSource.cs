using System;
using System.Collections;
using Recognissimo.Utils;
using UnityEngine;

namespace Recognissimo.Components
{
    /// <summary>
    ///     Sets up an AudioClip as speech source for the <see cref="SpeechRecognizer"/>
    /// </summary>
    [AddComponentMenu("Recognissimo/Speech Sources/AudioClip Speech Source")]
    public class AudioClipSpeechSource : SpeechSource
    {
        private bool _isProducing;

        /// <summary>
        ///     Audio clip from which the data will be taken
        /// </summary>
        public AudioClip clip;

        /// <inheritdoc />
        public override int SampleRate => clip.frequency;

        /// <inheritdoc />
        public override void StartProduce()
        {
            _isProducing = true;
            StartCoroutine(Produce());
        }

        /// <inheritdoc />
        public override void StopProduce()
        {
            _isProducing = false;
        }

        private IEnumerator Produce()
        {
            var pool = ArrayPool<float>.Shared;
            var array = pool.Rent(Math.Min(clip.samples, ArrayPool<float>.MaxArraySize));
            var samplesLeft = clip.samples;

            if (clip.channels > 1)
            {
                throw new NotSupportedException("Reading non-mono AudioClip is not supported yet");
            }

            var processedSamples = 0;
            while (_isProducing && samplesLeft > 0)
            {
                var requestedSamples = Math.Min(samplesLeft, array.Length);
                clip.GetData(array, processedSamples);

                for (var i = 0; i < requestedSamples; i++)
                {
                    array[i] = (short) (short.MaxValue * array[i]);
                }

                OnSamplesReady(new SamplesReadyEvent(array, requestedSamples));

                samplesLeft -= requestedSamples;
                processedSamples += requestedSamples;
                yield return null;
            }

            pool.Return(array);
            OnDried();
        }
    }
}