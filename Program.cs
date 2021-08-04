using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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

            //Console.WriteLine("CommandLine:");
            //Console.WriteLine(
            //    Environment.CommandLine.Replace(
            //        Process.GetCurrentProcess().MainModule.FileName + " ", ""));
            //Console.WriteLine(Interaction.Command());

            Console.WriteLine("File:" + args.FirstOrDefault());
            Console.WriteLine("File.Exists:" + File.Exists(args.FirstOrDefault()));

            if (!File.Exists(args.FirstOrDefault()))
            {
                var fileName = Path.GetFileName(args.FirstOrDefault());
                var url = "https://mili-01.coding.net/p/resp-repo/d/Haiamesen-JPNVCV-1.0/git/raw/master/";

                //var downloader = new DownloadService(new DownloadConfiguration()
                //{
                //    BufferBlockSize = 10240,
                //    ChunkCount = 8,
                //    MaxTryAgainOnFailover = 3,
                //    ParallelDownload = true,
                //    Timeout = 1000,
                //    RequestConfiguration =
                //    {
                //        Accept = "*/*",
                //        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                //        ProtocolVersion = HttpVersion.Version11,
                //        UserAgent = "INARI.Resp/0.1"
                //    }
                //});
                //downloader.DownloadFileTaskAsync(url + fileName, args.FirstOrDefault()).Wait();

                Console.WriteLine(url + fileName);
                new WebClient().DownloadFile(url + fileName, args.FirstOrDefault());

            }

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
            Console.WriteLine("Resampler:");
            Console.WriteLine(p.StandardOutput.ReadToEnd());
        }
    }
}
