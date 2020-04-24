﻿namespace Narchive.Formats
{
    public abstract class NarcArchiveEntry
    {
        /// <summary>
        /// Gets or sets the index of the entry.
        /// </summary>
        public virtual int Index { get; set; }

        /// <summary>
        /// Gets or sets the name of the entry.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Gets the relative path of the entry.
        /// </summary>
        public abstract string FullName { get; }

		/// <summary>
        /// Gets or sets the parent directory.
        /// </summary>
        public virtual NarcArchiveDirectoryEntry Parent { get; set; }
		
        /// <summary>
        /// Gets or sets the path, on the filesystem, of the entry.
        /// </summary>
        //public string Path { get; set; }
    }
}
