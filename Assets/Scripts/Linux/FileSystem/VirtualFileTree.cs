using System.Collections.Generic;
using Linux.IO;

namespace Linux.FileSystem
{
    public class VirtualFileTree : AbstractFileTree {
        protected Dictionary<File, BufferedStream> Streams;

        public VirtualFileTree(File root) : base(root) {
            Streams = new Dictionary<File, BufferedStream>();
        }

        protected override ITextIO OpenFileHandler(File file, int mode) {
            BufferedStream rootStream = Streams[file];
            return new TextStreamWrapper(rootStream, mode);
        }

        protected override void OnAddedFile(File file) {
            var rootStream = new BufferedStream(AccessMode.O_RDWR);
            Streams.Add(file, rootStream);
        }

        protected override void OnRemoveFile(File file) {
            Streams.Remove(file);
        }
    }
}