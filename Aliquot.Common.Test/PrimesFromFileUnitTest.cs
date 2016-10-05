using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aliquot.Common.Test
{
  [TestClass]
  public class PrimesFromFileUnitTest
  {
    public static IPrimes AssembleFromFile(int maxPrime)
    {
      string tempFileName = System.IO.Path.GetTempFileName();
      FileUtils.DeleteNoThrow(tempFileName);
      IPrimes ret = null;
      try
      {
        PrimesGeneratorSieveErat.Generate(tempFileName, maxPrime);
        ret = new PrimesFromFile(tempFileName);
      }
      finally
      {
        try
        {
          System.IO.File.Delete(tempFileName);
        }
        catch (Exception) { }
      }
      return ret;
    }

    [TestMethod]
    public void FileSmall()
    {
      IPrimes p = AssembleFromFile(100);
      Assert.AreEqual(2, p[0]);
      Assert.AreEqual(3, p[1]);
      Assert.AreEqual(5, p[2]);
      Assert.AreEqual(7, p[3]);
      Assert.AreEqual(11, p[4]);
      Assert.AreEqual(13, p[5]);
      Assert.AreEqual(17, p[6]);
      Assert.AreEqual(19, p[7]);
    }

    [TestMethod]
    public void FileLargerKnown()
    {
      IPrimes p = AssembleFromFile(1000000);
      Assert.AreEqual(541, p[99]);
      Assert.AreEqual(7919, p[999]);
      Assert.AreEqual(104729, p[9999]);
    }
  }
}
