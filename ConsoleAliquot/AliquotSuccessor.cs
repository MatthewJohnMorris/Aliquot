using System;
using System.Numerics;

namespace ConsoleAliquot
{
  class AliquotSuccessor
  {
    public BigInteger Current { get; private set; }
    public BigInteger Successor { get; private set; }
    public PrimeFactorisation Factorisation { get; private set; }
    public Exception Exception { get; private set; }

    public AliquotSuccessor(BigInteger current, BigInteger successor, PrimeFactorisation factorisation)
    {
      Current = current;
      Successor = successor;
      Factorisation = factorisation;
      Exception = null;
    }

    public AliquotSuccessor(IPrimes p, BigInteger n)
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
