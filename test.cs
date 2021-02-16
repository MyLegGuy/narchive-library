using System;
using System.IO;
using Narchive.Formats;

/* public class justcopyfile : CopyOnlyStream { */
/* 	// this is an example of using CopyOnlyStream to pass lots of files without having them all open at once */
/* 	String filename; */
/* 	public justcopyfile (string filename){ */
/* 		this.filename=filename; */
/* 	} */
/* 	public override long getLength(){ */
/* 		return (new FileInfo(filename)).Length; */
/* 	} */
/* 	public override void CopyTo(Stream other){ */
/* 		FileStream f = File.OpenRead(filename); */
/* 		f.CopyTo(other); */
/* 		f.Dispose(); */
/* 	} */
/* } */

public static class program{
	public static int Main(string[] args){
		// this file must exist
		string testfilename="/tmp/test.narc";

		/* // NarcArchive.getFilenames gets names only. */
		/* // no directory components at all. it's actually quite useless, but good enough for what i needed it for. */
		/* String[] a=NarcArchive.getFilenames(testfilename); */
		/* if (a!=null){ */
		/* 	for (int i=0;i<a.Length;++i){ */
		/* 		Console.WriteLine(a[i]); */
		/* 	} */
		/* }else{ */
		/* 	Console.WriteLine("no filenames"); */
		/* } */
		
		// load narc.
		// if the narc has filenames, item1 of the tuple will be an array of relative filepaths.
		// so if the narc has subdirectories also, the name could be something like "folder/filename"
		// if there are no filenames, item1 will be null
		Tuple<string[],MemoryStream[]> _retTuple = NarcArchive.extract(testfilename, false);
		string[] _names = _retTuple.Item1;
		Stream[] _streams = _retTuple.Item2; // data streams for the files. they are MemoryStreams and you own them.
		
		// test write all the files
		Directory.CreateDirectory("/tmp/outdir");
		for (int i=0;i<_streams.Length;++i){
			if (_names!=null){
				Console.WriteLine(_names[i]);
			}
			using (FileStream fs = File.OpenWrite("/tmp/outdir/"+i)){
				// the streams are already at the start for us.
				_streams[i].CopyTo(fs);
			}
		}

		// test make a narc.
		// if you check the hash of /tmp/test.narc and /tmp/out.narc, they should be the same.
		NarcArchive.create(_names,_streams,"/tmp/out.narc");
		return 0;
	}
}
