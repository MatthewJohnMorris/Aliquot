using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace ConsoleAliquot
{
  class PrimesFromFile : IPrimes
  {
    public enum ShowLoadProgress { Yes, No }
    private readonly ShowLoadProgress myShowLoadProgress;
    private readonly List<int> myPrimes;

    public PrimesFromFile(string path, ShowLoadProgress showLoadProgress)
    {
      myShowLoadProgress = showLoadProgress;
      myPrimes = new List<int>();

      if(showLoadProgress == ShowLoadProgress.Yes)
      {
        Console.Out.Write("Reading From File [" + path + "][");
      }
      Utilities.ReadCompressedFile(path, this.InputFromBinaryReader);
      if(showLoadProgress == ShowLoadProgress.Yes)
      {
        Console.Out.WriteLine("][" + myPrimes.Count + "]");
      }
    }

    private void InputFromBinaryReader(BinaryReader reader)
    {
      int n = reader.ReadInt32();
      int n10 = n/10;
      int c = 0;
      for(int i = 0; i < n; ++i)
      {
        if(myShowLoadProgress == ShowLoadProgress.Yes)
        {
          if (c++ == n10) { c = 0; Console.Out.Write("."); }
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
