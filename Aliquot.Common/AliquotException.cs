using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aliquot.Common
{
  public class AliquotException : Exception
  {
    public AliquotException()
    {
    }

    public AliquotException(string message)
      : base(message)
    {
    }

    public AliquotException(string message, Exception inner)
      : base(message, inner)
    {
    }
  }
}
