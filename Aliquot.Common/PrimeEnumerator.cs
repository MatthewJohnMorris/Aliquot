using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Aliquot.Common
{

  /// <summary>
  /// Allow enumeration of all Prime Numbers
  /// </summary>
  public class PrimesEnumerator : IEnumerator<BigInteger>
  {
    private readonly IPrimes myPrimes;
    private int myCurrentIndex;

    public PrimesEnumerator(IPrimes primes)
    {
      myPrimes = primes;
      myCurrentIndex = 0;
      Current = myPrimes[myCurrentIndex];
    }
    public BigInteger Current { get; private set; }
    public void Dispose() { return; }
    public bool MoveNext()
    {
      myCurrentIndex++;
      Current = myPrimes[myCurrentIndex];
      return true;
    }
    public void Reset()
    {
      myCurrentIndex = 0;
      Current = myPrimes[myCurrentIndex];
    }
    object IEnumerator.Current
    {
      get { return Current; }
    }
  }
}
