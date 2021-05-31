using System.Collections.Generic;
using Linux.IO;

namespace Linux.FileSystem
{
    public class BufferEntry {
        public readonly ITextIO Stream;
        public readonly bool IsLocal;

        public BufferEntry(ITextIO stream, bool isLocal) {
            Stream = stream;
            IsLocal = isLocal;
        }
    }

    public class VirtualFileTree : AbstractFileTree {
        protected Dictionary<File, BufferEntry> Streams;

        public VirtualFileTree(File root) : base(root) {
            Streams = new Dictionary<File, BufferEntry>();
        }

        protected override ITextIO OpenFileHandler(File file, int mode) {
            BufferEntry bufferEntry = Streams[file];

            if (bufferEntry.IsLocal) {
                return new TempStreamWrapper(bufferEntry.Stream, mode);
            }

            return bufferEntry.Stream;
        }

        protected override void OnAddedFile(File file, ITextIO stream) {
            bool isLocal = stream == null;

            if (isLocal) {
                stream = new BufferedStream(AccessMode.O_RDWR);
            }
            
            var bufferEntry = new BufferEntry(stream, isLocal);

            Streams.Add(file, bufferEntry);
        }

        protected override void OnRemoveFile(File file) {
            Streams.Remove(file);
        }
    }
}