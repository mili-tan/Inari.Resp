using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualBasic;

namespace Inari.Resp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Welcome to Inari.Resp");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("----------CommandLine----------");
            Console.WriteLine(
                Environment.CommandLine.Replace(
                    Process.GetCurrentProcess().MainModule.FileName + " ", ""));
            Console.WriteLine(Interaction.Command());
            Console.WriteLine("----------File----------");
            Console.WriteLine(args.FirstOrDefault());
            Console.WriteLine(File.Exists(args.FirstOrDefault()));

            var info = new ProcessStartInfo
            {
                FileName = @"E:\UTAU\tips.exe",
                Arguments = Interaction.Command(),
                RedirectStandardOutput = true,
                CreateNoWindow = false,
                UseShellExecute = false
            };
            var p = new Process {StartInfo = info, EnableRaisingEvents = true};
            p.Start();
            p.WaitForExit();
            Console.WriteLine("----------Resampler----------");
            Console.WriteLine(p.StandardOutput.ReadToEnd());
        }
    }
}
