using Narchive.Exceptions;
using Narchive.IO;
using Narchive.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Narchive.Formats
{
    public class NarcArchive
    {
		// add it or return it if it's there
		private static NarcArchiveDirectoryEntry insertFolderInto(NarcArchiveDirectoryEntry _d, string _searchName){
			for (int i=0;i<_d.Entries.Count;++i){
				if (_d.Entries[i].Name==_searchName && _d.Entries[i] is NarcArchiveDirectoryEntry){
					return (NarcArchiveDirectoryEntry)_d.Entries[i];
				}
			}
			NarcArchiveDirectoryEntry _newEntry = new NarcArchiveDirectoryEntry();
			_newEntry.Name=_searchName;
			_newEntry.Parent=_d;
			_d.Entries.Add(_newEntry);
			return _newEntry;
		}
		public static void create(string[] _inNames, Stream[] _inStreams, string outputPath){
			bool _usingFilenames = (_inNames!=null);
			NarcArchiveRootDirectoryEntry _root = new NarcArchiveRootDirectoryEntry();
			if (_usingFilenames){
				// make all the parent directory thingies and then shove the file entries into them.
				for (int i=0;i<_inStreams.Length;++i){
					NarcArchiveDirectoryEntry _curParent = _root;
					int _startSearchIndex=0;
					while(true){
						int _nextSlashIndex = _inNames[i].IndexOf('/',_startSearchIndex,_inNames[i].Length-_startSearchIndex);
						if (_nextSlashIndex!=-1){
							String _curFolderName = _inNames[i].Substring(_startSearchIndex,_nextSlashIndex-_startSearchIndex);
							_curParent = insertFolderInto(_curParent,_curFolderName);
							_startSearchIndex=_nextSlashIndex+1;
						}else{
							break;
						}
					}
					_curParent.Entries.Add(new NarcArchiveFileEntry{dataStream=_inStreams[i], Name=Path.GetFileName(_inNames[i])});
				}
			}else{
				for (int i=0;i<_inStreams.Length;++i){
					_root.Entries.Add(new NarcArchiveFileEntry{dataStream=_inStreams[i]});
				}
			}
			lowCreate(_root,outputPath,_usingFilenames);
		}
        public static void lowCreate(NarcArchiveRootDirectoryEntry rootDirectory, string outputPath, bool hasFilenames = true)
        {
            var directories = new List<NarcArchiveDirectoryEntry>
            {
                rootDirectory,
            };
            var files = new List<NarcArchiveFileEntry>();

            var directoryIndex = 1; // The root directory index is always 0.
            var fileIndex = 0;
            var position = 0;
            for (var i = 0; i < directories.Count; i++) // We'll be modifying directories during the loop, so we can't use a foreach loop here.
            {
                foreach (var entry in directories[i].Entries)
                {
                    if (entry is NarcArchiveDirectoryEntry directoryEntry)
                    {
                        directoryEntry.Index = directoryIndex;
                        directoryEntry.FirstFileIndex = fileIndex;

                        directories.Add(directoryEntry);

                        directoryIndex++;
                    }
                    else if (entry is NarcArchiveFileEntry fileEntry)
                    {
                        var length = (int)fileEntry.dataStream.Length;

                        fileEntry.Index = fileIndex;
                        fileEntry.Offset = position;
                        fileEntry.Length = length;

                        position += ((length + 3) / 4) * 4; // Offsets must be a multiple of 4

                        files.Add(fileEntry);

                        fileIndex++;
                    }
                }
            }
            var fimgLength = position;

            using (var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(output))
            {
				// Write out the NARC header
				writer.Write((byte)'N');
				writer.Write((byte)'A');
				writer.Write((byte)'R');
				writer.Write((byte)'C');
				writer.Write((byte)0xFE);
				writer.Write((byte)0xFF);
				writer.Write((byte)0);
				writer.Write((byte)1);

				writer.Write(0); // File length (will be written to later)

				writer.Write((short)16); // Header length (always 16)
				writer.Write((short)3); // Number of sections (always 3)

				// Write out the FATB section
				writer.Write((byte)'B');
				writer.Write((byte)'T');
				writer.Write((byte)'A');
				writer.Write((byte)'F');

				writer.Write(12 + (files.Count * 8)); // Section length
				writer.Write(files.Count); // Number of file entries

				foreach (var file in files)
				{
					writer.Write(file.Offset); // Start position
					writer.Write(file.Offset + file.Length); // End position
				}

				// Write out the FNTB section
				writer.Write((byte)'B');
				writer.Write((byte)'T');
				writer.Write((byte)'N');
				writer.Write((byte)'F');

				if (hasFilenames)
				{
					var fntbPosition = (int)output.Position - 4;

					writer.Write(0); // Section length (will be written to later)

					writer.Write(0); // Name entry offset for the root directory (will be written to later)
					writer.Write((short)0); // First file index (always 0)
					writer.Write((short)directories.Count); // Number of directories, including the root directory

					for (var i = 1; i < directories.Count; i++)
					{
						writer.Write(0); // Name entry offset for this directory (will be written to later)
						writer.Write((short)directories[i].FirstFileIndex); // Index of the first file in this directory
						writer.Write((short)(directories[i].Parent.Index | 0xF000)); // Parent directory
					}

					position = directories.Count * 8;
					foreach (var directory in directories)
					{
						directory.NameEntryOffset = position;

						foreach (var entry in directory.Entries)
						{
							var nameAsBytes = Encoding.UTF8.GetBytes(entry.Name);

							if (entry is NarcArchiveDirectoryEntry directoryEntry)
							{
								writer.Write((byte)(nameAsBytes.Length | 0x80)); // Length of the directory name
								writer.Write(nameAsBytes);
								writer.Write((short)(directoryEntry.Index | 0xF000));

								position += nameAsBytes.Length + 3;
							}
							else if (entry is NarcArchiveFileEntry fileEntry)
							{
								writer.Write((byte)nameAsBytes.Length); // Length of the file name
								writer.Write(nameAsBytes);

								position += nameAsBytes.Length + 1;
							}
						}

						writer.Write((byte)0);

						position++;
					}

					while (output.Length % 4 != 0)
					{
						writer.Write((byte)0xFF);
					}

					var fntbLength = (int)output.Position - fntbPosition;

					// Go back and write the name entry offsets for each directory
					output.Position = fntbPosition + 4;
					writer.Write(fntbLength);
					foreach (var directory in directories)
					{
						writer.Write(directory.NameEntryOffset);
						output.Position += 4;
					}
					output.Position = fntbPosition + fntbLength;
				}
				else
				{
					// The FNTB section is always the same if there are no filenames
					writer.Write(16); // Section length (always 16)
					writer.Write(4); // Always 4
					writer.Write((short)0); // First file index (always 0)
					writer.Write((short)1); // Number of directories, including the root directory (always 1)
				}

				// Write out the FIMG section
				writer.Write((byte)'G');
				writer.Write((byte)'M');
				writer.Write((byte)'I');
				writer.Write((byte)'F');

				writer.Write(fimgLength + 8); // Section length

				foreach (var file in files)
				{
					file.dataStream.Seek(0,SeekOrigin.Begin);
					file.dataStream.CopyTo(output);   

					while (output.Length % 4 != 0)
					{
						writer.Write((byte)0xFF);
					}
				}

				// Go back and write out the file length
				output.Position = 8;
				writer.Write((int)output.Length); // File length
				output.Position = output.Length;
            }
        }

		// returned names will be like "tmp/fileinsideofdirectory" or "filename"
        public static Tuple<string[],MemoryStream[]> extract(string inputPath, bool ignoreFilenames = false)
        {
            using (var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(input))
            {
                // Read the NARC header
                if (!(reader.ReadByte() == 'N'
                    && reader.ReadByte() == 'A'
                    && reader.ReadByte() == 'R'
                    && reader.ReadByte() == 'C'
                    && reader.ReadByte() == 0xFE
                    && reader.ReadByte() == 0xFF
                    && reader.ReadByte() == 0
                    && reader.ReadByte() == 1
                    && reader.ReadInt32() == input.Length))
                {
                    throw new InvalidFileTypeException(string.Format(ErrorMessages.NotANarcFile, Path.GetFileName(inputPath)));
                }

                var headerLength = reader.ReadInt16();
                var fatbPosition = headerLength;

                // Read the FATB section
                input.Position = fatbPosition;
                if (!(reader.ReadByte() == 'B'
                    && reader.ReadByte() == 'T'
                    && reader.ReadByte() == 'A'
                    && reader.ReadByte() == 'F'))
                {
                    throw new InvalidFileTypeException(string.Format(ErrorMessages.NotANarcFile, Path.GetFileName(inputPath)));
                }

                var fatbLength = reader.ReadInt32();
                var fntbPosition = fatbPosition + fatbLength;

                var fileEntryCount = reader.ReadInt32();
                var fileEntries = new List<NarcArchiveFileEntry>(fileEntryCount);
                for (var i = 0; i < fileEntryCount; i++)
                {
                    var offset = reader.ReadInt32();
                    var length = reader.ReadInt32() - offset;
                    fileEntries.Add(new NarcArchiveFileEntry
                    {
                        Offset = offset,
                        Length = length,
                    });
                }

                // Read the FNTB section
                input.Position = fntbPosition;
                if (!(reader.ReadByte() == 'B'
                    && reader.ReadByte() == 'T'
                    && reader.ReadByte() == 'N'
                    && reader.ReadByte() == 'F'))
                {
                    throw new InvalidFileTypeException(string.Format(ErrorMessages.NotANarcFile, Path.GetFileName(inputPath)));
                }
                
                var fntbLength = reader.ReadInt32();
                var fimgPosition = fntbPosition + fntbLength;

                var hasFilenames = !ignoreFilenames;

                // If the FNTB length is 16 or less, it's impossible for the entries to have filenames.
                // This section will always be at least 16 bytes long, but technically it's only required to be at least 8 bytes long.
                if (fntbLength <= 16)
                {
                    hasFilenames = false;
                }

                var rootNameEntryOffset = reader.ReadInt32();

                // If the root name entry offset is 4, then the entries don't have filenames.
                if (rootNameEntryOffset == 4)
                {
                    hasFilenames = false;
                }

                if (hasFilenames)
                {
                    var rootFirstFileIndex = reader.ReadInt16();
                    var rootDirectory = new NarcArchiveRootDirectoryEntry();

                    var directoryEntryCount = reader.ReadInt16(); // This includes the root directory
                    var directoryEntries = new List<NarcArchiveDirectoryEntry>(directoryEntryCount)
                    {
                        rootDirectory,
                    };

                    // This NARC contains filenames and directory names, so read them
                    for (var i = 1; i < directoryEntryCount; i++)
                    {
                        var nameEntryTableOffset = reader.ReadInt32();
                        var firstFileIndex = reader.ReadInt16();
                        var parentDirectoryIndex = reader.ReadInt16() & 0xFFF;

                        directoryEntries.Add(new NarcArchiveDirectoryEntry
                        {
                            Index = i,
                            Parent = directoryEntries[parentDirectoryIndex],
                            NameEntryOffset = nameEntryTableOffset,
                            FirstFileIndex = firstFileIndex,
                        });
                    }

                    NarcArchiveDirectoryEntry currentDirectory = rootDirectory;
                    var directoryIndex = 0;
                    var fileIndex = 0;
                    while (directoryIndex < directoryEntryCount)
                    {
                        var entryNameLength = reader.ReadByte();
                        if ((entryNameLength & 0x80) != 0)
                        {
                            // This is a directory name entry
                            var entryName = reader.ReadString(entryNameLength & 0x7F);
                            var entryDirectoryIndex = reader.ReadInt16() & 0xFFF;
                            var directoryEntry = directoryEntries[entryDirectoryIndex];

                            directoryEntry.Name = entryName;
                        }
                        else if (entryNameLength != 0)
                        {
                            // This is a file name entry
                            var entryName = reader.ReadString(entryNameLength);
                            var fileEntry = fileEntries[fileIndex];

                            fileEntry.Parent = directoryEntries[directoryIndex];
                            fileEntry.Name = entryName;

                            fileIndex++;
                        }
                        else
                        {
                            // This is the end of a directory
                            directoryIndex++;
                            if (directoryIndex >= directoryEntryCount)
                            {
                                break;
                            }
                            currentDirectory = directoryEntries[directoryIndex];
                        }
                    }
                }

                // Read the FIMG section
                input.Position = fimgPosition;
                if (!(reader.ReadByte() == 'G'
                    && reader.ReadByte() == 'M'
                    && reader.ReadByte() == 'I'
                    && reader.ReadByte() == 'F'))
                {
                    throw new InvalidFileTypeException(string.Format(ErrorMessages.NotANarcFile, Path.GetFileName(inputPath)));
                }

				string[] _retNames;
				MemoryStream[] _retStreams = new MemoryStream[fileEntries.Count];
                if (hasFilenames)
                {
					_retNames = new string[fileEntries.Count];
					for (int i=0;i<fileEntries.Count;++i){
						_retNames[i]=fileEntries[i].FullName;
                        _retStreams[i] = new MemoryStream();
                        using (var entryStream = new SubReadStream(input, fimgPosition + 8 + fileEntries[i].Offset, fileEntries[i].Length))
                        {
                            entryStream.CopyTo(_retStreams[i]);
                        }
					}
                }
                else
                {
					_retNames=null;
                    // This NARC doesn't contain filenames and directory names, so just use a file index as their filename
                    var index = 0; // think about how great it'll be when we have foreach! we won't need index variables or anyth-
                    foreach (var fileEntry in fileEntries)
                    {
						_retStreams[index] = new MemoryStream();
                        using (var entryStream = new SubReadStream(input, fimgPosition + 8 + fileEntry.Offset, fileEntry.Length))
                        {
                            entryStream.CopyTo(_retStreams[index]);
							_retStreams[index].Seek(0,SeekOrigin.Begin);
                        }
                        index++;
                    }
                }
				return new Tuple<string[],MemoryStream[]>(_retNames,_retStreams);
            }
        }
    }
}
