using System;
using System.Collections.Concurrent;
using Recognissimo.Utils;
using UnityEngine;
using Vosk;

namespace Recognissimo.Core
{
    public class RecognizerWrapper
    {
        private static readonly ArrayPool<float> Pool = ArrayPool<float>.Shared;
        private readonly Consumer<AudioBuffer> _consumer = new Consumer<AudioBuffer>();
        private readonly ConcurrentQueue<IResult> _outputQueue = new ConcurrentQueue<IResult>();
        
        public bool EnableDetailedResultDescription { get; set; }
        public Model SpeechModel { get; set; }
        public string Vocabulary { get; set; }
        public bool IsRecognizing => _consumer.IsActive;
        public int MaxAlternatives { get; set; }
        
        public void Start(int sampleRate)
        {
            VoskRecognizer voskRecognizer = null;
            
            _consumer.onStart = () =>
            {
                voskRecognizer = string.IsNullOrEmpty(Vocabulary)
                    ? new VoskRecognizer(SpeechModel, sampleRate)
                    : new VoskRecognizer(SpeechModel, sampleRate, Vocabulary);
                voskRecognizer.SetWords(EnableDetailedResultDescription);
                voskRecognizer.SetMaxAlternatives(MaxAlternatives);
            };

            _consumer.onConsume = audioBuffer =>
            {
                var resultReady = voskRecognizer.AcceptWaveform(audioBuffer.samples, audioBuffer.length);
                Pool.Return(audioBuffer.samples);
                
                if (resultReady)
                {
                    var result = JsonUtility.FromJson<Result>(voskRecognizer.Result());
                    
                    if (MaxAlternatives > 0 && result.alternatives.Count > 0)
                    {
                        result.text = result.alternatives[0].text;
                    }
                    
                    if (!string.IsNullOrEmpty(result.text))
                    {
                        _outputQueue.Enqueue(result);
                    }
                }
                else
                {
                    var partialResult = JsonUtility.FromJson<PartialResult>(voskRecognizer.PartialResult());

                    _outputQueue.Enqueue(partialResult);
                }
            };
            
            _consumer.onStop = () =>
            {
                var finalResult = JsonUtility.FromJson<Result>(voskRecognizer.FinalResult());

                if (MaxAlternatives > 0 && finalResult.alternatives.Count > 0)
                {
                    finalResult.text = finalResult.alternatives[0].text;
                }
                
                if (!string.IsNullOrEmpty(finalResult.text))
                {
                    _outputQueue.Enqueue(finalResult);
                }

                voskRecognizer.Dispose();
            };

            _consumer.Start();
        }

        public void Stop()
        {
            _consumer.Stop();
        }

        public void EnqueueSamples(float[] samples, int length)
        {
            AudioBuffer audioBuffer;
            audioBuffer.samples = Pool.Rent(length);
            audioBuffer.length = length;

            Array.Copy(samples, audioBuffer.samples, length);
            
            _consumer.Feed(audioBuffer);
        }

        public IResult GetNextResult()
        {
            _outputQueue.TryDequeue(out var result);
            return result;
        }

        private struct AudioBuffer
        {
            public float[] samples;
            public int length;
        }
    }
}