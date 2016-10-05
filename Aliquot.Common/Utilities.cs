using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Numerics;

namespace Aliquot.Common
{
  internal static class Utilities
  {
    public static void Log(string s, params object[] ao) { Trace.Write(string.Format(s, ao)); }
    public static void LogLine(string s, params object[] ao) { Trace.WriteLine(string.Format(s, ao)); }

    public static string FormatWith(this string s, params object[] ao) { return string.Format(s, ao); }

    /// <summary>
    /// Output to a compressed file. This function manages the compression, feeding
    /// off the output of the writing function.
    /// </summary>
    /// <param name="path">file to write to</param>
    /// <param name="func">writing function</param>
    public static void WriteCompressedFile(
      string path, 
      Action<BinaryWriter> func,
      FileMode fileMode = FileMode.Create)
    {
      using (var fileStream = System.IO.File.Open(path, fileMode))
      using(var compressedStream = new GZipStream(fileStream, CompressionMode.Compress))
      using(var writer = new BinaryWriter(compressedStream))
      {
        func(writer);
      }
    }

    public static TResult WriteFileAndReturnValue<TResult>(
      string path,
      Func<BinaryWriter, TResult> func,
      FileMode fileMode = FileMode.Create)
    {
      using (var fileStream = System.IO.File.Open(path, fileMode))
      using (var writer = new BinaryWriter(fileStream))
      {
        return func(writer);
      }
    }

    public static string GetUnderlyingFileName(BinaryWriter writer)
    {
      FileStream fs = writer.BaseStream as FileStream;
      if (fs != null)
      {
        return fs.Name;
      }
      GZipStream gfs = writer.BaseStream as GZipStream;
      if (gfs != null)
      {
        FileStream fs2 = gfs.BaseStream as FileStream;
        if (fs2 != null)
        {
          return fs2.Name + " (gzipped)";
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
      using (var fileStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
      using (var decompressedStream = new GZipStream(fileStream, CompressionMode.Decompress))
      using (var reader = new BinaryReader(decompressedStream))
      {
        func(reader);
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
      using (var reader = new BinaryReader(fileStream))
      {
        func(reader);
      }
    }

    public static List<BigInteger> PerfectNumbers()
    {
      var ret = new List<BigInteger>() {
 	      6, 28, 496, 8128, 33550336, 8589869056, 137438691328, 2305843008139952128 };
      return ret;
    }

  }
}
