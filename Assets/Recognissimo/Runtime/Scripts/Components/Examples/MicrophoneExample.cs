using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Recognissimo.Core;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

namespace Recognissimo.Components.Examples
{
    [AddComponentMenu("Recognissimo/Examples/Microphone Example")]
    public class MicrophoneExample : MonoBehaviour
    {
        private const string InitializationMessage = "Loading speech model and setup recognizer...";
        private const string GreetingMessage = "Press 'Space' or tap to start/stop recognition";
        private const string RecognitionStartedMessage = "Recognizing...";
        private const string RecognizerCrashedMessage = "Recognizer crashed";

        private SystemLanguage _language;
        private bool _ready;
        private RecognizedText _recognizedText;

        [SerializeField] private Dropdown languageDropdown;
        [SerializeField] private MicrophoneSpeechSource micSource;
        [SerializeField] private LanguageModelProvider modelProvider;
        [SerializeField] private SpeechRecognizer recognizer;
        [SerializeField] private Text text;

        public SystemLanguage defaultLanguage = SystemLanguage.Unknown; 
        
        private void Awake()
        {
            _language = defaultLanguage != SystemLanguage.Unknown
                ? defaultLanguage
                : Application.systemLanguage;
            
            _recognizedText = new RecognizedText();
        }
        
        private async void Start()
        {
            text.text = InitializationMessage;
            
            try
            {
                await InitPlatformPermissions();                
                InitMicSource();
                InitLanguage();
                await InitModelProvider();
                InitLanguageDropdown();
                InitRecognizer();
                OnInitialized();
            }
            catch (Exception e)
            {
                OnError(e.Message);
            }
        }

        private void Update()
        {
            if (!_ready)
            {
                return;
            }
            
            var spaceReleased = Input.GetKeyUp(KeyCode.Space);
            var touchEnded = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended;
            
            if (!spaceReleased && !touchEnded)
            {
                return;
            }

            if (recognizer.IsRecognizing)
            {
                OnStop();
            }
            else
            {
                OnStart();
            }
        }

        private static async Task InitPlatformPermissions()
        {
#if UNITY_IOS
            await PlatformPermissions.RequestIOSPermissions();
#elif UNITY_ANDROID
            PlatformPermissions.RequestAndroidPermissions();
#endif
            await Task.CompletedTask;
        }
        
        private void InitMicSource()
        {
            micSource.microphoneSettings.deviceIndex = 0;
            micSource.microphoneSettings.sampleRate = 16000;
            micSource.StartMicrophone();
        }
        
        private void InitLanguage()
        {
            if (modelProvider.speechModels.Any(info => info.language == _language))
            {
                return;
            }
            
            const SystemLanguage fallbackLanguage = SystemLanguage.English;

            Debug.LogWarning($"Fallback from {_language.ToString()} to {fallbackLanguage}");

            _language = fallbackLanguage;
        }
        
        private async Task InitModelProvider()
        {
            await modelProvider.InitializeAsync();
            await modelProvider.LoadLanguageModelAsync(_language);
        }

        private void InitLanguageDropdown()
        {
            languageDropdown.options = modelProvider.speechModels
                .Select(info => new Dropdown.OptionData {text = info.language.ToString()})
                .ToList();

            languageDropdown.value =
                languageDropdown.options.FindIndex(option => option.text == _language.ToString());

            languageDropdown.onValueChanged.AddListener(index =>
            {
                var optionText = languageDropdown.options[index].text;
                var selectedLanguage = (SystemLanguage) Enum.Parse(typeof(SystemLanguage), optionText);
                OnLanguageChanged(selectedLanguage);
            });
        }

        private void InitRecognizer()
        {
            recognizer.speechSource = micSource;
            recognizer.modelProvider = modelProvider;
            recognizer.partialResultReady.AddListener(OnPartialResultReady);
            recognizer.resultReady.AddListener(OnResultReady);
            recognizer.crashed.AddListener(OnCrashed);
        }

        private void UpdateUiText()
        {
            text.text = _recognizedText.CurrentText;
        }

        private void OnStart()
        {
            _recognizedText.Clear();

            text.text = RecognitionStartedMessage;
            recognizer.modelProvider = modelProvider;

            try
            {
                recognizer.StartRecognition();
            }
            catch (Exception e)
            {
                text.text = e.Message;
            }
        }

        private void OnStop()
        {
            text.text = GreetingMessage;
            recognizer.StopRecognition();
        }

        private void OnInitialized()
        {
            text.text = GreetingMessage;
            _ready = true;
        }

        private void OnError(string error)
        {
            if (text != null)
            {
                text.text = error;
            }
        }

        private void OnCrashed()
        {
            OnError(RecognizerCrashedMessage);
        }

        private async void OnLanguageChanged(SystemLanguage language)
        {
            _ready = false;

            recognizer.StopRecognition();
            text.text = InitializationMessage;
            await modelProvider.LoadLanguageModelAsync(language);
            OnInitialized();
        }

        private void OnResultReady(Result result)
        {
            _recognizedText.Add(result);
            UpdateUiText();
        }

        private void OnPartialResultReady(PartialResult partialResult)
        {
            _recognizedText.Add(partialResult);
            UpdateUiText();
        }

        private class RecognizedText
        {
            private string _changingText;
            private string _stableText;

            public string CurrentText => $"{_stableText} <color=grey>{_changingText}</color>";

            public void Add(Result result)
            {
                _changingText = "";
                _stableText = $"{_stableText} {result.text}";
            }

            public void Add(PartialResult partialResult)
            {
                _changingText = partialResult.partial;
            }

            public void Clear()
            {
                _changingText = "";
                _stableText = "";
            }
        }

        private static class PlatformPermissions
        {
            public static async Task RequestIOSPermissions()
            {
                var result = Application.RequestUserAuthorization(UserAuthorization.Microphone);
                var isComplete = new TaskCompletionSource<bool>();
                result.completed += operation =>
                {
                    if (operation.isDone)
                    {
                        isComplete.SetResult(operation.isDone);
                    }
                    else
                    {
                        isComplete.SetException(new InvalidOperationException("Microphone access denied"));
                    }
                };

                await isComplete.Task;
            }

            public static void RequestAndroidPermissions()
            {
                var requestedPermissions = new List<string>
                    {Permission.Microphone, Permission.ExternalStorageWrite, Permission.ExternalStorageRead};

                List<string> FindMissingPermissions() =>
                    requestedPermissions.FindAll(permission => !Permission.HasUserAuthorizedPermission(permission));

                FindMissingPermissions().ForEach(Permission.RequestUserPermission);

                if (FindMissingPermissions().Count > 0)
                {
                    throw new InvalidOperationException("Permission request failed");
                }
            }
        }
    }
}