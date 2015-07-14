using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aliquot.Common.Test
{
  [TestClass]
  public class PrimeFactorisationUnitTest
  {
    [TestMethod]
    public void FactorPerfect()
    {
      IPrimes p = PrimesFromFileUnitTest.AssembleFromFile(10000);

      var pf6 = new PrimeFactorisation(p, 6);
      Assert.AreEqual(6, pf6.SumAllProperDivisors());

      var pf28 = new PrimeFactorisation(p, 28);
      Assert.AreEqual(28, pf28.SumAllProperDivisors());

      var pf496 = new PrimeFactorisation(p, 496);
      Assert.AreEqual(496, pf496.SumAllProperDivisors());

      var pf8128 = new PrimeFactorisation(p, 8128);
      Assert.AreEqual(8128, pf8128.SumAllProperDivisors());
    }

    private void TestSigma(IPrimes p, int n, int sigma_n)
    {
      var pf = new PrimeFactorisation(p, n);
      Assert.AreEqual(sigma_n, n + pf.SumAllProperDivisors());
    }

    [TestMethod]
    public void TestFirst70()
    {
      IPrimes p = PrimesFromFileUnitTest.AssembleFromFile(10000);

      TestSigma(p, 1, 1);
      TestSigma(p, 2, 3);
      TestSigma(p, 3, 4);  
      TestSigma(p, 4, 7);  
      TestSigma(p, 5, 6);  
      TestSigma(p, 6, 12);  
      TestSigma(p, 7, 8);  
      TestSigma(p, 8, 15);  
      TestSigma(p, 9, 13); 
      TestSigma(p, 10, 18);  
      TestSigma(p, 11, 12);  
      TestSigma(p, 12, 28);  
      TestSigma(p, 13, 14);  
      TestSigma(p, 14, 24);  
      TestSigma(p, 15, 24);  
      TestSigma(p, 16, 31);  
      TestSigma(p, 17, 18);  
      TestSigma(p, 18, 39);  
      TestSigma(p, 19, 20);  
      TestSigma(p, 20, 42);  
      TestSigma(p, 21, 32);  
      TestSigma(p, 22, 36);  
      TestSigma(p, 23, 24);  
      TestSigma(p, 24, 60);  
      TestSigma(p, 25, 31);  
      TestSigma(p, 26, 42);  
      TestSigma(p, 27, 40);  
      TestSigma(p, 28, 56);  
      TestSigma(p, 29, 30);  
      TestSigma(p, 30, 72);  
      TestSigma(p, 31, 32);  
      TestSigma(p, 32, 63);  
      TestSigma(p, 33, 48);  
      TestSigma(p, 34, 54);  
      TestSigma(p, 35, 48);  
      TestSigma(p, 36, 91);  
      TestSigma(p, 37, 38);  
      TestSigma(p, 38, 60);  
      TestSigma(p, 39, 56);  
      TestSigma(p, 40, 90);  
      TestSigma(p, 41, 42);  
      TestSigma(p, 42, 96);  
      TestSigma(p, 43, 44);  
      TestSigma(p, 44, 84);  
      TestSigma(p, 45, 78);  
      TestSigma(p, 46, 72);  
      TestSigma(p, 47, 48);  
      TestSigma(p, 48, 124);  
      TestSigma(p, 49, 57);  
      TestSigma(p, 50, 93);  
      TestSigma(p, 51, 72);  
      TestSigma(p, 52, 98);  
      TestSigma(p, 53, 54);  
      TestSigma(p, 54, 120);  
      TestSigma(p, 55, 72);  
      TestSigma(p, 56, 120);  
      TestSigma(p, 57, 80);  
      TestSigma(p, 58, 90);  
      TestSigma(p, 59, 60);  
      TestSigma(p, 60, 168);  
      TestSigma(p, 61, 62);  
      TestSigma(p, 62, 96);  
      TestSigma(p, 63, 104);  
      TestSigma(p, 64, 127);  
      TestSigma(p, 65, 84);  
      TestSigma(p, 66, 144);  
      TestSigma(p, 67, 68);  
      TestSigma(p, 68, 126);  
      TestSigma(p, 69, 96);  
      TestSigma(p, 70, 144);  
    }
  }
}
