using UnityEngine;
using Vosk;

namespace Recognissimo.Components
{
    /// <summary>
    ///     Base class for all model providers
    /// </summary>
    public abstract class ModelProvider : MonoBehaviour
    {
        /// <summary>
        ///     Language model instance. The parameter is read once at the start of recognition
        /// </summary>
        public virtual Model Model { get; protected set; }

        /// <summary>
        ///     Helper method to create language model from path provided. Model instantiating is time consuming.
        ///     Prefer this over direct model instantiation as it handles native exceptions
        /// </summary>
        /// <param name="path">The path to the directory containing model files</param>
        /// <returns>Model instance</returns>
        protected static Model CreateModel(string path)
        {
            try
            {
                return new Model(path);
            }
            catch
            {
                return null;
            }
        }
    }
}