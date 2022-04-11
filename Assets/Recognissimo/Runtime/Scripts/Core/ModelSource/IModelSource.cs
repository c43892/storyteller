namespace Recognissimo.Core
{
    /// <summary>
    /// Interface for model source
    /// </summary>
    public interface IModelSource
    {
        /// <summary>
        /// Model name
        /// </summary>
        string ModelName { get; }
        
        /// <summary>
        /// Extract model files to specified folder
        /// </summary>
        /// <param name="to">Extract path</param>
        void SaveTo(string to);
    }
}