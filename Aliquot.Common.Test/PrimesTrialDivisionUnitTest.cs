﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Aliquot.Common;

namespace Aliquot.Common.Test
{
  [TestClass]
  public class PrimesTrialDivisionUnitTest
  {
    [TestMethod]
    public void TrialDivSmall()
    {
      IPrimes p = new PrimesTrialDivision();
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
    public void TrialDivLargerKnown()
    {
      IPrimes p = new PrimesTrialDivision();
      Assert.AreEqual(541, p[99]);
      Assert.AreEqual(7919, p[999]);
      Assert.AreEqual(104729, p[9999]);
    }
  }
}
