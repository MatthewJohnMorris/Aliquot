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
        // First pass just writes primes out to uncompressed temp file
        int numPrimes = WriteUncompressedTempFile(tempPath, maxPrime, progressIndicator);

        // Second pass re-writes, with #primes header, to compressed file
        var tempDetails = new TempDetails(tempPath, numPrimes, progressIndicator);
        Utilities.WriteCompressedFile(path, tempDetails.Writer);
      }
      finally
      {
        // Make sure the temp file is deleted, it's about half a Gig!
        try
        {
          File.Delete(tempPath);
        }
        catch(Exception) {}
      }
    }

    class TempDetails
    {
      public string TempPath;
      public int NumPrimes;
      public Progress<ProgressEventArgs> ProgressIndicator;

      public TempDetails(
        string tempPath,
        int numPrimes,
        Progress<ProgressEventArgs> progressIndicator)
      {
        TempPath = tempPath;
        NumPrimes = numPrimes;
        ProgressIndicator = progressIndicator;
      }
      public void Writer(
        BinaryWriter writer)
      {
        int updateEvery = NumPrimes / 1000;
        if (updateEvery == 0) { updateEvery = 1; }

        // First, write the count of primes
        writer.Write((Int32)NumPrimes);

        // Now copy from the temp path into our target stream
        using (var fsIn = File.Open(TempPath, FileMode.Open, FileAccess.Read))
        {
          using (var reader = new BinaryReader(fsIn))
          {
            for (int i = 0; i < NumPrimes; ++i)
            {
              if (i % updateEvery == 0)
              {
                int percent = (NumPrimes < 100) ? i * 100 / NumPrimes : i / (NumPrimes/100);
                ProgressEventArgs.RaiseEvent(ProgressIndicator, percent, string.Format("PrimesSieveErat writing primes"));
              }
              int prime = reader.ReadInt32();
              writer.Write((Int32)prime);
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

      // Initialisation for Progress Reporting
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
          // Progress Reporting
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

            // Eliminate multiples
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
