using System.Collections.Generic;
using System.Numerics;

namespace Aliquot.Common
{
  public interface IPrimes
  {
    BigInteger this[int index] { get; }
  }
}
