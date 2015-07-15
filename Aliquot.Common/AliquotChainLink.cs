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
    /// <summary>
    /// Properties
    /// </summary>
    public BigInteger Current { get; private set; }
    public BigInteger Successor { get; private set; }
    public PrimeFactorisation Factorisation { get; private set; }
    public Exception Exception { get; private set; }

    public AliquotChainLink(BigInteger current, BigInteger successor, PrimeFactorisation factorisation)
    {
      Current = current;
      Successor = successor;
      Factorisation = factorisation;
      Exception = null;
    }

    public AliquotChainLink(IPrimes p, BigInteger n)
    {
      Current = n;
      Successor = 0;
      Factorisation = null;
      Exception = null;
      try
      {
        Init(p);
      }
      catch(Exception e)
      {
        Exception = e;
      }
    }

    private void Init(IPrimes p)
    {
      Factorisation = new PrimeFactorisation(p, Current);
      Successor = Factorisation.SumAllProperDivisors();
    }

  }
}
