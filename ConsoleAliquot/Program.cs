using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

using OptionName = ConsoleAliquot.CommandLineParser.OptionName;
using Aliquot.Common;

namespace ConsoleAliquot
{
  class Program
  {
    static int Main(string[] args)
    {
      try
      {
        Main_MayThrow(args);
        return 0;
      }
      catch (Exception e)
      {
        Console.Out.WriteLine("Exception: " + e.Message);
        return 1;
      }
    }
    private static void Main_MayThrow(string[] args)
    {
      Trace.Listeners.Add(new ConsoleTraceListener());

      CommandLineParser clp = new CommandLineParser(args);
      string primesFile = clp.OptionValue(OptionName.PrimesFile, "primes.bin");
      string sPrimesLimit = clp.OptionValue(OptionName.PrimesLimit, "");
      string adbName = clp.OptionValue(OptionName.AdbFile, "aliquot.adb");
      string sDbLimit = clp.OptionValue(OptionName.AdbLimit, "100000");

      if (clp.HasOption(OptionName.MakePrimes))
      {
        if (!WarnOfLongOperationAndCheckIfUserWantsToContinue(string.Format("create primes file {0}", primesFile), 3.5))
        {
          return;
        }
        MakePrimesFile(primesFile, sPrimesLimit);
      }

      if(clp.HasOption(OptionName.MakeAdb))
      {
        if (!WarnOfLongOperationAndCheckIfUserWantsToContinue(string.Format("create aliquot DB {0}", adbName), 40.0))
        {
          return;
        }
        MakeAdbFile(primesFile, adbName, sDbLimit);
      }

      if (clp.HasOption(OptionName.Init))
      {
        if (!WarnOfLongOperationAndCheckIfUserWantsToContinue(string.Format("create primes file {0} and aliquot DB {1}", primesFile, adbName), 43.5))
        {
          return;
        }

        Console.Out.WriteLine("Creating primes (-makeprimes)");
        MakePrimesFile(primesFile);

        Console.Out.WriteLine("Creating ADB (-makeadb)");
        MakeAdbFile(primesFile, adbName, sDbLimit);

        Console.Out.WriteLine("Initialisation Complete.");
      }

      if (clp.HasOption(OptionName.ShowAdb))
      {
        var db = AliquotDatabase.Open(adbName);
        db = null;
      }

      if(clp.HasOption(OptionName.GvTree))
      {
        string gvOut = clp.OptionValue(OptionName.GvOut, "stdout");
        BigInteger treeBase = BigInteger.Parse(clp.OptionValue(OptionName.GvTree, "3"));
        WriteGvTree(gvOut, adbName, treeBase, sDbLimit);
      }

      if(clp.HasOption(OptionName.ExportTable))
      {
        ExportTable(adbName, "2", sDbLimit);    
      }

      if(clp.HasOption(OptionName.GvFindDot))
      {
        GraphViz.FindDotExe(GraphViz.GetUserInput_Int32_Console);
      }

      if(clp.OptionValues.Count == 0)
      {
        string assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
        string assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        Console.Out.WriteLine("{0} {1}", assemblyName, assemblyVersion);
        Console.Out.WriteLine("");
        Console.Out.WriteLine("Routines to investigate Aliquot Sequences");
        Console.Out.WriteLine("");
        Console.Out.WriteLine("Switches:");
        Console.Out.WriteLine("-AdbFile=FILENAME      ... name of ADB file (default aliquot.adb)");
        Console.Out.WriteLine("-AdbLimit=MAXTOPROCESS ... Highest number to look at (default 100000)");
        Console.Out.WriteLine("-ExportTable           ... write table of numbers in ADB");
        Console.Out.WriteLine("-GvFindDot             ... discover dot.exe");
        Console.Out.WriteLine("-GvTree=TREEBASE       ... make Gv tree (default 3)");
        Console.Out.WriteLine("-GvOut                 ... set output file for Gv functions");
        Console.Out.WriteLine("-Init                  ... create all initial setup");
        Console.Out.WriteLine("-MakeAdb               ... create ADB file");
        Console.Out.WriteLine("-MakePrimes            ... create Primes file");
        Console.Out.WriteLine("-PrimesFile=FILENAME   ... name of Primes file (default primes.bin)");
        Console.Out.WriteLine("-ShowAdb               ... dump details of ADB file");
      }
#if false
      var ptd = new PrimesTrialDivision();
      var db = AliquotDatabase.Create(ptd, 100); // 100000
      db.SaveAs("aliquot100.adb");
      var db2 = AliquotDatabase.Open("aliquot100.adb");
      // db.ShowTree(3, 1000);
      return;
#endif
    }

    private static void MakePrimesFile(string primesFile, string sPrimesLimit = "")
    {
      int primesLimit = sPrimesLimit.Length == 0 ? (Int32.MaxValue - 1) : Int32.Parse(sPrimesLimit);
      PrimesGeneratorSieveErat.Generate(primesFile, primesLimit, CreateProgressReporter());
    }

