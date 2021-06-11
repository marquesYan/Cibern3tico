using System.Text;
using SysStreamReader = System.IO.StreamReader;
using SysStreamWriter = System.IO.StreamWriter;
using Linux.IO;

namespace Linux.FileSystem {
    public class TempStreamWrapper : AbstractTextIO {
        protected ITextIO StreamBackend;

        protected StringBuilder InternalBuffer = new StringBuilder();

        public TempStreamWrapper(
            ITextIO stream,
            int mode
        ) : base(mode) {
            StreamBackend = stream;

            // Maybe truncate
            if (AccessMode.CanCreate(mode)) {
                Truncate();
            }

            FillInternalBuffer();
        }

        protected override void InternalClose() {
            InternalBuffer = null;
        }

        protected override string InternalRead(int length) {
            return BufferedStream.ExhaustBuffer(InternalBuffer, Pointer, length);
        }

        protected override int InternalAppend(string data) {
            InternalBuffer.Append(data);
            return StreamBackend.Write(data);
        }

        protected override bool CanMovePointer(int newPosition) {
            StreamBackend.Seek(newPosition);
            return newPosition < InternalBuffer.Length;
        }

        protected override void InternalTruncate() {
            if (StreamBackend != null) {
                InternalBuffer.Clear();
                StreamBackend.Truncate();
            }
        }

        protected void FillInternalBuffer() {
            InternalAppend(StreamBackend.Read());

            Length = InternalBuffer.Length;
        }
    }

    public class LocalFileStream : AbstractTextIO {
        protected SysStreamReader ReaderStream;

        protected SysStreamWriter WriterStream;
        public LocalFileStream(
            string path,
            int mode
        ) : base(mode) {
            if (AccessMode.CanWrite(mode)) {
                WriterStream = new SysStreamWriter(
                    path,
                    mode == AccessMode.O_APONLY
                );
            }

            if (AccessMode.CanRead(mode)) {
                ReaderStream = new SysStreamReader(path);
            }
        }

        protected override void InternalTruncate() {
            //
        }

        protected override int InternalAppend(string data) {
            WriterStream.Write(data);

            return data.Length;
        }

        protected override string InternalRead(int length) {
            if (length == -1) {
                return ReaderStream.ReadToEnd();
            }

            char[] buffer = new char[length];
            ReaderStream.Read();

            return buffer.ToString();
        }

        protected override bool CanMovePointer(int newPosition) {
            return false;
        }

        protected override void InternalClose() {
            System.Exception exception = null;

            if (WriterStream != null) {
                try {
                    WriterStream.Dispose();
                } catch (System.Exception e1) {
                    exception = e1;
                }
            } 

            if (ReaderStream != null) {
                try {
                    ReaderStream.Dispose();
                } catch (System.Exception e2) {
                    exception = e2;
                }
            }

            if (exception != null) {
                throw exception;
            }
        }
    }
}