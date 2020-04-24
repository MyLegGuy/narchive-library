﻿using System.Collections.Generic;

namespace Narchive.Formats
{
    public class NarcArchiveDirectoryEntry : NarcArchiveEntry
    {
        /// <summary>
        /// Gets the relative path of the entry.
        /// </summary>
        public override string FullName => Parent != null
            ? System.IO.Path.Combine(Parent.FullName, Name)
            : Name;

        /// <summary>
        /// Gets the entries in the directory.
        /// </summary>
        public List<NarcArchiveEntry> Entries { get; set; } = new List<NarcArchiveEntry>();

        /// <summary>
        /// Gets or sets the offset of the directory name in the name entry table.
        /// </summary>
        internal int NameEntryOffset { get; set; }

        /// <summary>
        /// Gets or sets the index of the first file in the directory.
        /// </summary>
        internal int FirstFileIndex { get; set; }
    }
}
