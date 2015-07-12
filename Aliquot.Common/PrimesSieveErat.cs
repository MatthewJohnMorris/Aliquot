using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Aliquot.Common
{
  public class PrimesSieveErat : IPrimes
  {
    int myMaxPrime;
    List<int> myPrimes;
    public PrimesSieveErat(int maxPrime)
    {
      myMaxPrime = maxPrime;
      myPrimes = GetAllPrimesLessThan(maxPrime);
    }

    private static List<int> GetAllPrimesLessThan(int maxPrime)
    {
      var primes = new List<int>() { 2 };
      var maxSquareRoot = Math.Sqrt(maxPrime);
      var eliminated = new BitArray(maxPrime + 1);
      int percent = 0;
      Utilities.LogLine("PrimesSieveErat {0}", maxPrime);
      for (int i = 3; i <= maxPrime; i += 2)
      {
        int newPercent = (int)(100.0 * i / maxPrime);
        if(newPercent > percent)
        {
          Utilities.LogLine("PrimesSieveErat {0}%", newPercent);
          percent = newPercent;
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
      Utilities.Log("Writing To File [");
      int n = myPrimes.Count;
      writer.Write((Int32)n);
      int decile = 0;
      int i = 0;
      foreach(int prime in myPrimes)
      {
        int newDecile = (int)(10.0 * i / n);
        if(newDecile != decile)
        {
          Utilities.Log(".");
          decile = newDecile;
        }
        writer.Write((Int32)prime);
        i++;
      }
      Utilities.LogLine("]");
    }

  }
}
