﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
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
                Console.WriteLine("Drop the folder and press enter to generate hash");
                var dir = new DirectoryInfo(Console.ReadLine());
                var hashs = new Dictionary<string, string>();
                Parallel.ForEach(dir.GetFiles(), item =>
                {
                    if (item.Extension != ".wav" && item.Name != "oto.ini") return;
                    var hash = Convert.ToBase64String(
                        new SHA1CryptoServiceProvider().ComputeHash(File.ReadAllBytes(item.FullName)));
                    Console.WriteLine(hash + ":" + item.Name);
                    hashs.Add(item.Name, hash);
                });
                File.WriteAllText(dir.FullName + @"/resp.hash", JsonSerializer.Serialize(hashs));
                return;
            }

            //Console.WriteLine("CommandLine:");
            //Console.WriteLine(Interaction.Command());

            Console.WriteLine("------------Inari.Resp v0.1-------------");
            Console.WriteLine("File:" + args.FirstOrDefault());
            Console.WriteLine("File.Exists:" + File.Exists(args.FirstOrDefault()));

            var consoleColor = Console.ForegroundColor;
            var respPath =Path.GetDirectoryName(args.FirstOrDefault()) + @"\resp.json";
            var hashPath = Path.GetDirectoryName(args.FirstOrDefault()) + @"\resp.hash";
            var fileName = Path.GetFileName(args.FirstOrDefault());
            var fileExists = File.Exists(args.FirstOrDefault());
            var respExists = File.Exists(respPath);
            var hashExists = File.Exists(hashPath);
            var res = AppDomain.CurrentDomain.BaseDirectory + "resampler.exe";

            if (respExists)
            {
                var respJson = Json.Parse(File.ReadAllText(respPath));
                var url = respJson.AsObjectGetString("source");
                res = respJson.AsObjectGetString("resampler");
                if (!res.Contains('/') && !res.Contains('\\')) res = AppDomain.CurrentDomain.BaseDirectory + res;

                if (!hashExists ||
                    (DateTime.UtcNow - new FileInfo(hashPath).LastWriteTimeUtc).TotalHours > 24)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Sync:resp.hash");
                    new WebClient().DownloadFile(url + "resp.hash", hashPath);
                }

                var hash = JsonSerializer.Deserialize<Dictionary<string, string>>(hashPath);
                if (fileExists && hash.TryGetValue(fileName, out string fileHash) && fileHash != Convert.ToBase64String(
                    new SHA1CryptoServiceProvider().ComputeHash(File.ReadAllBytes(args.FirstOrDefault()))))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Outdated:" + fileName);
                    File.Delete(args.FirstOrDefault());
                }

                if (!fileExists)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
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
