using System.Collections.Generic;
using System.Numerics;

namespace Aliquot.Common
{
  /// <summary>
  /// This is the abstract interface for all sources of Prime numbers
  /// </summary>
  public interface IPrimes
  {
    /// <summary>
    /// Get the (index)'th prime number (0th is 2, 1st is 3, 2nd is 5, and so on)
    /// </summary>
    /// <param name="index">Which prime number to get</param>
    /// <returns>The prime number</returns>
    BigInteger this[int index] { get; }
  }
}
