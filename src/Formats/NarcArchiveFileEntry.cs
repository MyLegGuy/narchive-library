using System.IO;
namespace Narchive.Formats
{
    public class NarcArchiveFileEntry : NarcArchiveEntry
    {
        /// <summary>
        /// Gets the relative path of the entry.
        /// </summary>
        public override string FullName => Parent != null
            ? System.IO.Path.Combine(Parent.FullName, Name)
            : Name;

        /// <summary>
        /// Gets or sets the offset of the file data.
        /// </summary>
        internal int Offset { get; set; }

        /// <summary>
        /// Gets or sets the length of the file data.
        /// </summary>
        internal int Length { get; set; }

		public Stream dataStream;
    }
}
