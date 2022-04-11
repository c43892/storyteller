using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Recognissimo.Core;
using Recognissimo.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Recognissimo.Components
{
    /// <summary>
    ///     Voice control component
    /// </summary>
    [AddComponentMenu("Recognissimo/Voice Control")]
    public class VoiceControl : MonoBehaviour
    {
        /// <summary>
        ///     List of voice commands. The value is read when <see cref="VoiceControl.Setup"/> called or when script is enabled if <see cref="VoiceControl.autoStart"/> is active
        /// </summary>
        public List<VoiceCommand> commands;

        /// <summary>
        ///     Speech recognizer. The value is read when <see cref="VoiceControl.Setup"/> called or when script is enabled if <see cref="VoiceControl.autoStart"/> is active
        /// </summary>
        public SpeechRecognizer recognizer;

        /// <summary>
        ///     Whether to activate voice control at startup 
        /// </summary>
        public bool autoStart;

        private readonly CommandDecoder _decoder = new CommandDecoder();
        private bool _isActive;

        /// <summary>
        ///     Setup component. Should be called before <see cref="VoiceControl.StartControl"/>. Time-consuming
        /// </summary>
        public void Setup()
        {
            if (commands == null || recognizer == null)
            {
                return;
            }

            _decoder.Setup(commands);

            recognizer.resultReady.RemoveListener(Process);
            recognizer.resultReady.AddListener(Process);
            recognizer.enableDetailedResultDescription = false;

            var allPhrases = commands.SelectMany(command => command.phrases);
            var distinctWords = Regex.Split(string.Join(" ", allPhrases), @"\W+").Distinct();
            var finalGrammar = string.Join(" ", distinctWords).ToLower();

            recognizer.vocabulary.wordList = new List<string> {finalGrammar, "[unk]"};
        }

        /// <summary>
        ///     <see cref="VoiceControl.Setup"/> async variant
        /// </summary>
        /// <returns>Task object</returns>
        public async Task SetupAsync()
        {
            await Task.Run(Setup);
        }

        /// <summary>
        ///     Start voice commands processing. 
        /// </summary>
        public void StartControl()
        {
            if (!recognizer)
            {
                return;
            }

            recognizer.StartRecognition();
            _isActive = true;
        }

        /// <summary>
        ///     Stop voice commands processing
        /// </summary>
        public void StopControl()
        {
            if (!recognizer)
            {
                return;
            }

            recognizer.StopRecognition();
            _isActive = false;
            
            _decoder.Stop();

            while (_decoder.TryDequeue(out _))
            {
            }
        }
        
        private void Start()
        {
            if (autoStart)
            {
                Setup();
                StartControl();
            }
        }

        private void Update()
        {
            while (_isActive && _decoder.TryDequeue(out var onSpoken))
            {
                onSpoken?.Invoke();
            }
        }
        
        private void Process(Result result)
        {
            _decoder.Enqueue(result.text);
        }

        [Serializable]
        public class SpokenEvent : UnityEvent
        {
            public SpokenEvent() {}

            public SpokenEvent(UnityAction action)
            {
                AddListener(action);
            }
        }

        /// <summary>
        ///     Phrase/callback pair for voice control
        /// </summary>
        [Serializable]
        public struct VoiceCommand
        {
            /// <summary>
            /// List of phrases to recognize. Case-insensitive.
            /// You can use groups "()" and alternations "|" to create options:
            /// <code>
            ///     "red|green"; // "red" and "green" will be recognized
            ///     "turn (on|off) the light"; // "turn on the light" or "turn off the light"
            /// </code>
            /// </summary>
            public List<string> phrases;

            /// <summary>
            /// UnityEvent that is triggered when phrase from the <see cref="VoiceCommand.phrases"/> is spoken.
            /// </summary>
            public SpokenEvent onSpoken;
        }

        private class CommandDecoder
        {
            private readonly ConcurrentQueue<SpokenEvent> _outputQueue = new ConcurrentQueue<SpokenEvent>();
            private readonly Consumer<string> _consumer = new Consumer<string>();

            private SpokenEvent[] _events; 
            private Regex _regex;
            private string _undecoded;

            public void Setup(List<VoiceCommand> commands)
            {
                if (commands == null)
                {
                    throw new ArgumentNullException(nameof(commands));
                }

                _events = commands.Select(command => command.onSpoken).ToArray();

                var groups = commands.Select(command => string.Join("|", command.phrases));
                var pattern = string.Join("|", groups.Select(i => $"({i})"));
                _regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                _undecoded = string.Empty;
                
                _consumer.onConsume = Decode;
                _consumer.Start();
            }

            public void Enqueue(string command)
            {
                _consumer.Feed(command);
            }

            public bool TryDequeue(out SpokenEvent onSpoken)
            {
                return _outputQueue.TryDequeue(out onSpoken);
            }

            public void Stop()
            {
                _consumer.Stop();
            }

            private void Decode(string text)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                if (!string.IsNullOrEmpty(_undecoded))
                {
                    text = $"{_undecoded} {text}";
                }

                var lastDecodedCharPos = 0;

                foreach (Match match in _regex.Matches(text))
                {
                    var token = match.Value;
                    var groups = match.Groups;
                    
                    lastDecodedCharPos = match.Index + token.Length;
                    
                    for (var i = 1; i < groups.Count; i++)
                    {
                        var value = groups[i].Value;

                        if (token.Equals(value, StringComparison.Ordinal))
                        {
                            var groupIndex = i - 1;
                            if (groupIndex < _events.Length)
                            {
                                _outputQueue.Enqueue(_events[groupIndex]);
                            }

                            break;
                        }
                    }
                }
                
                _undecoded = text.Substring(lastDecodedCharPos);
            }
        }
    }
}