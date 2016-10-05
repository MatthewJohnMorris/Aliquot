using System;
using System.IO;

namespace Aliquot.Common
{
  public class FileUtils
  {
    public static void DeleteNoThrow(string path)
    {
      try
      {
        File.Delete(path);
      }
      catch (Exception) { }
    }

  }
}
