using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Aliquot.Common
{
  public class PrimesFromFile : IPrimes
  {
    public enum ShowLoadProgress { Yes, No }
    private readonly List<int> myPrimes;
    private IProgress<ProgressEventArgs> myProgressIndicator;

    public override string ToString()
    {
      return string.Format("PrimesFromFile: {0:N0} primes, highest {1:N0}", myPrimes.Count, myPrimes.Last());
    }

    public PrimesFromFile(string path, Progress<ProgressEventArgs> handler = null)
    {
      myProgressIndicator = handler;
      myPrimes = new List<int>();

      Utilities.ReadCompressedFile(path, this.InputFromBinaryReader);
    }

    private void InputFromBinaryReader(BinaryReader reader)
    {
      ProgressEventArgs.RaiseEvent(myProgressIndicator, 0, "PrimesFromFile: Start");

      int n = reader.ReadInt32();
      int n100 = n/100;
      int c = 0;
      for(int i = 0; i < n; ++i)
      {
        if (c++ == n100)
        {
          c = 0;

          // Raise progress message
          string message = string.Format("PrimesFromFile: Read {0:N0} of {1:N0}", i, n);
          BigInteger b_i = i;
          BigInteger b_n = n;
          BigInteger b_percent = b_i * 100 / b_n;
          int percent = int.Parse(b_percent.ToString());
          ProgressEventArgs.RaiseEvent(myProgressIndicator, percent, message);
        }
        myPrimes.Add(reader.ReadInt32());
      }
    }

    public BigInteger this[int index]
    {
      get
      {
        return myPrimes[index];
      }
    }

  }
}
