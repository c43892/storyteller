using System;
using System.Collections.Generic;

namespace Recognissimo.Core
{
    public interface IResult
    {
    }

    /// <summary>
    ///     Partial speech recognition result which may change as recognizer process more data
    /// </summary>
    [Serializable]
    public struct PartialResult : IResult
    {
        /// <summary>
        ///     Decoded text
        /// </summary>
        public string partial;
    }

    /// <summary>
    ///     Speech recognition result
    /// </summary>
    [Serializable]
    public struct Result : IResult
    {
        /// <summary>
        ///     Detailed description of decoded word
        /// </summary>
        [Serializable]
        public struct Word
        {
            /// <summary>
            ///     Confidence (from zero to one)
            /// </summary>
            public float conf;

            /// <summary>
            ///     Start time of the word (seconds)
            /// </summary>
            public float start;

            /// <summary>
            ///     End time of the word (seconds)
            /// </summary>
            public float end;

            /// <summary>
            ///     Decoded word
            /// </summary>
            public string word;
        }

        /// <summary>
        ///     Detailed description of decoded text
        /// </summary>
        public List<Word> result;

        /// <summary>
        ///     Decoded text
        /// </summary>
        public string text;

        [Serializable]
        public struct Alternative
        {
            /// <summary>
            ///     Decoded text
            /// </summary>
            public string text;
        }

        /// <summary>
        ///     List of all possible recognition results. Sorted in descending order of confidence
        /// </summary>
        public List<Alternative> alternatives;
    }
}