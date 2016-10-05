using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aliquot.Common
{
  public class ProgressEventArgs : EventArgs
  {
    public readonly int Percent;
    public readonly string Message;
    public ProgressEventArgs(int percent, string message)
    {
      Percent = percent;
      Message = message;
    }
    public static void RaiseEvent(IProgress<ProgressEventArgs> handler, int percent, string message)
    {
      if (handler != null)
      {
        handler.Report(new ProgressEventArgs(percent, message));
      }
    }
  }
}
