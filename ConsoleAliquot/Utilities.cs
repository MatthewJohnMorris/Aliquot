using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace ConsoleAliquot
{
  class Utilities
  {
    public static void Log(string s) { Trace.Write(s); }
    public static void Log(string s, object o1) { Trace.Write(string.Format(s, o1)); }
    public static void Log(string s, object o1, object o2) { Trace.Write(string.Format(s, o1, o2)); }
    public static void Log(string s, object o1, object o2, object o3) { Trace.Write(string.Format(s, o1, o2, o3)); }
    public static void Log(string s, object o1, object o2, object o3, object o4) { Trace.Write(string.Format(s, o1, o2, o3, o4)); }
    public static void Log(string s, object o1, object o2, object o3, object o4, object o5) { Trace.Write(string.Format(s, o1, o2, o3, o4, o5)); }
    public static void LogLine(string s) { Trace.WriteLine(s); }
    public static void LogLine(string s, object o1) { Trace.WriteLine(string.Format(s, o1)); }
    public static void LogLine(string s, object o1, object o2) { Trace.WriteLine(string.Format(s, o1, o2)); }
    public static void LogLine(string s, object o1, object o2, object o3) { Trace.WriteLine(string.Format(s, o1, o2, o3)); }
    public static void LogLine(string s, object o1, object o2, object o3, object o4) { Trace.WriteLine(string.Format(s, o1, o2, o3, o4)); }
    public static void LogLine(string s, object o1, object o2, object o3, object o4, object o5) { Trace.WriteLine(string.Format(s, o1, o2, o3, o4, o5)); }

    public delegate void OutputToBinaryWriter(BinaryWriter writer);

    public static void WriteCompressedFile(string path, OutputToBinaryWriter func)
    {
      using(var fileStream = File.Create(path))
      {
        using(var compressedStream = new GZipStream(fileStream, CompressionMode.Compress))
        {
          using(var writer = new BinaryWriter(compressedStream))
          {
            func(writer);
          }
        }
      }
    }

    public delegate void InputFromBinaryReader(BinaryReader reader);

    public static void ReadCompressedFile(string path, InputFromBinaryReader func)
    {
      using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read))
      {
        using (var decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress))
        {
          using (var reader = new BinaryReader(decompressedStream))
          {
            func(reader);
          }
        }
      }
    }

  }
}
