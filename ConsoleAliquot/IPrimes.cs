using System.Collections.Generic;
using System.Numerics;

namespace ConsoleAliquot
{
  public interface IPrimes
  {
    BigInteger this[int index] { get; }
  }
}
