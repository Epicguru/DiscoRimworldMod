using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Drawing;
using System.IO;

namespace VideoTool
{
    public class Compressor
    {
        public float MinBrightnessForWhite = 0.5f;

        public virtual bool IsWhite(Color color)
        {
            return color.GetBrightness() >= MinBrightnessForWhite;
        }

        private short MakeChangeMarker(int index, bool white)
        {
            // Index needs to be increased by 1, because 0 is reserved as the end-of-frame marker.
            index += 1;

            short marker = (short)index;
            if (!white)
                marker *= -1;

            return marker;
        }

        public void Compress(string outputFile, int frameRate, string inputFolder, string searchPattern = "*.bmp")
        {
            if (!Directory.Exists(inputFolder))
                throw new DirectoryNotFoundException(inputFolder);

            Compress(outputFile, frameRate, Directory.GetFiles(inputFolder, searchPattern, SearchOption.TopDirectoryOnly));
        }

        public void Compress(string outputFile, int frameRate, string[] imagePaths)
        {
            var info = new FileInfo(outputFile);
            if (!info.Directory.Exists)
                info.Directory.Create();

            using var file = new FileStream(outputFile, FileMode.Create);
            using var zip = new ZipOutputStream(file);
            using var writer = new BinaryWriter(zip);

            zip.PutNextEntry(new ZipEntry("data"){CompressionMethod = CompressionMethod.Deflated});

            Console.WriteLine($"Starting compression of {imagePaths.Length} frames at {frameRate} fps, {MinBrightnessForWhite} white cutoff.");

            int width = 0;
            int height = 0;
            bool[] lastFrame = null;

            int frame = 0;
            foreach (var path in imagePaths)
            {
                if (frame % 120 == 0)
                    Console.WriteLine($"Processing frame {frame}/{imagePaths.Length} ({(float)frame/imagePaths.Length*100f:F1}%)");

                Bitmap img = new Bitmap(Image.FromFile(path));
                if (width == 0)
                {
                    width = img.Width;
                    height = img.Height;
                    Console.WriteLine($"Writing first frame metadata and data: {(short)width}x{(short)height}@{(short)frameRate}fps");
                    writer.Write((short) width);
                    writer.Write((short) height);
                    writer.Write((short) frameRate);

                    lastFrame = new bool[width * height];
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int index = x + (height - y - 1) * width;
                            bool white = IsWhite(img.GetPixel(x, y));
                            lastFrame[index] = white;
                            writer.Write(MakeChangeMarker(index, white));
                        }
                    }

                    // End-of-frame
                    writer.Write((short)0);
                }
                else
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int index = x + (height - y - 1) * width;
                            bool white = IsWhite(img.GetPixel(x, y));
                            bool hasChanged = lastFrame[index] != white;
                            lastFrame[index] = white;

                            if(hasChanged)
                                writer.Write(MakeChangeMarker(index, white));
                        }
                    }

                    // End-of-frame
                    writer.Write((short)0);
                }

                img.Dispose();
                frame++;
            }
        }
    }
}
