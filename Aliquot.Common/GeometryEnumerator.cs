using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Aliquot.Common
{
  internal class GeometryEnumerator
  {
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
