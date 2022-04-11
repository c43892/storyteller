using System;
using UnityEngine;

namespace Recognissimo.Components
{
    /// <summary>
    ///     Base class for all speech sources
    /// </summary>
    public abstract class SpeechSource : MonoBehaviour
    {
        /// <summary>
        ///     Speech sampling rate. The parameter is read once at the start of recognition
        /// </summary>
        public virtual int SampleRate => -1;

        /// <summary>
        ///     Event signaling the arrival of new samples. The submitted samples will be added to the recognition queue
        /// </summary>
        public event EventHandler<SamplesReadyEvent> SamplesReady;

        /// <summary>
        ///     Event signaling that samples have run out and will no longer be available
        /// </summary>
        public event EventHandler Dried;

        /// <summary>
        ///     Method called by the recognizer at the start of recognition
        /// </summary>
        public abstract void StartProduce();

        /// <summary>
        ///     Method called by the recognizer at the stop of recognition
        /// </summary>
        public abstract void StopProduce();

        /// <summary>
        ///     Helper method for firing the event
        /// </summary>
        protected void OnSamplesReady(SamplesReadyEvent e)
        {
            SamplesReady?.Invoke(this, e);
        }

        /// <summary>
        ///     Helper method for firing the event
        /// </summary>
        protected void OnDried()
        {
            Dried?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Event data
        /// </summary>
        public class SamplesReadyEvent : EventArgs
        {
            public SamplesReadyEvent(float[] samples, int length)
            {
                Samples = samples;
                Length = length;
            }

            /// <summary>
            ///     Audio samples array. Only mono 16-bit PCM supported
            /// </summary>
            public float[] Samples { get; }

            /// <summary>
            ///     Audio samples array payload length
            /// </summary>
            public int Length { get; }
        }
    }
}