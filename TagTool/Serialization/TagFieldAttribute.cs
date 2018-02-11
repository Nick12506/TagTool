using TagTool.Cache;
using System;
using System.Runtime.InteropServices;

namespace TagTool.Serialization
{
    /// <summary>
    /// Attribute for automatically-serializable values in a tag.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class TagFieldAttribute : Attribute
    {
        /// <summary>
        /// The offset of the value from the beginning of the structure.
        /// If this is -1 (default), then the current stream position will be used.
        /// </summary>
        public int Offset { get; set; } = -1;

        /// <summary>
        /// The size of the value in bytes.
        /// If this is 0 (default), then the size will be inferred from the value type.
        /// </summary>
        public uint Size { get; set; }

        /// <summary>
        /// If the value is an inline array, determines the number of elements in the array.
        /// If the value is a string, determines the maximum number of characters in the string (including the null terminator).
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// If the field is a string, determines the character set of the string.
        /// </summary>
        public CharSet CharSet { get; set; } = CharSet.Ansi;

        /// <summary>
        /// Determines if the tag field is a padding value.
        /// </summary>
        public bool Padding { get; set; } = false;
        
        /// <summary>
        /// Determines if the tag field is a pointer to its actual value.
        /// </summary>
        public bool Pointer { get; set; } = false;

        /// <summary>
        /// The power of two to align the field's data to.
        /// Only applicable to fields which contain pointers.
        /// </summary>
        public uint Align { get; set; } = 0;

        /// <summary>
        /// Determines if the tag field is in its "short" format.
        /// </summary>
        public bool Short { get; set; } = false;

        /// <summary>
        /// Determines if the tag field is part of the tag data, or used for tool purposes only.
        /// </summary>
        public bool Local { get; set; } = false;

        /// <summary>
        /// The minimum cache version the tag field is present in.
        /// </summary>
        public CacheVersion MinVersion { get; set; } = CacheVersion.Unknown;

        /// <summary>
        /// The maximum cache version the tag field is present in.
        /// </summary>
        public CacheVersion MaxVersion { get; set; } = CacheVersion.Unknown;

        /// <summary>
        /// If the field is a tag reference, an array of valid group tags, or any if null.
        /// </summary>
        public string[] ValidTags { get; set; } = null;

        /// <summary>
        /// If the field is a string id, determines if it is used as a label.
        /// </summary>
        public bool Label { get; set; } = false;
    }
}