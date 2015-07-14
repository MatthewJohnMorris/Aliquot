using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;

namespace Aliquot.Common
{
  public class PrimesGeneratorSieveErat
  {
    public static void Generate(
      string path,
      int maxPrime,
      Progress<ProgressEventArgs> progressIndicator = null)
    {
      string tempPath = System.IO.Path.GetTempFileName();
      try
      {
        GenerateUsingTempFile(path, maxPrime, progressIndicator, tempPath);
      }
      finally
      {
        try
        {
          File.Delete(tempPath);
        }
        catch(Exception) {}
      }
    }

    private static void GenerateUsingTempFile(
      string path,
      int maxPrime,
      Progress<ProgressEventArgs> progressIndicator,
      string tempPath)
    {
      // First pass just writes primes out, need then to re-read and re-save!
      int numPrimes = WriteUncompressedTempFile(tempPath, maxPrime, progressIndicator);

      // Re-read and write to compressed file
      int n100 = numPrimes / 100;
      if (n100 == 0) { n100 = 1; }
      using (var fsOut = File.Create(path))
      {
        using (var compressedStream = new GZipStream(fsOut, CompressionMode.Compress))
        {
          using (var writer = new BinaryWriter(compressedStream))
          {
            writer.Write((Int32)numPrimes);
            using (var fsIn = File.Open(tempPath, FileMode.Open, FileAccess.Read))
            {
              using (var reader = new BinaryReader(fsIn))
              {
                for (int i = 0; i < numPrimes; ++i)
                {
                  if(i % n100 == 0)
                  {
                    int percent = i / n100;
                    ProgressEventArgs.RaiseEvent(progressIndicator, percent, string.Format("PrimesSieveErat writing primes"));
                  }
                  int prime = reader.ReadInt32();
                  writer.Write((Int32)prime);
                }
              }
            }
          }
        }
      }

    }

    private static int WriteUncompressedTempFile(
      string tempPath,
      int maxPrime,
      Progress<ProgressEventArgs> progressIndicator)
    {
      var p = new PrimesGeneratorSieveErat(maxPrime, progressIndicator);
      using (var fileStream = System.IO.File.Open(tempPath, FileMode.Open))
      {
        using (var writer = new BinaryWriter(fileStream))
        {
          return p.OutputToBinaryWriter(writer);
        }
      }
    }

    private int myMaxPrime;
    private IProgress<ProgressEventArgs> myProgressIndicator;

    private PrimesGeneratorSieveErat(
      int maxPrime,
      Progress<ProgressEventArgs> progressIndicator = null)
    {
      myMaxPrime = maxPrime;
      myProgressIndicator = progressIndicator;
    }

    private int OutputToBinaryWriter(BinaryWriter writer)
    {
      // First prime (2)
      writer.Write((Int32)2);
      int numPrimes = 1;

      var maxSquareRoot = Math.Sqrt(myMaxPrime);
      var eliminated = new BitArray(myMaxPrime + 1);

      ProgressEventArgs.RaiseEvent(myProgressIndicator, 0, "PrimesSieveErat sizing");

      double sizeSum = 0.0;
      {
        int nextReport = 4;
        for (int i = 3; i <= myMaxPrime; i += 2)
        {
          if (i > nextReport && nextReport > 0)
          {
            sizeSum += Math.Log10((double)(myMaxPrime / nextReport));
            nextReport += i;
          }
        }
      }

      ProgressEventArgs.RaiseEvent(myProgressIndicator, 0, "PrimesSieveErat sieving");

      double progressSum = 0.0;
      {
        int nextReport = 4;
        BigInteger progressTotalEvery = 0;
        for (int i = 3; i <= myMaxPrime; i += 2)
        {
          if (i > nextReport && nextReport > 0)
          {
            progressSum += Math.Log10((double)(myMaxPrime / nextReport));
            nextReport += i;
            int percent = (int)(progressSum * 100.0 / sizeSum);
            ProgressEventArgs.RaiseEvent(myProgressIndicator, percent, string.Format("PrimesSieveErat sieving {0} from [2,{1}]", i, myMaxPrime));
          }

          if (!eliminated[i])
          {
            // We've found a prime - write it out
            writer.Write((Int32)i);
            numPrimes++;

            if (i < maxSquareRoot)
            {
              for (int j = i * i; j <= myMaxPrime && j > 0; j += 2 * i)
              {
                eliminated[j] = true;
              }
            }
          }
        }
      }

      return numPrimes;
    }

  }
}
