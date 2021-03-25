using System;
using VideoTool;

namespace VideoToolRunnable
{
    public class Entry
    {
        public static void Main(string[] args)
        {
            string input = args[0];
            string output = args[1];
            int fps = int.Parse(args[2]);
            
            var comp = new Compressor();
            if (args.Length >= 4)
                comp.MinBrightnessForWhite = float.Parse(args[3]);
            comp.Compress(output, fps, input);

            Console.WriteLine();
            Console.WriteLine("Done");
        }
    }
}
