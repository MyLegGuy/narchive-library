using System.Collections.Generic;
using System.IO;

namespace Narchive.Formats
{
    public class NarcArchiveRootDirectoryEntry : NarcArchiveDirectoryEntry
    {
        /// <summary>
        /// Gets the index of the root directory, which is always 0.
        /// </summary>
        public override int Index => 0;

        /// <summary>
        /// Gets the name of the root directory, which is always an empty string.
        /// </summary>
        public override string Name => string.Empty;

        /// <summary>
        /// Gets the parent directory, which is always null for the root directory.
        /// </summary>
        public override NarcArchiveDirectoryEntry Parent => null;

        /// <summary>
        /// Initializes an instance of <see cref="NarcArchiveRootDirectoryEntry"/>.
        /// </summary>
        public NarcArchiveRootDirectoryEntry()
        {
        }
    }
}
