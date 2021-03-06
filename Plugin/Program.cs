using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IniParser;
using Newtonsoft.Json;

namespace Inari.Resp.Plugin
{
    class Program
    {
        public static Dictionary<string, (string name, DirectoryInfo dir, bool root)> OtoDict =
            new Dictionary<string, (string name, DirectoryInfo dir, bool root)>();

        public static ConsoleColor ConsoleSColor = Console.ForegroundColor;

        static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(args.FirstOrDefault()))
            {
                Console.WriteLine("Not expected startup parameters, as UTAU plug-in please.");
                Console.ReadLine();
                return;
            }

            var perfixData = new FileIniDataParser().Parser.Parse(
                new WebClient().DownloadString("https://cdn.jsdelivr.net/gh/mili-tan/Inari.Resp@master/Plugin/prefix.ini"));
            var ustData = new FileIniDataParser().Parser.Parse(File.ReadAllText(args.FirstOrDefault(), Encoding.Default)
                .Replace("[#VERSION]\r\n" + "UST Version 1.20\r\n", ""));

            var voiceDir = ustData.Sections["#SETTING"]["VoiceDir"].TrimEnd('\\') + "\\";
            var respDict = new Dictionary<(string name, DirectoryInfo dir),(DirectoryInfo dir, bool root)>();
            var preDict = new Dictionary<string, string>();

            if (!File.Exists(voiceDir + "resp.json"))
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("The RESP.json configuration file is missing. ");
                Console.WriteLine("---------------");
                Console.ForegroundColor = ConsoleSColor;
                return;
            }

            var cfgDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(voiceDir + "resp.json"));
            var url = cfgDict["source"].TrimEnd('/').TrimEnd('\\') + "/";

            if (!File.Exists(voiceDir + "resp.hash") ||
                (DateTime.UtcNow - new FileInfo(voiceDir + "resp.hash").LastWriteTimeUtc).TotalHours > 24)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Sync:RESP.hash | " + url + "resp.hash");
                Download(url + "resp.hash", voiceDir + "resp.hash");
                Console.ForegroundColor = ConsoleSColor;
            }

            var hashDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(voiceDir + "resp.hash"));

            if (File.Exists(voiceDir + "prefix.map"))
            {
                foreach (var i in File.ReadAllLines(voiceDir + "prefix.map", Encoding.Default))
                {
                    if (string.IsNullOrWhiteSpace(i)) continue;
                    var split = i.Split('\t');
                    if (split.Length >= 2 && !string.IsNullOrWhiteSpace(split.LastOrDefault()))
                        preDict.Add(split.FirstOrDefault(), split.LastOrDefault());
                }
            }

            try
            {
                Console.WriteLine(voiceDir);
                var vDir = new DirectoryInfo(voiceDir);
                GetOto(vDir, true);

                if (vDir.GetDirectories().Length != 0)
                    foreach (var directory in vDir.GetDirectories())
                        GetOto(directory, false);

                //foreach (var item in OtoDict)
                //    Console.WriteLine(item.Key + ":" + item.Value.Item2.FullName + item.Value.Item1);
                //foreach (var i in preDict) Console.WriteLine(i.Key + ":" + i.Value);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            foreach (var itemSection in ustData.Sections)
            {
                try
                {
                    if (!itemSection.Keys.ContainsKey("NoteNum")) continue;
                    if (itemSection.Keys["Lyric"] == "R") continue;
                    var lyric = itemSection.Keys["Lyric"];
                    var tone = perfixData.Sections.FirstOrDefault().Keys[itemSection.Keys["NoteNum"]];
                    (string name, DirectoryInfo dir, bool root) targetValue;
                    if (preDict.TryGetValue(tone, out var prefixValue)) lyric += prefixValue;
                    lock (OtoDict) if (!OtoDict.TryGetValue(lyric, out targetValue)) continue;
                    var path = targetValue.dir.FullName + "\\" + targetValue.name;
                    Console.WriteLine(
                        $"{lyric} : {path} : {File.Exists(path)} {(targetValue.root ? " *" : string.Empty)}");

                    if (File.Exists(path) && !hashDict.Values.Contains(Convert.ToBase64String(
                        new SHA1CryptoServiceProvider().ComputeHash(File.ReadAllBytes(path)))))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Outdated:" + path);
                        File.Delete(path);
                        File.Delete(path + ".frq");
                        File.Delete(path + ".frc");
                        File.Delete(path + ".pmk");
                        File.Delete(path + ".dio");
                        File.Delete(path + ".vs4ufrq");
                        File.Delete(path + ".llsm");
                        File.Delete(targetValue.dir.FullName + @"\desc.mrq");

                        if (!hashDict.Values.Contains(Convert.ToBase64String(
                            new SHA1CryptoServiceProvider().ComputeHash(
                                File.ReadAllBytes(targetValue.dir.FullName + @"\oto.ini")))))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Outdated:oto.ini");
                            File.Delete(targetValue.dir.FullName + @"\oto.ini");
                            Download(!targetValue.root ? url + targetValue.dir.Name + @"\oto.ini" : url + @"\oto.ini",
                                targetValue.dir.FullName + @"\oto.ini");
                        }

                        Console.ForegroundColor = ConsoleSColor;
                    }
                    if (File.Exists(path)) continue;
                    if (!respDict.ContainsKey((targetValue.name, targetValue.dir)))
                        respDict.Add((targetValue.name, targetValue.dir), (targetValue.dir, targetValue.root));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            if (respDict.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("---------------");
                Console.WriteLine("Files are all synced.");
                Console.ReadLine();
                Console.ForegroundColor = ConsoleSColor;
                return;
            }

            Parallel.ForEach(respDict, i =>
            {
                var uname = !i.Value.root ? i.Value.dir.Name + "\\" + i.Key.name : i.Key.name;
                Download(url + uname,
                    i.Value.dir.FullName + "\\" + i.Key.name);
            });

            Console.WriteLine("---------------");
            Console.WriteLine("Files are all synced.");
            Thread.Sleep(500);
        }

        public static void Download(string url, string path)
        {
            try
            {
                new WebClient().DownloadFile(url, path);
                if (File.Exists(path))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"{url} : DONE!");
                    Console.ForegroundColor = ConsoleSColor;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{url} : TimeOut!");
                    Console.ForegroundColor = ConsoleSColor;
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e.Message);
                if (File.Exists(path)) File.Delete(path);
                Console.ForegroundColor = ConsoleSColor;
            }
        }

        public static void GetOto(DirectoryInfo vDir, bool root)
        {
            foreach (var file in vDir.GetFiles())
            {
                if (file.Name != "oto.ini") continue;
                Parallel.ForEach(File.ReadLines(file.FullName, Encoding.Default), i =>
                {
                    if (string.IsNullOrWhiteSpace(i)) return;
                    var split = i.Split(',').FirstOrDefault().Split('=');
                    try
                    {
                        lock (OtoDict)
                            if (!OtoDict.ContainsKey(split.LastOrDefault()))
                                OtoDict.Add(split.LastOrDefault(), (split.FirstOrDefault(), vDir, root));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(i);
                        Console.WriteLine(e);
                    }
                });
            }
        }
    }
}