    private static Progress<Aliquot.Common.ProgressEventArgs> CreateProgressReporter()
    {
      return new Progress<Aliquot.Common.ProgressEventArgs>(ReportProgress);
    }
    private static void ReportProgress(Aliquot.Common.ProgressEventArgs args)
    {
      System.Console.Out.WriteLine(string.Format("{0}% complete: {1}", args.Percent, args.Message));
    }

    private static void MakeAdbFile(string primesFile, string adbName, string sDbLimit)
    {
      int dbLimit = int.Parse(sDbLimit);
      var p = new PrimesFromFile(primesFile, CreateProgressReporter());
      var adb = AliquotDatabase.Create(p, dbLimit, CreateProgressReporter());
      adb.SaveAs(adbName);
    }

    private static void WriteGvTree(string gvOut, string adbName, BigInteger treeBase, string sDbLimit)
    {
      bool isFile = false;
      TextWriter writer = System.Console.Out;
      if (0 != string.Compare(gvOut, "stdout", StringComparison.OrdinalIgnoreCase))
      {
        writer = new StreamWriter(gvOut + ".gv");
        isFile = true;
      }

      try
      {
        Console.Out.WriteLine("/*");
        var db = AliquotDatabase.Open(adbName);
        Console.Out.WriteLine("*/");
        BigInteger dbLimit = BigInteger.Parse(sDbLimit);
        db.WriteTree(treeBase, dbLimit, writer);
      }
      finally
      {
        if (isFile)
        {
          writer.Dispose();
        }
      }

      // try generating graphic
      if (isFile)
      {
        GraphViz.RunDotExe(gvOut, "svg");
      } // if: isFile
    }

    private static void ExportTable(string adbName, string sFrom, string sTo)
    {
      // Table Export
      // n, Prime Factors, Aliquot Root, Aliquot Sum
      Console.Out.WriteLine("/*");
      var db = AliquotDatabase.Open(adbName);
      Console.Out.WriteLine("*/");
      BigInteger nFrom = BigInteger.Parse(sFrom);
      BigInteger nTo = BigInteger.Parse(sTo);
      db.ExportTable(Console.Out, nFrom, nTo, AliquotDatabase.ExportFormat.Tsv);
    }

    private static bool WarnOfLongOperationAndCheckIfUserWantsToContinue(string description, double time)
    {
      Console.Out.WriteLine("Init mode - this initialises all the files you need");
      Console.Out.WriteLine("");
      Console.Out.WriteLine("It will {0}.", description);
      Console.Out.WriteLine("");
      Console.Out.WriteLine("Let's try and figure out roughly how long this is likely to take...");
      Console.Out.WriteLine("On Matt's machine (3.166GHz processor) this takes about {0:N2} minutes", time);
      Console.Out.WriteLine("Let's get your processor information:");
      double dMaxClockSpeedGHz = 0.0;
      using (ManagementObjectSearcher win32Proc = new ManagementObjectSearcher("select * from Win32_Processor"))
      {
        foreach (ManagementObject obj in win32Proc.Get())
        {
          double dClockSpeedMHz = Convert.ToDouble(obj["CurrentClockSpeed"]);
          double dClockSpeedGHz = dClockSpeedMHz / 1000.0;
          dMaxClockSpeedGHz = Math.Max(dMaxClockSpeedGHz, dClockSpeedGHz);
          string procName = obj["Name"].ToString();
          string manufacturer = obj["Manufacturer"].ToString();
          string version = obj["Version"].ToString();
          Console.Out.WriteLine("* Processor: {0:N3}GHz [{1} {2} {3}]", dClockSpeedGHz, manufacturer, procName, version);
        }
      }
      if (dMaxClockSpeedGHz == 0.0)
      {
        Console.Out.WriteLine("Sorry: couldn't get any processor speed information!");
      }
      else
      {
        double speedRatio = dMaxClockSpeedGHz / 3.166; // actual speed on this machine!
        Console.Out.WriteLine("Your fastest processor is {0:N3}GHz, which is {1:N2} times the speed of Matt's machine.",
          dMaxClockSpeedGHz, speedRatio);
        double expectedMinutes = time / speedRatio;
        Console.Out.WriteLine("So your expected time to complete is {0:N2} minutes.", expectedMinutes);
      }
      Console.Out.WriteLine("");
      Console.Out.WriteLine("Are you sure you want to continue? (y to continue, n to exit)");

      bool validKeyHit = false;
      while (!validKeyHit)
      {
        ConsoleKeyInfo cki = Console.ReadKey();
        Console.Out.WriteLine("");
        Console.Out.WriteLine("Key pressed: {0}", cki.Key);
        if (cki.Key == ConsoleKey.Y)
        {
          Console.Out.WriteLine("Will continue");
          validKeyHit = true;
        }
        else if (cki.Key == ConsoleKey.N)
        {
          Console.Out.WriteLine("Will exit program");
          validKeyHit = true;
          return false;
        }
        else
        {
          Console.Out.WriteLine("Unrecognised key: please try again");
        }
      }

      return true;
    }

  }
}
