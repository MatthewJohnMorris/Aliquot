using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aliquot.Common
{
  /// <summary>
  /// Routines to interact with GraphViz, the open source application for producing images of
  /// (directed or undirected) graphs.
  /// </summary>
  public class GraphViz
  {
    /// <summary>
    /// File in which to store the location of dot.exe: this is written to the same location as the
    /// calling assembly.
    /// </summary>
    public const string FileNameGvDotLocation = "aliquot.gvdotlocation";

    /// <summary>
    /// Interactive function to list all instances of dot.exe off ProgramFilesX86 and to let the 
    /// user select one to use for graphing.
    /// </summary>
    public static void FindDotExe()
    {
      var pathProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
      string fileToFind = "dot.exe";
      Console.Out.WriteLine("Searching for [{0}] in [{1}]", fileToFind, pathProgramFilesX86);
      var files = new List<string>(Directory.GetFiles(pathProgramFilesX86, fileToFind, SearchOption.AllDirectories));
      if(files.Count == 0)
      {
        throw new ApplicationException(string.Format("Could not find [{0}] in [{1}]", fileToFind, pathProgramFilesX86));
      }
      Console.Out.WriteLine("Here are all the instances of [{0}] in [{1}]", fileToFind, pathProgramFilesX86);
      int n = files.Count;
      for (int i = 0; i < n; ++i)
      {
        Console.Out.WriteLine("[{0}] {1}", i, files[i]);
      }
      Console.Out.WriteLine("Please type the number of the {0} you want to use, and hit RETURN", fileToFind);
      var input = Console.ReadLine();
      int nInput = Convert.ToInt32(input);
      if(nInput < 0)
      {
        throw new ApplicationException(string.Format("Input ({0}) is less than minimum allowed (0)", input));
      }
      if (nInput >= n)
      {
        throw new ApplicationException(string.Format("Input ({0}) is greater than maximum allowed ({1})", input, n-1));
      }
      using(var w = new StreamWriter(GraphViz.FileNameGvDotLocation))
      {
        w.WriteLine(files[nInput]);
      }
    }

    /// <summary>
    /// Run dot.exe to produce an image from a graph file.
    /// </summary>
    /// <param name="gvOut">Location of input (without extension - we assume the extension is .gv)</param>
    /// <param name="gvFileType">Type of image (svg is often a good choice)</param>
    public static void RunDotExe(string gvOut, string gvFileType)
    {
      if (!System.IO.File.Exists(GraphViz.FileNameGvDotLocation))
      {
        throw new ApplicationException("Can't generate GraphVis, as there is no aliquot.gvdotlocation file - try running with -gvfinddot");
      }
      using (var r = new StreamReader(GraphViz.FileNameGvDotLocation))
      {
        var gvdotLocation = r.ReadLine();
        string arguments = "-T" + gvFileType + " " + gvOut + ".gv -o " + gvOut + "." + gvFileType;

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.CreateNoWindow = false;
        startInfo.UseShellExecute = false;
        startInfo.FileName = gvdotLocation;
        startInfo.WindowStyle = ProcessWindowStyle.Normal;
        startInfo.Arguments = arguments;

        // Start the process with the info we specified.
        // Call WaitForExit and then the using statement will close.
        int processTimeoutInMilliseconds = 5000;
        using (Process exeProcess = Process.Start(startInfo))
        {
          exeProcess.WaitForExit(processTimeoutInMilliseconds);
          if (!exeProcess.HasExited)
          {
            throw new ApplicationException(string.Format("Has not exited after timeout of {0} ms: [{1} {2}]", processTimeoutInMilliseconds, gvdotLocation, arguments));
          }
          if(exeProcess.ExitCode != 0)
          {
            throw new ApplicationException(string.Format("Non-zero exit code {0} from [{1} {2}]", exeProcess.ExitCode, gvdotLocation, arguments));
          }
        } // using: process
      } // using: read file
    }

  }
}
