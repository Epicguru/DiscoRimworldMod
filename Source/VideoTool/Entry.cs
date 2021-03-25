using System;

namespace VideoTool
{
    public class Entry
    {
        public static void Main(string[] args)
        {
            const string INPUT_FOLDER =
                @"C:\Users\The Superior One\Downloads\ffmpeg-2021-03-21-git-75fd3e1519-full_build\ffmpeg-2021-03-21-git-75fd3e1519-full_build\bin\Frames";
            const string OUTPUT_FILE = @"C:\Users\The Superior One\Desktop\BadApple.bwcv";

            var comp = new Compressor();
            comp.Compress(OUTPUT_FILE, 30, INPUT_FOLDER);

            Console.WriteLine();
            Console.WriteLine("Done");
            Console.ReadKey();
        }
    }
}
