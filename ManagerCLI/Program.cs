using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using Newtonsoft.Json;

namespace ManagerCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(args.FirstOrDefault()))
            {
                Console.WriteLine("未包含应有的参数，请作为UTAU插件使用");
                return;
            }

            var perfixData = new FileIniDataParser().Parser.Parse(
                File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" + "prefix.ini"));
            var ustData = new FileIniDataParser().Parser.Parse(File.ReadAllText(args.FirstOrDefault(), Encoding.Default)
                .Replace("[#VERSION]\r\n" + "UST Version 1.20\r\n", ""));
            ustData.Sections.RemoveSection("#PREV");
            ustData.Sections.RemoveSection("#NEXT");

            var voiceDir = ustData.Sections["#SETTING"]["VoiceDir"].TrimEnd('\\') + "\\";
            var respList = new List<(string name, DirectoryInfo dir, bool root)>();
            var otoDict = new Dictionary<string, (string name, DirectoryInfo dir, bool root)>();
            var preDict = new Dictionary<string, string>();

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
                otoDict = otoDict.Concat(GetOto(vDir, true)).ToDictionary(p => p.Key, p => p.Value);

                if (vDir.GetDirectories().Length != 0)
                    foreach (var directory in vDir.GetDirectories())
                        otoDict = otoDict.Concat(GetOto(directory, false)).ToDictionary(p => p.Key, p => p.Value);

                //foreach (var item in otoDict)
                //    Console.WriteLine(item.Key + ":" + item.Value.Item2.FullName + item.Value.Item1);
                //foreach (var i in preDict) Console.WriteLine(i.Key + ":" + i.Value);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Parallel.ForEach(ustData.Sections, itemSection =>
            {
                if (!itemSection.Keys.ContainsKey("NoteNum")) return;
                if (itemSection.Keys["Lyric"] == "R") return;
                try
                {
                    var lyric = itemSection.Keys["Lyric"];
                    var tone = perfixData.Sections.FirstOrDefault().Keys[itemSection.Keys["NoteNum"].ToString()];

                    if (preDict.TryGetValue(tone, out var prefixValue)) lyric += prefixValue;
                    if (!otoDict.TryGetValue(lyric, out var targetValue)) return;
                    var path = targetValue.dir.FullName + targetValue.name;
                    Console.WriteLine(
                        $"{lyric} : {path} : {File.Exists(path)} {(targetValue.root ? " : Main" : string.Empty)}");
                    if (!File.Exists(path)) respList.Add(targetValue);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

            if (respList.Count == 0) return;
            if (!File.Exists(voiceDir + "resp.json"))
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("The RESP.json configuration file is missing. ");
                Console.ReadLine();
                return;
            }

            var respDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(voiceDir + "resp.json"));
            var timeout = respDict.TryGetValue("timeout", out var timeValue) ? Convert.ToInt32(timeValue) : 5000;
            var url = respDict["source"];

            Console.ForegroundColor = ConsoleColor.Green;
            Parallel.ForEach(respList, i =>
            {
                Download(url + (i.root ? i.dir.Name + "\\" + i.name : i.name),
                    i.dir.FullName + i.name, timeout);
            });

            Console.ReadLine();
        }

        public static void Download(string url, string path, int timeout)
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

        public static Dictionary<string, (string, DirectoryInfo, bool)> GetOto(DirectoryInfo vDir, bool root)
        {
            var otoDict = new Dictionary<string, (string, DirectoryInfo, bool)>();
            foreach (var file in vDir.GetFiles())
            {
                if (file.Name != "oto.ini") continue;
                Parallel.ForEach(File.ReadLines(file.FullName, Encoding.Default), i =>
                {
                    if (string.IsNullOrWhiteSpace(i)) return;
                    var split = i.Split(',').FirstOrDefault().Split('=');
                    if (otoDict.ContainsKey(split.LastOrDefault())) return;
                    try
                    {
                        lock (otoDict)
                            otoDict.Add(split.LastOrDefault(), (split.FirstOrDefault(), vDir, root));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(i);
                        Console.WriteLine(e);
                    }
                });
            }

            return otoDict;
        }
    }
}
