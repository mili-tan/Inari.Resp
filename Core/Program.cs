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
                NoRes();
                return;
            }

            Console.WriteLine("CommandLine:" + Interaction.Command());
            Console.WriteLine("------------INARI.RESP v0.13------------");

            var fileInfo = new FileInfo(args.FirstOrDefault());
            var consoleColor = Console.ForegroundColor;
            var voiceName = fileInfo.Directory.Name;
            var voicePath = fileInfo.Directory.FullName;
            var utauPath = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                if (!File.Exists(fileInfo.Directory.FullName + @"\resp.json") &&
                    File.Exists(fileInfo.Directory.Parent.FullName + @"\resp.json"))
                {
                    voiceName = fileInfo.Directory.Parent.Name;
                    voicePath = fileInfo.Directory.Parent.FullName;
                }

                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "utau.exe"))
                {
                    if (File.Exists(fileInfo.Directory.Parent.Parent.FullName + @"\utau.exe"))
                        utauPath = fileInfo.Directory.Parent.Parent.FullName;
                    else if (File.Exists(fileInfo.Directory.Parent.Parent.Parent.FullName + @"\utau.exe"))
                        utauPath = fileInfo.Directory.Parent.Parent.Parent.FullName;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            var respPath = voicePath + @"\resp.json";
            var hashPath = voicePath + @"\resp.hash";
            var fileName = fileInfo.FullName.Split(voiceName).Last().TrimStart('\\');
            var res = AppDomain.CurrentDomain.BaseDirectory + "resampler.exe";

            if (!File.Exists(respPath) && !fileInfo.Directory.FullName.Contains(@"\voice\"))
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("This is not valid UTAU Voice path.");
            }
            else
            {
                Console.WriteLine("VoiceName:" + voiceName + " | File.Exists:" + fileInfo.Exists);
                Console.WriteLine("File:" + fileInfo.FullName + " | " + fileName);
            }

            if (!fileInfo.Directory.Exists) fileInfo.Directory.Create();

            if (File.Exists(respPath))
            {
                var respDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(respPath));
                var timeout = respDict.TryGetValue("timeout", out var timeValue) ? Convert.ToInt32(timeValue) : 5000;
                var url = respDict["source"];
                res = respDict["resampler"];

                if (!res.Contains('/') && !res.Contains('\\'))
                    res = File.Exists(utauPath + '\\' + res)
                        ? utauPath + '\\' + res
                        : AppDomain.CurrentDomain.BaseDirectory + res;

                if (!File.Exists(res))
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("Resampler NotFound:" + res);
                    res = AppDomain.CurrentDomain.BaseDirectory + "resampler.exe";
                    if (!File.Exists(res))
                    {
                        Console.WriteLine("Resampler NotFound:" + res);
                        res = fileInfo.Directory.FullName.Split("voice").First() + @"\resampler.exe";
                    }
                    if (!File.Exists(res)) Console.WriteLine("Resampler NotFound:" + res);
                }
                if (!File.Exists(hashPath) ||
                    (DateTime.UtcNow - new FileInfo(hashPath).LastWriteTimeUtc).TotalHours > 24)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("Sync:RESP.hash | " + url + "resp.hash");
                    Download(url + "resp.hash", hashPath,timeout);
                }
                if (!File.Exists(fileInfo.FullName))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(url + fileName);
                    Download(url + fileName, fileInfo.FullName, timeout);
                }

                if (File.Exists(fileInfo.FullName + ".noupdate") ||
                    File.Exists(fileInfo.Directory.FullName + @"\resp.noupdate"))
                {
                    Console.WriteLine("NoUpdate:" + fileName);
                    return;
                }

                ProcessRes(res, Interaction.Command(), consoleColor);
                var hashDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(hashPath));
                var keyExists = hashDict.TryGetValue(fileName, out var fileHash);
                if (!keyExists) keyExists = hashDict.TryGetValue(fileName.TrimStart('\\'), out fileHash);
                if (!keyExists) keyExists = hashDict.TryGetValue('\\' + fileName, out fileHash);

                Console.WriteLine("HashKeyExists:" + keyExists);

                if (fileInfo.Exists && keyExists && fileHash != Convert.ToBase64String(
                    new SHA1CryptoServiceProvider().ComputeHash(File.ReadAllBytes(fileInfo.FullName))))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Outdated:" + fileName + " | Newer:" + fileHash);
                    File.Delete(fileInfo.FullName);
                    File.Delete(fileInfo.FullName + ".frq");
                    File.Delete(fileInfo.FullName + ".frc");
                    File.Delete(fileInfo.FullName + ".pmk");
                    File.Delete(fileInfo.FullName + ".dio");
                    File.Delete(fileInfo.FullName + ".vs4ufrq");
                    File.Delete(fileInfo.FullName + ".llsm");
                    File.Delete(fileInfo.Directory.FullName + @"\desc.mrq");

                    if (!hashDict.Values.Contains(Convert.ToBase64String(
                        new SHA1CryptoServiceProvider().ComputeHash(
                            File.ReadAllBytes(fileInfo.Directory.FullName + @"\oto.ini")))))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Outdated:oto.ini");
                        File.Delete(fileInfo.Directory.FullName + @"\oto.ini");
                    }
                }

                if (!File.Exists(fileInfo.Directory.FullName + @"\oto.ini"))
                {
                    string otoUrl = url + fileName.TrimEnd(fileInfo.Name.ToCharArray()) + "oto.ini",
                        otoPath = fileInfo.Directory.FullName + @"\oto.ini";
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(otoUrl);
                    Download(otoUrl, otoPath, timeout);
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("The RESP.json configuration file is missing. ");
                ProcessRes(res, Interaction.Command(), consoleColor);
            }

            Console.WriteLine();
            Console.ForegroundColor = consoleColor;
        }

        public static void Download(string url, string path,int timeout)
        {
            try
            {
                new WebClient().DownloadFileTaskAsync(url, path).Wait(timeout);
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        public static void ProcessRes(string res, string cmd, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine();
            var info = new ProcessStartInfo
            {
                FileName = res,
                Arguments = cmd,
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

        public static void NoRes()
        {
            var line = string.Empty;
            Console.WriteLine("Welcome to Inari.Resp");
            while (string.IsNullOrEmpty(line) || !Directory.Exists(line))
            {
                Console.WriteLine("Drop the folder and press enter to generate hash");
                line = Console.ReadLine().Trim('\"');
            }

            var dir = new DirectoryInfo(line);
            var hashDict = new Dictionary<string, string>();
            Parallel.ForEach(dir.GetFiles(), item =>
            {
                if (item.Extension != ".wav" && item.Name != "oto.ini") return;
                var hash = Convert.ToBase64String(
                    new SHA1CryptoServiceProvider().ComputeHash(File.ReadAllBytes(item.FullName)));
                Console.WriteLine(hash + ":" + item.Name);
                hashDict.Add(item.Name, hash);
            });
            Parallel.ForEach(dir.GetDirectories(), subDirs =>
            {
                Parallel.ForEach(subDirs.GetFiles(), item =>
                {
                    if (item.Extension != ".wav" && item.Name != "oto.ini") return;
                    var hash = Convert.ToBase64String(
                        new SHA1CryptoServiceProvider().ComputeHash(File.ReadAllBytes(item.FullName)));
                    Console.WriteLine(hash + ":" + item.FullName.Split(dir.Name).Last());
                    hashDict.Add(item.FullName.Split(dir.Name).Last(), hash);
                });
            });
            File.WriteAllText(dir.FullName + @"\resp.hash",
                JsonConvert.SerializeObject(hashDict, Formatting.Indented));
        }
    }
}
