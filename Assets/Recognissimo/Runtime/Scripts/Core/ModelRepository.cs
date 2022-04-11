using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Recognissimo.Utils;
using UnityEngine;

namespace Recognissimo.Core
{
    /// <summary>
    /// Model information
    /// </summary>
    [Serializable]
    public struct ModelInfo
    {
        /// <summary>
        /// Model name
        /// </summary>
        public string name;
        
        /// <summary>
        /// Model ID
        /// </summary>
        public string id;
        
        /// <summary>
        /// Path to the model files
        /// </summary>
        public string path;
        
        /// <summary>
        /// User information 
        /// </summary>
        public string tag;
    }
    
    /// <summary>
    /// Repository for storing language models
    /// </summary>
    public class ModelRepository
    {
        private const string DefaultModelName = "Model";
        private const string ConfigFileName = ".models.json";

        private readonly string _configFilePath;
        private readonly string _libraryPath;

        private List<ModelInfo> _models;

        /// <summary>
        /// Load existing or create new repository in the specified folder.
        /// Settings file is created during initialization 
        /// </summary>
        /// <param name="libraryPath">Repository directory path</param>
        public ModelRepository(string libraryPath)
        {
            _libraryPath = Path.GetFullPath(libraryPath);
            
            if (!Directory.Exists(_libraryPath))
            {
                Directory.CreateDirectory(_libraryPath);
            }

            _configFilePath = Path.Combine(_libraryPath, ConfigFileName);
            LoadConfig();
        }

        /// <summary>
        /// Get <see cref="ModelInfo"/> for existing models
        /// </summary>
        /// <returns>ModelInfo enumerable (empty if no models loaded)</returns>
        public IEnumerable<ModelInfo> Models()
        {
            return _models.AsReadOnly();
        }

        /// <summary>
        /// Change tag of existing model
        /// </summary>
        /// <param name="id">Model ID</param>
        /// <param name="tag">New tag</param>
        public void SetTag(string id, string tag)
        {
            ModelInfo TagSetter(ModelInfo info)
            {
                info.tag = tag;
                return info;
            }

            Set(id, TagSetter);
        }

        /// <summary>
        /// Change name of existing model
        /// </summary>
        /// <param name="id">Model ID</param>
        /// <param name="name">New name</param>
        public void SetName(string id, string name)
        {
            ModelInfo NameSetter(ModelInfo info)
            {
                info.name = name;
                return info;
            }

            Set(id, NameSetter);
        }

        /// <summary>
        /// Remove model from list of models. It doesn't remove local files
        /// </summary>
        /// <param name="id">Model ID</param>
        public void Remove(string id)
        {
            var index = FindIndexById(id);
            _models.RemoveAt(index);
            UpdateConfigFile();
        }

        /// <summary>
        /// Install model from model source. Model files will be unpacked into repository folder
        /// </summary>
        /// <param name="modelSource">Model source</param>
        /// <returns><see cref="ModelInfo"/> of installed model</returns>
        public ModelInfo InstallModel(IModelSource modelSource)
        {
            var modelName = modelSource.ModelName;

            if (string.IsNullOrEmpty(modelName))
            {
                modelName = DefaultModelName;
            }

            var modelPath = Path.Combine(_libraryPath, modelName);
            modelSource.SaveTo(modelPath);

            return AddExistingModel(modelPath, modelName);
        }

        /// <summary>
        /// Add model from local folder. Doesn't move files
        /// </summary>
        /// <param name="modelPath">Model folder</param>
        /// <param name="modelName">Model name</param>
        /// <returns><see cref="ModelInfo"/> of installed model</returns>
        /// <exception cref="DirectoryNotFoundException">Model folder not found</exception>
        /// <exception cref="FileNotFoundException">Model files not found in specified folder</exception>
        public ModelInfo AddExistingModel(string modelPath, string modelName)
        {
            if (!Directory.Exists(modelPath))
            {
                throw new DirectoryNotFoundException($"Model folder {modelPath} not found");
            }
            
            if (!ContainsValidModelFiles(modelPath))
            {
                throw new FileNotFoundException($"Model folder {modelPath} lacks required files");
            }

            var info = new ModelInfo {name = modelName, path = modelPath, id = Guid.NewGuid().ToString()};
            _models.Add(info);

            UpdateConfigFile();

            return info;
        }

        private void LoadConfig()
        {
            using (var stream = new StreamReader(File.Open(_configFilePath, FileMode.OpenOrCreate)))
            {
                LoadConfigFromJson(stream.ReadToEnd());
            }

            CleanupConfig();
            UpdateConfigFile();
        }

        private void LoadConfigFromJson(string json)
        {
            var serializableList = JsonUtility.FromJson<SerializableList<ModelInfo>>(json);
            _models = serializableList?.list ?? new List<ModelInfo>();
        }

        private void CleanupConfig()
        {
            _models.RemoveAll(info => !Directory.Exists(info.path) || !ContainsValidModelFiles(info.path));
        }

        private void UpdateConfigFile()
        {
            var serializableList = new SerializableList<ModelInfo>() {list = _models};
            var json = JsonUtility.ToJson(serializableList);
            File.WriteAllText(_configFilePath, json);
        }

        private void Set(string id, Func<ModelInfo, ModelInfo> setter)
        {
            var index = FindIndexById(id);
            _models[index] = setter(_models[index]);
            UpdateConfigFile();
        }

        private int FindIndexById(string id)
        {
            var index = _models.FindIndex(info => info.id.Equals(id));
            
            if (index == -1)
            {
                throw new KeyNotFoundException($"Bad ID {id} provided");
            }
            
            return index;
        }

        private static bool ContainsValidModelFiles(string modelPath)
        {
            const string mdlV1Path = "final.mdl";
            const string confV1Path = "mfcc.conf";
            const string mdlV2Path = "am/final.mdl";
            const string confV2Path = "conf/mfcc.conf";

            string[] v1Files = {mdlV1Path, confV1Path};
            string[] v2Files = {mdlV2Path, confV2Path};
            string[][] versions = {v1Files, v2Files};

            bool ModelFilesExist(IEnumerable<string> modelFiles)
            {
                return modelFiles
                    .Select(filePath => Path.Combine(modelPath, filePath))
                    .All(File.Exists);
            }

            return versions.Any(ModelFilesExist);
        }
    }
}