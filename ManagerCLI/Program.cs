using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser;

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
            var otoDict = new Dictionary<string, (string, DirectoryInfo, bool)>();
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
                otoDict = otoDict.Concat(GetOto(vDir)).ToDictionary(p => p.Key, p => p.Value);

                if (vDir.GetDirectories().Length != 0)
                    foreach (var directory in vDir.GetDirectories())
                        otoDict = otoDict.Concat(GetOto(directory)).ToDictionary(p => p.Key, p => p.Value);

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
                    var path = targetValue.Item2.FullName + targetValue.Item1;
                    Console.WriteLine($"{lyric} : {path} : {File.Exists(path)}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

            Console.ReadLine();
        }

        public static Dictionary<string, (string, DirectoryInfo, bool)> GetOto(DirectoryInfo vDir)
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
                            otoDict.Add(split.LastOrDefault(), (split.FirstOrDefault(), vDir, true));
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
