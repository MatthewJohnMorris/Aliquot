using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;

namespace Aliquot.Common
{
  /// <summary>
  /// Supply an unending series of Prime Numbers
  /// 
  /// This was useful when first getting things up and running, but trial division is very slow!
  /// </summary>
  public class PrimesTrialDivision : IPrimes
  {
    private List<BigInteger> myPrimes;
    private List<BigInteger> myPrimesSq;
    private bool myIsLastCandidateMinusOneModSix = true;
    public PrimesTrialDivision()
    {
      myPrimes = new List<BigInteger>();
      myPrimesSq = new List<BigInteger>();
      this.AddNewPrime(2);
      this.AddNewPrime(3);
      this.AddNewPrime(5);
    }
    private void AddNewPrime(BigInteger prime)
    {
      myPrimes.Add(prime);
      myPrimesSq.Add(prime * prime);
    }
    public BigInteger this[int index]
    {
      get
      {
        // Generate new primes as necessary in order to get the one we want. Typically
        // this is called by the enumerator so we will just be getting one more than
        // we already have.
        while (index >= myPrimes.Count)
        {
          AddNextPrime();
        }
        return myPrimes[index];
      }
    }
    private void AddNextPrime()
    {
      bool found = false;
      BigInteger candidate = myPrimes.Last();
      while(!found)
      {
        // Just do 6n-1 and 6n+1 (Factor Wheel on {2,3})
        int toAdd = myIsLastCandidateMinusOneModSix ? 2 : 4;
        candidate += toAdd;
        myIsLastCandidateMinusOneModSix = !myIsLastCandidateMinusOneModSix;

        // Divisor test
        for(int i = 0; i < myPrimes.Count; ++i)
        {
          // If our prime divisor > sqrt(candidate), candidate is prime
          var primeSq = myPrimesSq[i];
          if(primeSq > candidate)
          {
            found = true;
            break;
          }
          // See if prime divides candidate (in which case candidate is composite)
          var prime = myPrimes[i];
          if(candidate % prime == 0)
          {
            break;
          }
        }
      }
      this.AddNewPrime(candidate);

      if(myPrimes.Count % 100000 == 0)
      {
        Utilities.LogLine("Primes: C {0} L {1} M {2} S {3}", myPrimes.Count, candidate.ToString().Length, candidate, candidate * candidate);
      }
    }

  }
}
