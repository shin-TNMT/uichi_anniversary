using System;
using System.IO;

class Program
{
    static int ToInt32BigEndian(byte[] b, int start)
    {
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(b, start, 4);
        }
        return BitConverter.ToInt32(b, start);
    }

    static int ReadBigEndianInt(byte[] data, int offset)
    {
        return (data[offset] << 24) | (data[offset+1] << 16) | (data[offset+2] << 8) | data[offset+3];
    }

    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: get_png_size <path-to-png>");
            return 1;
        }
        var path = args[0];
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found: {path}");
            return 2;
        }
        try
        {
            using var fs = File.OpenRead(path);
            byte[] header = new byte[24];
            int read = fs.Read(header, 0, header.Length);
            if (read < 24)
            {
                Console.WriteLine("File too small to be a PNG");
                return 3;
            }
            // PNG signature: 8 bytes, then 4 bytes length, then 4 bytes chunk type (IHDR), then IHDR data
            // Width at offset 16-19, Height at 20-23 (big-endian)
            if (header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47)
            {
                Console.WriteLine("Not a PNG file (signature mismatch)");
                return 4;
            }
            int width = ReadBigEndianInt(header, 16);
            int height = ReadBigEndianInt(header, 20);
            Console.WriteLine($"PNG size: {width} x {height}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            return 100;
        }
    }
}
