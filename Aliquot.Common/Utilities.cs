using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace Aliquot.Common
{
  internal static class Utilities
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

    /// <summary>
    /// Output to a compressed file. This function manages the compression, feeding
    /// off the output of the writing function.
    /// </summary>
    /// <param name="path">file to write to</param>
    /// <param name="func">writing function</param>
    public static void WriteCompressedFile(
      string path, 
      Action<BinaryWriter> func)
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

    public static TResult WriteFileAndReturnValue<TResult>(
      string path,
      Func<BinaryWriter, TResult> func,
      FileMode fileMode = FileMode.Create)
    {
      using (var fileStream = System.IO.File.Open(path, fileMode))
      {
        using (var writer = new BinaryWriter(fileStream))
        {
          return func(writer);
        }
      }
    }

    public static string GetUnderlyingFileName(BinaryWriter writer)
    {
      FileStream fs = writer.BaseStream as FileStream;
      if (fs != null)
      {
        return fs.Name;
      }
      else
      {
        GZipStream gfs = writer.BaseStream as GZipStream;
        if (gfs != null)
        {
          FileStream fs2 = gfs.BaseStream as FileStream;
          if (fs2 != null)
          {
            return fs2.Name + " (gzipped)";
          }
        }
      }
      return "(nofile)";
    }

    /// <summary>
    /// Read from a compressed file. This function manages the decompression,
    /// passing a decompressed stream through to the reading function.
    /// </summary>
    /// <param name="path">file to read from</param>
    /// <param name="func">reading function</param>
    public static void ReadCompressedFile(string path, Action<BinaryReader> func)
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

    /// <summary>
    /// Read from a normal file.
    /// </summary>
    /// <param name="path">file to read from</param>
    /// <param name="func">reading function</param>
    public static void ReadFile(string path, Action<BinaryReader> func)
    {
      using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read))
      {
        using (var reader = new BinaryReader(fileStream))
        {
          func(reader);
        }
      }
    }

  }
}
