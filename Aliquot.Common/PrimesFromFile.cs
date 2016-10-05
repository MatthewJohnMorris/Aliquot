using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;

namespace Aliquot.Common
{
  public class PrimesFromFile : IPrimes
  {
    public enum ShowLoadProgress { Yes, No }
    private int[] myPrimes;
    private IProgress<ProgressEventArgs> myProgressIndicator;
    private CancellationToken? myMaybeCancellationToken;

    public override string ToString()
    {
      return "PrimesFromFile: {0:N0} primes, highest {1:N0}".FormatWith(myPrimes.Length, myPrimes.Last());
    }

    public PrimesFromFile(
      string path, 
      Progress<ProgressEventArgs> progressIndicator = null,
      CancellationToken? maybeCancellationToken = null)
    {
      myPrimes = null;
      myProgressIndicator = progressIndicator;
      myMaybeCancellationToken = maybeCancellationToken;

      Utilities.ReadCompressedFile(path, this.InputFromBinaryReader);
    }

    private void InputFromBinaryReader(BinaryReader reader)
    {
      ProgressEventArgs.RaiseEvent(myProgressIndicator, 0, "PrimesFromFile: Start");

      int n = reader.ReadInt32();
      myPrimes = new int[n];
      int n100 = n/100;
      int c = 0;
      for(int i = 0; i < n; ++i)
      {
        if (c++ == n100)
        {
          c = 0;

          // Check for cancellation
          if(myMaybeCancellationToken.HasValue)
          {
            if(myMaybeCancellationToken.Value.IsCancellationRequested)
            {
              Console.Out.WriteLine("Cancel request made at " + DateTime.Now.ToString("hh:mm:ss"));
            }
            myMaybeCancellationToken.Value.ThrowIfCancellationRequested();
          }

          // Raise progress message
          string message = "PrimesFromFile: Read {0:N0} of {1:N0}".FormatWith(i, n);
          BigInteger b_i = i;
          BigInteger b_n = n;
          BigInteger b_percent = b_i * 100 / b_n;
          int percent = int.Parse(b_percent.ToString());
          ProgressEventArgs.RaiseEvent(myProgressIndicator, percent, message);
        }
        myPrimes[i] = reader.ReadInt32();
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
