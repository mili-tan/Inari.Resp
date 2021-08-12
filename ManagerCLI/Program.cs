using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;

namespace ManagerCLI
{
    class Program
    {
        public static IniData UstData;
        private static readonly string UstHeader = "[#VERSION]\r\n" + "UST Version 1.20\r\n";
        private static readonly Encoding EncodeJPN = Encoding.GetEncoding("Shift_JIS");
        static void Main(string[] args)
        {
            if (string.IsNullOrWhiteSpace(args.FirstOrDefault()))
            {
                Console.WriteLine("未包含应有的参数，请作为UTAU插件使用");
                return;
            }

            string ustFileStr = File.ReadAllText(args.FirstOrDefault(), Encoding.Default)
                .Replace(UstHeader, "");

            var perfixData =
                new FileIniDataParser().Parser.Parse(
                    File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" + "prefix.ini"));
            UstData = new FileIniDataParser().Parser.Parse(ustFileStr);
            UstData.Sections.RemoveSection("#PREV");
            UstData.Sections.RemoveSection("#NEXT");

            var path = UstData.Sections["#SETTING"]["VoiceDir"].TrimEnd('\\') + "\\";

            try
            {
                Console.WriteLine(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            Parallel.ForEach(UstData.Sections, itemSection =>
            {
                if (!itemSection.Keys.ContainsKey("NoteNum")) return;
                if (itemSection.Keys["Lyric"] == "R") return;
                try
                {
                    Console.WriteLine(itemSection.Keys["NoteNum"].ToString());
                    Console.WriteLine(perfixData.Sections.FirstOrDefault()
                        .Keys[itemSection.Keys["NoteNum"].ToString()]);
                    if (!itemSection.Keys.ContainsKey("@filename")) return;
                    var filename = itemSection.Keys["@filename"].ToString();
                    Console.WriteLine(filename + ":" + File.Exists(path + filename));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            });

            Console.ReadLine();
        }
    }
}
