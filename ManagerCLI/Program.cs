using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IniParser;
using Newtonsoft.Json;

namespace RespP
{
    class Program
    {
        public static Dictionary<string, (string name, DirectoryInfo dir, bool root)> OtoDict =
            new Dictionary<string, (string name, DirectoryInfo dir, bool root)>();

        static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(args.FirstOrDefault()))
            {
                Console.WriteLine("Not expected startup parameters, as UTAU plug-in please.");
                Console.ReadLine();
                return;
            }

            var perfixData = new FileIniDataParser().Parser.Parse(
                File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" + "prefix.ini"));
            var ustData = new FileIniDataParser().Parser.Parse(File.ReadAllText(args.FirstOrDefault(), Encoding.Default)
                .Replace("[#VERSION]\r\n" + "UST Version 1.20\r\n", ""));
            //ustData.Sections.RemoveSection("#PREV");
            //ustData.Sections.RemoveSection("#NEXT");

            var voiceDir = ustData.Sections["#SETTING"]["VoiceDir"].TrimEnd('\\') + "\\";
            var preDict = new Dictionary<string, string>();

            if (!File.Exists(voiceDir + "resp.json"))
            {
                Console.WriteLine("---------------");
                Console.ForegroundColor = ConsoleColor.DarkRed;
                if (!File.Exists(voiceDir + "resp.json")) Console.WriteLine("The RESP.json configuration file is missing. ");
                return;
            }

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

                //foreach (var item in otoDict)
                //    Console.WriteLine(item.Key + ":" + item.Value.Item2.FullName + item.Value.Item1);
                //foreach (var i in preDict) Console.WriteLine(i.Key + ":" + i.Value);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            var cfgDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(voiceDir + "resp.json"));
            var url = cfgDict["source"];

            Parallel.ForEach(ustData.Sections, itemSection =>
            {
                if (!itemSection.Keys.ContainsKey("NoteNum")) return;
                if (itemSection.Keys["Lyric"] == "R") return;
                try
                {
                    var lyric = itemSection.Keys["Lyric"];
                    var tone = perfixData.Sections.FirstOrDefault().Keys[itemSection.Keys["NoteNum"].ToString()];
                    (string name, DirectoryInfo dir, bool root) targetValue;
                    if (preDict.TryGetValue(tone, out var prefixValue)) lyric += prefixValue;

                    lock (OtoDict) if (!OtoDict.TryGetValue(lyric, out targetValue)) return;
                    var path = targetValue.dir.FullName + "\\" + targetValue.name;
                    Console.WriteLine(
                        $"{lyric} : {path} : {File.Exists(path)} {(targetValue.root ? " *" : string.Empty)}");
                    if (File.Exists(path)) return;
                    var uname = !targetValue.root ? targetValue.dir.Name + "\\" + targetValue.name : targetValue.name;
                    Download(url + uname, targetValue.dir.FullName + "\\" + targetValue.name);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
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
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"{url} : TimeOut!");
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine(e.Message);
                if (File.Exists(path)) File.Delete(path);
                Console.ForegroundColor = ConsoleColor.White;
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
