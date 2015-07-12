using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Aliquot.Common
{
  /// <summary>
  /// Factorise a number into prime factors
  /// </summary>
  public class PrimeFactorisation
  {
    /// <summary>
    /// Helper class for producing a factorisation
    /// </summary>
    public class FactorAndPower
    {
      private readonly BigInteger factor;
      public BigInteger Factor { get { return factor; } }
      private readonly int power;
      public int Power { get { return power; } }
      public FactorAndPower(BigInteger factor, int power)
      {
        this.factor = factor;
        this.power = power;
      }
      public override string ToString()
      {
        if(Power == 1)
        {
          return Factor.ToString();
        }
        else
        {
          return string.Format("{0}^{1}", Factor, Power);
        }
      }
      public FactorAndPower IncrementPower()
      {
        return new FactorAndPower(factor, power + 1);
      }
    }

    private List<FactorAndPower> myFactorsAndPowers;
    public List<FactorAndPower> FactorsAndPowers { get { return myFactorsAndPowers; } }
    private PrimeFactorisation(List<FactorAndPower> factorsAndPowers)
    {
      myFactorsAndPowers = factorsAndPowers;
    }
    public PrimeFactorisation(IPrimes primes, BigInteger n)
    {
      myFactorsAndPowers = new List<FactorAndPower>();

      PrimesEnumerator e = new PrimesEnumerator(primes);
      var currentPrimeCandidate = e.Current;
      var residue = n;
      BigInteger factorToSave = -1;
      int powerToSave = 0;
      while(residue > 1)
      {
        // Look for a prime divisor
        BigInteger primeDivisor = 0;
        if (residue % currentPrimeCandidate == 0)
        {
          primeDivisor = currentPrimeCandidate;
        }
        else if (currentPrimeCandidate * currentPrimeCandidate > residue)
        {
          primeDivisor = residue;
        }
        else
        {
          // get the next prime to try
          e.MoveNext();
          currentPrimeCandidate = e.Current;
          continue;
        }

        // Divisor found: update data

        // See if it's a new prime
        if (primeDivisor != factorToSave)
        {
          // New prime: write the term for any previous prime out and start again
          if (factorToSave > 0)
          {
            myFactorsAndPowers.Add(new FactorAndPower(factor: factorToSave, power: powerToSave));
          }
          factorToSave = primeDivisor;
          powerToSave = 1;
        }
        else
        {
          powerToSave++; // same as last prime: keep track of powers
        }

        residue /= primeDivisor;

      } // while: residue > 1 (we still have factoring to do)

      // We are at the end so write final term out
      myFactorsAndPowers.Add(new FactorAndPower(factor: factorToSave, power: powerToSave));

    }
    public override string ToString()
    {
      var sb = new StringBuilder();
      foreach(var factorAndPower in myFactorsAndPowers)
      {
        if(sb.Length > 0)
        {
          sb.Append(" * ");
        }
        sb.Append(factorAndPower.ToString());
      }
      return sb.ToString();
    }

    public BigInteger SumAllProperDivisors()
    {
      BigInteger result = 1;
      BigInteger n = 1;

      var factorsAndPowers = myFactorsAndPowers;
      int numFactors = factorsAndPowers.Count;
      for (int i = 0; i < numFactors; ++i)
      {
        // Where we have p^n as a factor, build (1 + p + ... + p^n)
        BigInteger termSum = 1;
        BigInteger factor = factorsAndPowers[i].Factor;
        BigInteger currentFactorPower = 1;
        int power = factorsAndPowers[i].Power;
        for(int j = 1; j <= power; ++j)
        {
          currentFactorPower *= factor;
          termSum += currentFactorPower;
        }
        // Result is the product of these terms
        result *= termSum;
        // We also construct n as we go
        n *= currentFactorPower;
      }

      // We summed all divisors including n itself: remove n for the sum of all *proper*
      // divisors, which is the Aliquot number.
      result -= n;

      return result;
    }

    public void Write(BinaryWriter writer)
    {
      writer.Write((UInt64)FactorsAndPowers.Count);
      foreach(var factorAndPower in FactorsAndPowers)
      {
        writer.Write((UInt64)factorAndPower.Factor);
        writer.Write((UInt64)factorAndPower.Power);
      }
    }
    public static PrimeFactorisation Create(BinaryReader reader)
    {
      var factorsAndPowers = new List<FactorAndPower>();
      UInt64 count = reader.ReadUInt64();
      for (UInt64 i = 0; i != count; ++i)
      {
        UInt64 uiFactor = reader.ReadUInt64();
        UInt64 uiPower = reader.ReadUInt64();
        BigInteger factor = new BigInteger(uiFactor);
        int power = (int)uiPower;
        factorsAndPowers.Add(new FactorAndPower(factor, power));
      }
      return new PrimeFactorisation(factorsAndPowers);
    }

  }
}
