#if !UNITY_EDITOR && UNITY_ANDROID
#define UNITY_STANDALONE_ANDROID
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Recognissimo.Core;
using UnityEngine;
using Vosk;

namespace Recognissimo.Components
{
    /// <summary>
    ///     Model provider for different languages
    /// </summary>
    [AddComponentMenu("Recognissimo/Model Providers/Language Model Provider")]
    public class LanguageModelProvider : ModelProvider
    {
        private string _obbPath;
        private string _persistentDataPath;
        private ModelRepository _repository;
        private string _streamingAssetsPath;

        /// <summary>
        ///     List of available models
        /// </summary>
        public List<ModelStreamingAssetsPath> speechModels;
        
        /// <summary>
        ///     Whether to start initialization as soon as the component awakes.
        ///     If selected, <see cref="LanguageModelProvider.defaultLanguage"/> will be used
        /// </summary>
        public bool setupOnAwake;

        /// <summary>
        ///     Language loaded by default if <see cref="LanguageModelProvider.setupOnAwake"/> is used
        /// </summary>
        [HideInInspector]
        public SystemLanguage defaultLanguage = SystemLanguage.English;
        
        /// <inheritdoc />
        public override Model Model { get; protected set; }

        /// <summary>
        ///     Loads the model of the selected language and saves it to the <see cref="LanguageModelProvider.Model"/>.
        ///     Time-consuming
        /// </summary>
        /// <param name="language">Language of new model</param>
        public void LoadLanguageModel(SystemLanguage language)
        {
            if (_repository == null)
            {
                return;
            }

            if (!speechModels.Any(model => model.language.Equals(language)))
            {
                Debug.LogError($"{language.ToString()} Language not supported");
                return;
            }
            
            var modelTag = CreateModelTagForLanguage(language);

            if (!TryFindModelInfoByModelTag(modelTag, out var modelInfo))
            {
                modelInfo = InstallLanguageModel(language);
                _repository.SetTag(modelInfo.id, JsonUtility.ToJson(modelTag));
            }

            Model = CreateModel(modelInfo.path);
        }

        /// <summary>
        ///     <see cref="LoadLanguageModel"/> async variant
        /// </summary>
        /// <param name="language">Language of new model</param>
        /// <returns>Task object</returns>
        public async Task LoadLanguageModelAsync(SystemLanguage language)
        {
            await Task.Run(() => { LoadLanguageModel(language); });
        }

        /// <summary>
        ///     Initialize state, load and check models. Time-consuming
        /// </summary>
        public void Initialize()
        {
            _repository = new ModelRepository(_persistentDataPath);
        }

        /// <summary>
        ///     <see cref="Initialize"/> async variant
        /// </summary>
        /// <returns>Task object</returns>
        public async Task InitializeAsync()
        {
            await Task.Run(Initialize);
        }

        private void Awake()
        {
            _obbPath = Application.dataPath;
            _persistentDataPath = Application.persistentDataPath;
            _streamingAssetsPath = Application.streamingAssetsPath;

            if (setupOnAwake)
            {
                Initialize();
                LoadLanguageModel(defaultLanguage);
                if (Model == null)
                {
                    Debug.LogError("Failed to initialise model");
                }
            }
        }

        private bool TryFindModelInfoByModelTag(ModelTag modelTag, out ModelInfo modelInfo)
        {
            modelInfo = _repository.Models().FirstOrDefault(model => modelTag.Equals(model.tag));
            return !modelInfo.Equals(default(ModelInfo));
        }

        private string StreamingAssetsLanguageModelPath(SystemLanguage language)
        {
            return speechModels.Find(model => model.language == language).modelPath;
        }

        private string AbsoluteLanguageModelPath(SystemLanguage language)
        {
            return Path.Combine(_streamingAssetsPath, StreamingAssetsLanguageModelPath(language));
        }

        private ModelTag CreateModelTagForLanguage(SystemLanguage language)
        {
#if UNITY_STANDALONE_ANDROID
            long lastWriteTime =
                ((DateTimeOffset) File.GetLastWriteTime(_streamingAssetsPath)).ToUnixTimeMilliseconds();
#else
            var modelPath = AbsoluteLanguageModelPath(language);
            var lastWriteTime = ((DateTimeOffset) Directory.GetLastWriteTime(modelPath)).ToUnixTimeMilliseconds();
#endif
            return new ModelTag {language = language, lastWriteTime = lastWriteTime};
        }

        private ModelInfo InstallLanguageModel(SystemLanguage language)
        {
#if UNITY_STANDALONE_ANDROID
            var modelSource = new ZipModelSource(_obbPath, "assets/" + StreamingAssetsLanguageModelPath(language));
            return _repository.InstallModel(modelSource);
#else
            var modelPath = AbsoluteLanguageModelPath(language);
            return _repository.AddExistingModel(modelPath, $"Model {language.ToString()}");
#endif
        }

        /// <summary>
        ///     Additional model info
        /// </summary>
        [Serializable]
        public struct ModelTag
        {
            /// <summary>
            ///     Language of the model
            /// </summary>
            public SystemLanguage language;

            /// <summary>
            ///     Time the model was installed. Field is used on Android to avoid model re-extraction from OBB
            /// </summary>
            public long lastWriteTime;

            public bool Equals(string json)
            {
                return Equals(JsonUtility.FromJson<ModelTag>(json));
            }
        }

        /// <summary>
        ///     Model language/path pair
        /// </summary>
        [Serializable]
        public struct ModelStreamingAssetsPath
        {
            /// <summary>
            ///     Language of the model
            /// </summary>
            public SystemLanguage language;

            /// <summary>
            ///     Path relative to StreamingAssets folder
            /// </summary>
            public string modelPath;
        }
    }
}