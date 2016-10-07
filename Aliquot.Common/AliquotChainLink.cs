using System;
using System.Numerics;

namespace Aliquot.Common
{
  /// <summary>
  /// Holds a link in an Aliquot chain (or Current and an exception if a link could not be
  /// calcualted).
  /// </summary>
  public class AliquotChainLink
  {
    public readonly BigInteger Current;
    public readonly BigInteger Successor;
    public readonly PrimeFactorisation Factorisation;
    public readonly Exception Exception;

    public AliquotChainLink(BigInteger current, BigInteger successor, PrimeFactorisation factorisation)
    {
      Current = current;
      Successor = successor;
      Factorisation = factorisation;
    }

    public AliquotChainLink(IPrimes p, BigInteger n)
    {
      Current = n;
      try
      {
        Factorisation = new PrimeFactorisation(p, Current);
        Successor = Factorisation.SumAllProperDivisors();
      }
      catch(Exception e)
      {
        Exception = e;
      }
    }

  }
}
