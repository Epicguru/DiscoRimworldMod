using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace VideoTool
{
    public class VideoLoader : IDisposable
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int FrameRate { get; private set; }
        public long BytesLoaded => memStream?.Length ?? 0;

        public bool[] CurrentFrame;

        private MemoryStream memStream;

        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            using var stream = new FileStream(filePath, FileMode.Open);
            using var zip = new ZipInputStream(stream);
            zip.GetNextEntry();

            memStream = new MemoryStream();
            zip.CopyTo(memStream);
            memStream.Position = 0;
        }

        private bool ResolveChangeMarker(short input, out int index)
        {
            bool isWhite = input > 0;
            if (!isWhite)
                input *= -1;

            index = input - 1;
            return isWhite;
        }

        private bool TryReadNext(out short data)
        {
            int rba = memStream.ReadByte();
            int rbb = memStream.ReadByte();
            if (rba == -1 || rbb == -1)
            {
                data = 0;
                return false;
            }
            byte ba = (byte)rba;
            byte bb = (byte)rbb;

            data = (short)(ba | (bb << 8));
            return true;
        }

        public void Restart()
        {
            if (memStream == null)
                return;

            memStream.Position = 0;
            CurrentFrame = null;
            LoadNextFrame();
        }

        public bool LoadNextFrame()
        {
            if (CurrentFrame == null)
            {
                // Load the resolution and framerate first.
                TryReadNext(out short w);
                TryReadNext(out short h);
                TryReadNext(out short fr);
                Width = w;
                Height = h;
                FrameRate = fr;

                // Read initial frame data.
                CurrentFrame = new bool[w * h];
                for (int i = 0; i < w * h; i++)
                {
                    bool couldRead = TryReadNext(out short read);
                    if (!couldRead)
                    {
                        return false;
                    }
                    bool white = ResolveChangeMarker(read, out int index);
                    CurrentFrame[index] = white;
                }

                // Read end-of-frame.
                bool goodEnd = TryReadNext(out short eof) && eof == 0;
                return goodEnd;
            }
            else
            {
                while (true)
                {
                    bool canRead = TryReadNext(out short read);
                    if (!canRead) // End-of-file. Return false to let caller know that there is no more data.
                        return false;
                    if (read == 0) // End-of-frame. Nothing else to do for now.
                        break;

                    // Resolve pixel change.
                    bool isWhite = ResolveChangeMarker(read, out int index);
                    CurrentFrame[index] = isWhite;
                }
                return true;
            }
        }

        public void Dispose()
        {
            if (memStream == null)
                return;

            memStream.Dispose();
            memStream = null;
        }
    }
}
