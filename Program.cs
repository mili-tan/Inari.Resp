using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Microsoft.VisualBasic;
using MojoJson;

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
            //Console.WriteLine(Interaction.Command());

            Console.WriteLine("File:" + args.FirstOrDefault());
            Console.WriteLine("File.Exists:" + File.Exists(args.FirstOrDefault()));

            var consoleColor = Console.ForegroundColor;
            var respName = args.FirstOrDefault() + @"\resp.json";
            var fileName = Path.GetFileName(args.FirstOrDefault());
            var fileExists = File.Exists(args.FirstOrDefault());
            var respExists = File.Exists(respName);
            var res = AppDomain.CurrentDomain.BaseDirectory + "resampler.exe";

            if (respExists)
            {
                Console.ForegroundColor = ConsoleColor.Green;

                var respJson = Json.Parse(File.ReadAllText(respName));
                var url = respJson.AsObjectGetString("source");
                res = respJson.AsObjectGetString("resampler");
                if (!res.Contains('/') && !res.Contains('\\')) res = AppDomain.CurrentDomain.BaseDirectory + res;

                if (!fileExists)
                {
                    Console.WriteLine(url + fileName);
                    new WebClient().DownloadFile(url + fileName, args.FirstOrDefault());
                }

                Console.ForegroundColor = consoleColor;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("The RESP.json configuration file is missing. ");
                Console.ForegroundColor = consoleColor;
            }

            var info = new ProcessStartInfo
            {
                FileName = res,
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
