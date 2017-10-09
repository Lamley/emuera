using System.Collections.Generic;
using System.Drawing;
using System.IO;
using MinorShift.Emuera.Sub;

namespace MinorShift.Emuera.Content
{
    internal static class AppContents
    {
        private static readonly Dictionary<string, AContentFile> resourceDic = new Dictionary<string, AContentFile>();
        private static readonly Dictionary<string, AContentItem> itemDic = new Dictionary<string, AContentItem>();

        public static T GetContent<T>(string name) where T : AContentItem
        {
            if (name == null)
                return null;
            name = name.ToUpper();
            if (!itemDic.ContainsKey(name))
                return null;
            return itemDic[name] as T;
        }

        public static void LoadContents()
        {
            if (!Directory.Exists(Program.ContentDir))
                return;
            try
            {
                var bmpfilelist = new List<string>();
                bmpfilelist.AddRange(Directory.GetFiles(Program.ContentDir, "*.png", SearchOption.TopDirectoryOnly));
                bmpfilelist.AddRange(Directory.GetFiles(Program.ContentDir, "*.bmp", SearchOption.TopDirectoryOnly));
                bmpfilelist.AddRange(Directory.GetFiles(Program.ContentDir, "*.jpg", SearchOption.TopDirectoryOnly));
                bmpfilelist.AddRange(Directory.GetFiles(Program.ContentDir, "*.gif", SearchOption.TopDirectoryOnly));
                foreach (var filename in bmpfilelist)
                {
//リスト化のみ。Loadはまだ
                    var name = Path.GetFileName(filename).ToUpper();
                    resourceDic.Add(name, new BaseImage(name, filename));
                }
                var csvFiles = Directory.GetFiles(Program.ContentDir, "*.csv", SearchOption.TopDirectoryOnly);
                foreach (var filename in csvFiles)
                {
                    var lines = File.ReadAllLines(filename, Config.Encode);
                    foreach (var line in lines)
                    {
                        if (line.Length == 0)
                            continue;
                        var str = line.Trim();
                        if (str.Length == 0 || str.StartsWith(";"))
                            continue;
                        var tokens = str.Split(',');
                        var item = CreateFromCsv(tokens);
                        if (item != null && !itemDic.ContainsKey(item.Name))
                            itemDic.Add(item.Name, item);
                    }
                }
            }
            catch
            {
                throw new CodeEE("リソースファイルのロード中にエラーが発生しました");
            }
        }

        public static void UnloadContents()
        {
            foreach (var img in resourceDic.Values)
                img.Dispose();
            resourceDic.Clear();
            itemDic.Clear();
        }

        private static AContentItem CreateFromCsv(string[] tokens)
        {
            if (tokens.Length < 2)
                return null;
            var name = tokens[0].Trim().ToUpper();
            var parentName = tokens[1].ToUpper();
            if (name.Length == 0 || parentName.Length == 0)
                return null;
            if (!resourceDic.ContainsKey(parentName))
                return null;
            var parent = resourceDic[parentName];
            if (parent is BaseImage)
            {
                var parentImage = parent as BaseImage;
                parentImage.Load(Config.TextDrawingMode == TextDrawingMode.WINAPI);
                if (!parentImage.Enabled)
                    return null;
                var rect = new Rectangle(new Point(0, 0), parentImage.Bitmap.Size);
                var noresize = false;
                if (tokens.Length >= 6)
                {
                    var rectValue = new int[4];
                    var sccs = true;
                    for (var i = 0; i < 4; i++)
                        sccs &= int.TryParse(tokens[i + 2], out rectValue[i]);
                    if (sccs)
                        rect = new Rectangle(rectValue[0], rectValue[1], rectValue[2], rectValue[3]);
                    if (tokens.Length >= 7)
                    {
                        var keywordTokens = tokens[6].Split('|');
                        foreach (var keyword in keywordTokens)
                            switch (keyword.Trim().ToUpper())
                            {
                                case "NORESIZE":
                                    throw new NotImplCodeEE();
                                    noresize = true;
                                    break;
                            }
                    }
                }
                var image = new CroppedImage(name, parentImage, rect, noresize);
                return image;
            }
            return null;
        }
    }
}