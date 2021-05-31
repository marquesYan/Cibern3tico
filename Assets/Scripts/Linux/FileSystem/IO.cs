using Linux.IO;

namespace Linux.FileSystem {
    public class TempStreamWrapper : BufferedStream {
        protected ITextIO StreamBackend;

        public TempStreamWrapper(
            ITextIO stream,
            int mode
        ) : base(mode){
            StreamBackend = stream;
        }

        protected override void InternalClose() {
            // Save our buffer
            StreamBackend.Write(Buffer);

            base.InternalClose();
        }
    }
}