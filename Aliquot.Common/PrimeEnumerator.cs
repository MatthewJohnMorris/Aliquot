using System.Collections;
using System.Collections.Generic;
using System.Numerics;

namespace Aliquot.Common
{

  /// <summary>
  /// Allow enumeration of all Prime Numbers
  /// </summary>
  /// 

  internal class PrimesEnumerator
  {
    public static IEnumerator<BigInteger> Create(IPrimes primes)
    {
      for (int index = 0; true; ++index)
      {
        yield return primes[index];
      }
    }
  }

}
