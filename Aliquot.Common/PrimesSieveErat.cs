using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Threading;

namespace Aliquot.Common
{
  public static class PrimesGeneratorSieveErat
  {
    public static void Generate(
      string path,
      int maxPrime,
      Progress<ProgressEventArgs> progressIndicator = null,
      CancellationToken? maybeCancellationToken = null)
    {
      string tempPath = System.IO.Path.GetTempFileName();
      string tempPath2 = System.IO.Path.GetTempFileName();
      using(new DisposableAction(() => FileUtils.DeleteNoThrow(tempPath)))
      using(new DisposableAction(() => FileUtils.DeleteNoThrow(tempPath2)))
      {
        // First pass just writes primes out to uncompressed temp file
        int numPrimes = Utilities.WriteFileAndReturnValue(
          tempPath,
          writer =>
            PrimesGeneratorSieveErat.GenerateAndWriteOutPrimes(
              writer, maxPrime, progressIndicator, maybeCancellationToken),
          FileMode.Open);

        // Second pass re-writes, with #primes header, to compressed file
        Action<BinaryWriter> writerFunction = 
          writer =>
            Utilities.ReadFile(
              tempPath,
              reader =>
                CreateFinalOutput(reader, writer, numPrimes, progressIndicator, maybeCancellationToken)
                );
        Utilities.WriteCompressedFile(
          tempPath2,
          writerFunction,
          FileMode.Open);

        // Finally rename
        File.Move(tempPath2, path);
      }
    }

    public static void CreateFinalOutput(
      BinaryReader reader, 
      BinaryWriter writer,
      int numPrimes,
      Progress<ProgressEventArgs> progressIndicator,
      CancellationToken? maybeCancellationToken)
    {
      // First, write the count of primes
      writer.Write((Int32)numPrimes);

      int updateEvery = numPrimes / 1000;
      if (updateEvery == 0) { updateEvery = 1; }

      for (int i = 0; i < numPrimes; ++i)
      {
        if (i % updateEvery == 0)
        {
          if(maybeCancellationToken.HasValue)
          {
            maybeCancellationToken.Value.ThrowIfCancellationRequested();
          }
          int percent = (numPrimes < 100) ? i * 100 / numPrimes : i / (numPrimes / 100);
          ProgressEventArgs.RaiseEvent(progressIndicator, percent, "PrimesSieveErat writing primes");
        }
        int prime = reader.ReadInt32();
        writer.Write((Int32)prime);
      }
    }

    /// <summary>
    /// The Sieve itself
    /// </summary>
    /// <param name="writer">where to write the primes out</param>
    /// <returns>the number of primes generated</returns>
    private static int GenerateAndWriteOutPrimes(
      BinaryWriter writer,
      int maxPrime,
      Progress<ProgressEventArgs> progressIndicator = null,
      CancellationToken? maybeCancellationToken = null)
    {
      // First prime (2)
      writer.Write((Int32)2);
      int numPrimes = 1;

      var maxSquareRoot = Math.Sqrt(maxPrime);
      var eliminated = new BitArray(maxPrime + 1);

      ProgressEventArgs.RaiseEvent(progressIndicator, 0, "PrimesSieveErat sizing");

      // Initialisation for Progress Reporting
      double sizeSum = 0.0;
      {
        int nextReport = 4;
        for (int i = 3; i <= maxPrime; i += 2)
        {
          if (i > nextReport && nextReport > 0)
          {
            sizeSum += Math.Log10((double)(maxPrime / nextReport));
            nextReport += i;
          }
        }
      }

      ProgressEventArgs.RaiseEvent(progressIndicator, 0, "PrimesSieveErat sieving");

      double progressSum = 0.0;
      {
        int nextReport = 4;
        BigInteger progressTotalEvery = 0;
        for (int i = 3; i <= maxPrime; i += 2)
        {
          // Progress Reporting
          if (i > nextReport && nextReport > 0)
          {
            if(maybeCancellationToken.HasValue)
            {
              maybeCancellationToken.Value.ThrowIfCancellationRequested();
            }

            progressSum += Math.Log10((double)(maxPrime / nextReport));
            nextReport += i;
            int percent = (int)(progressSum * 100.0 / sizeSum);
            ProgressEventArgs.RaiseEvent(
              progressIndicator, 
              percent,
              "PrimesSieveErat sieving {0} from [2,{1}]".FormatWith(i, maxPrime));
          }

          if (!eliminated[i])
          {
            // We've found a prime - write it out
            writer.Write((Int32)i);
            numPrimes++;

            // Eliminate multiples
            if (i < maxSquareRoot)
            {
              for (int j = i * i; j <= maxPrime && j > 0; j += 2 * i)
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
