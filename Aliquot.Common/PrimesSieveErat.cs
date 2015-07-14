using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Aliquot.Common
{
  public class PrimesSieveErat : IPrimes
  {
    private int myMaxPrime;
    private IProgress<ProgressEventArgs> myProgressIndicator;
    private List<int> myPrimes;
    public PrimesSieveErat(int maxPrime, Progress<ProgressEventArgs> progressIndicator = null)
    {
      myMaxPrime = maxPrime;
      myProgressIndicator = progressIndicator;
      myPrimes = GetAllPrimesLessThan(maxPrime);
    }

    private List<int> GetAllPrimesLessThan(int maxPrime)
    {
      var primes = new List<int>() { 2 };
      var maxSquareRoot = Math.Sqrt(maxPrime);
      var eliminated = new BitArray(maxPrime + 1);

      ProgressEventArgs.RaiseEvent(myProgressIndicator, 0, "PrimesSieveErat sieving");

      int percent = 0;
      BigInteger progressTotalEvery = 0;
      for (int i = 3; i <= maxPrime; i += 2)
      {
        if (i % 128 == 0)
        {
          int newPercent = (int)(i / (maxPrime / 100));
          if (newPercent > percent)
          {
            ProgressEventArgs.RaiseEvent(myProgressIndicator, newPercent, string.Format("PrimesSieveErat sieving {0} from [2,{1}]", i, maxPrime));
            percent = newPercent;
          }
        }

        if (!eliminated[i])
        {
          primes.Add(i);
          if (i < maxSquareRoot)
          {
            for (int j = i * i; j <= maxPrime && j > 0; j += 2 * i)
            {
              eliminated[j] = true;
            }
          }
        }
      }
      return primes;
    }

    public BigInteger this[int index]
    {
      get
      {
        return myPrimes[index];
      }
    }

    public void WriteToFile(string path)
    {
      Utilities.WriteCompressedFile(path, this.OutputToBinaryWriter);
    }
    private void OutputToBinaryWriter(BinaryWriter writer)
    {
      int n = myPrimes.Count;
      writer.Write((Int32)n);
      int decile = 0;
      int i = 0;
      ProgressEventArgs.RaiseEvent(myProgressIndicator, 0, "Outputting primes to file");
      foreach (int prime in myPrimes)
      {
        int newDecile = (int)(10.0 * i / n);
        if(newDecile != decile)
        {
          ProgressEventArgs.RaiseEvent(myProgressIndicator, newDecile * 10, "Outputting primes to file");
          decile = newDecile;
        }
        writer.Write((Int32)prime);
        i++;
      }
    }

  }
}
