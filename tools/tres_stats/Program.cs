using System;
using System.IO;
using System.Text.RegularExpressions;
class Program {
    static int Main(string[] args) {
        if (args.Length == 0) { Console.WriteLine("Usage: tres_stats <path-to-tres>"); return 1; }
        var path = args[0];
        if (!File.Exists(path)) { Console.WriteLine("File not found: " + path); return 2; }
        var text = File.ReadAllText(path);
        var rx = new Regex(@"(?<=^|\s)(\d+):(\d+)/", RegexOptions.Multiline);
        var matches = rx.Matches(text);
        int maxX = -1, maxY = -1, count = 0;
        foreach (Match m in matches) {
            if (m.Groups.Count>=3) {
                int x = int.Parse(m.Groups[1].Value);
                int y = int.Parse(m.Groups[2].Value);
                if (x>maxX) maxX = x;
                if (y>maxY) maxY = y;
                count++;
            }
        }
        Console.WriteLine($"Found {count} tile keys. maxX={maxX}, maxY={maxY}");
        return 0;
    }
}
