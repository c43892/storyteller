using System;
using System.Collections.Generic;
using Recognissimo.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Recognissimo.Components
{
    /// <summary>
    ///     This is the primary Recognissimo component. It processes audio data and outputs a result based on the language
    ///     model
    /// </summary>
    [AddComponentMenu("Recognissimo/Speech Recognizer")]
    public class SpeechRecognizer : MonoBehaviour
    {
        private RecognizerWrapper _recognizer;
        private bool _sourceDried;

        /// <summary>
        ///     Model provider. This value is read when <see cref="SpeechRecognizer.StartRecognition"/> called
        /// </summary>
        public ModelProvider modelProvider;

        /// <summary>
        ///     Speech source. This value is read when <see cref="SpeechRecognizer.StartRecognition"/> called
        /// </summary>
        public SpeechSource speechSource;

        /// <summary>
        ///     Vocabulary. This value is read when <see cref="SpeechRecognizer.StartRecognition"/> called
        /// </summary>
        public Vocabulary vocabulary;

        /// <summary>
        ///     Whether the recognition result should include details
        /// </summary>
        public bool enableDetailedResultDescription;

        /// <summary>
        ///     Whether the PartialResult can be empty
        /// </summary>
        public bool allowEmptyPartialResults;
        
        /// <summary>
        ///     Whether the recognition result should contain list of alternative results
        /// </summary>
        public int alternatives;

        /// <summary>
        ///     New partial result ready
        /// </summary>
        public PartialResultEvent partialResultReady;

        /// <summary>
        ///     New result ready
        /// </summary>
        public ResultEvent resultReady;

        /// <summary>
        ///     Speech source dried and all samples are recognized
        /// </summary>
        public UnityEvent finished;

        /// <summary>
        ///     Speech recognizer crashed
        /// </summary>
        public UnityEvent crashed;

        /// <summary>
        ///     Current recognition state
        /// </summary>
        public bool IsRecognizing { get; private set; }

        /// <summary>
        ///     Start speech recognition. Fields <see cref="SpeechRecognizer.speechSource"/> and <see cref="SpeechRecognizer.modelProvider"/> must be set by the time the method is called
        /// </summary>
        public void StartRecognition()
        {
            if (IsRecognizing)
            {
                StopRecognition();
            }
            
            if (!ValidateInputs())
            {
                return;
            }

            _recognizer = new RecognizerWrapper
            {
                EnableDetailedResultDescription = enableDetailedResultDescription,
                SpeechModel = modelProvider.Model,
                MaxAlternatives = alternatives
            };

            if (vocabulary.wordList?.Count > 0)
            {
                _recognizer.Vocabulary = CreateVocabularyString();
            }

            AttachEventHandlers();
            _recognizer.Start(speechSource.SampleRate);

            _sourceDried = false;
            IsRecognizing = true;

            speechSource.StartProduce();
        }
        
        /// <summary>
        ///     Stop speech recognition
        /// </summary>
        public void StopRecognition()
        {
            if (_recognizer == null) return;
            if (!_sourceDried) speechSource.StopProduce();
            if (_recognizer.IsRecognizing) _recognizer.Stop();
            IsRecognizing = false;
            DetachEventHandlers();
        }

        private bool ValidateInputs()
        {
            var error = FindError();
            
            if (error == null)
            {
                return true;
            }

            Debug.LogError(error);
            return false;
        }

        private string FindError()
        {
            if (!modelProvider) return "Model provider not set";
            if (modelProvider.Model == null) return "Model of model provider is null";
            if (!speechSource) return "Speech source not set";
            if (speechSource.SampleRate <= 0) return "Speech source sample rate value cannot be used";
            return null;
        }

        private void OnSourceDried(object sender, EventArgs e)
        {
            _sourceDried = true;
            _recognizer.Stop();
        }

        private void OnSamplesReady(object sender, SpeechSource.SamplesReadyEvent e)
        {
            if (_recognizer.IsRecognizing)
            {
                _recognizer.EnqueueSamples(e.Samples, e.Length);
            }
        }

        private void AttachEventHandlers()
        {
            speechSource.Dried += OnSourceDried;
            speechSource.SamplesReady += OnSamplesReady;
        }

        private void DetachEventHandlers()
        {
            speechSource.Dried -= OnSourceDried;
            speechSource.SamplesReady -= OnSamplesReady;
        }

        private string CreateVocabularyString()
        {
            const string separator = "\",\"";
            return $"[\"{string.Join(separator, vocabulary.wordList).ToLower()}\"]";
        }

        private void HandleRecognizerFinished()
        {
            StopRecognition();

            if (_sourceDried)
            {
                finished?.Invoke();
            }
            else
            {
                crashed?.Invoke();
            }
        }

        private void ObserveRecognizer()
        {
            switch (_recognizer.GetNextResult())
            {
                case null:
                    if (!_recognizer.IsRecognizing)
                    {
                        HandleRecognizerFinished();
                    }
                    break;
                case PartialResult partialResult:
                    if (!string.IsNullOrEmpty(partialResult.partial) || allowEmptyPartialResults)
                    {
                        partialResultReady?.Invoke(partialResult);
                    }
                    break;
                case Result result:
                    resultReady?.Invoke(result);
                    break;
            }
        }
        
        private void Update()
        {
            if (IsRecognizing)
            {
                ObserveRecognizer();
            }
        }
        
        /// <summary>
        ///     Recognizer's vocabulary
        /// </summary>
        [Serializable]
        public struct Vocabulary
        {
            /// <summary>
            ///     List of words to recognize. Speech recognizer will select the result only from the presented words.
            ///     Use special word "[unk]" (without quotes) to allow unknown words in the output:
            /// </summary>    
            /// <example>
            /// <code>
            ///     vocabulary.wordList = new List&lt;string&gt; {"light", "on", "off", "[unk]"};
            /// </code>
            ///     This feature may not work with some language models
            /// </example>
            public List<string> wordList;
        }

        [Serializable]
        public class PartialResultEvent : UnityEvent<PartialResult>
        {
        }

        [Serializable]
        public class ResultEvent : UnityEvent<Result>
        {
        }
    }
}