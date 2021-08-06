using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

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
                Parallel.ForEach(dir.GetDirectories(), subDirs =>
                {
                    Parallel.ForEach(subDirs.GetFiles(), item =>
                    {
                        if (item.Extension != ".wav" && item.Name != "oto.ini") return;
                        var hash = Convert.ToBase64String(
                            new SHA1CryptoServiceProvider().ComputeHash(File.ReadAllBytes(item.FullName)));
                        Console.WriteLine(hash + ":" + item.FullName.Split(dir.Name).Last().TrimStart('\\'));
                        hashs.Add(item.FullName.Split(dir.Name).Last().TrimStart('\\'), hash);
                    });
                });
                File.WriteAllText(dir.FullName + @"/resp.hash",
                    JsonConvert.SerializeObject(hashs, Formatting.Indented));
                return;
            }

            //Console.WriteLine("CommandLine:");
            //Console.WriteLine(Interaction.Command());

            Console.WriteLine("------------Inari.Resp v0.1-------------");

            var consoleColor = Console.ForegroundColor;
            var fileInfo = new FileInfo(args.FirstOrDefault());
            var dirName = fileInfo.Directory.Name;
            var respPath = fileInfo.DirectoryName + @"\resp.json";
            var hashPath = fileInfo.DirectoryName + @"\resp.hash";
            var fileName = fileInfo.FullName.Split(dirName).Last().TrimStart('\\');
            var fileExists = fileInfo.Exists;
            var respExists = File.Exists(respPath);
            var hashExists = File.Exists(hashPath);
            var res = AppDomain.CurrentDomain.BaseDirectory + "resampler.exe";

            Console.WriteLine("File:" + fileInfo.FullName.Split(dirName).Last());
            Console.WriteLine("File.Exists:" + fileExists);
            Console.WriteLine();

            if (!fileInfo.Directory.Exists) fileInfo.Directory.Create();

            if (respExists)
            {
                var respDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(respPath));
                var url = respDict["source"];
                res = respDict["resampler"];
                if (!res.Contains('/') && !res.Contains('\\')) res = AppDomain.CurrentDomain.BaseDirectory + res;

                if (!hashExists ||
                    (DateTime.UtcNow - new FileInfo(hashPath).LastWriteTimeUtc).TotalHours > 24)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Sync:resp.hash");
                    try
                    {
                        new WebClient().DownloadFileTaskAsync(url + "resp.hash", hashPath).Wait(3000);
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine(e);
                    }
                }

                var hashDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(hashPath));
                if (fileExists && hashDict.TryGetValue(fileName, out string fileHash) && fileHash != Convert.ToBase64String(
                    new SHA1CryptoServiceProvider().ComputeHash(File.ReadAllBytes(args.FirstOrDefault()))))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Outdated:" + fileName);
                    File.Delete(args.FirstOrDefault());
                    fileExists = false;
                }

                if (!fileExists)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(url + fileName);
                    try
                    {
                        new WebClient().DownloadFileTaskAsync(url + fileName, args.FirstOrDefault()).Wait(5000);
                    }
                    catch (Exception e)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine(e);
                    }
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
