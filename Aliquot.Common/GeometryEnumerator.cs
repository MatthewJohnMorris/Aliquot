using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Aliquot.Common
{
  /// <summary>
  /// Creates triangular, square, pentagonal etc numbers
  /// </summary>
  internal class GeometryEnumerator
  {
    /// <summary>
    /// Creates a stream of numbers of the "number of sides" required.
    /// </summary>
    /// <param name="geometry">The "number of sides": 3=triangular, 4=square, and so on</param>
    /// <returns>An enumerator that provides a stream of numbers</returns>
    public static IEnumerator<BigInteger> Create(int geometry)
    {
      int coefficient2 = geometry - 2;
      int coefficient1 = 4 - geometry;
      for (int index = 1; true; ++index)
      {
        BigInteger i = index;
        BigInteger v2 = coefficient2 * i * i;
        BigInteger v1 = coefficient1 * i;
        BigInteger result = (v2 + v1) / 2;
        yield return result;
      }
    }
  }
}
