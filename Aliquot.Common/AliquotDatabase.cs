using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Aliquot.Common
{
  public class AliquotDatabase
  {
    public Dictionary<BigInteger, AliquotChainLink> Links { get; private set; }
    public Dictionary<string, string> CreationProperties { get; private set; }
    private IProgress<ProgressEventArgs> myProgressIndicator;
    private CancellationToken? myMaybeCancellationToken;

    private AliquotDatabase()
    {
      Links = new Dictionary<BigInteger, AliquotChainLink>();
      CreationProperties = new Dictionary<string, string>();
    }
    public AliquotDatabase(
      Dictionary<BigInteger, AliquotChainLink> links,
      Dictionary<string, string> creationProperties)
    {
      Links = links;
      CreationProperties = creationProperties;
    }

    public static AliquotDatabase Create(
      IPrimes p, 
      int dbLimit,
      Progress<ProgressEventArgs> progressIndicator = null,
      CancellationToken? maybeCancellationToken = null)
    {
      var creationProperties = new Dictionary<string, string>();
      creationProperties["Create.ChainStartLimit"] = dbLimit.ToString();

      BigInteger upperLimit = BigInteger.Parse("1000000000000000"); // 10^15
      Utilities.LogLine("AliquotDatabase: Create to {0}, Successor Limit {1} ({2} digits)", dbLimit, upperLimit, upperLimit.ToString().Length);
      creationProperties["Create.UpperLimit"] = upperLimit.ToString();

      var links = new Dictionary<BigInteger, AliquotChainLink>();
      DateTime dtStart = DateTime.UtcNow;

      int progress = 0;

      // Set up a parallel run across the collection
      var range = Enumerable.Range(1, dbLimit);
      var parOpts = new ParallelOptions();
      parOpts.CancellationToken = maybeCancellationToken.Value;
      // We *could* set this, but really .NET should be able to figure it out!
      // parOpts.MaxDegreeOfParallelism = System.Environment.ProcessorCount;
      Parallel.ForEach(range,
        (i) =>
      {
        // Build the chain onwards from this number
        BigInteger n = i;
        while (n > 1 && n < upperLimit)
        {
          // Get the new link
          var s = new AliquotChainLink(p, n);
          // Abandon if we would go above the limit
          if (s.Successor > upperLimit) { break; }
          // Synchronize on the links collection since this is in parallel
          lock(links)
          {
            // We exit if we are joining an existing chain
            if (links.ContainsKey(n)) { break; }
            // It's a new link - add it to the database
            links[n] = s;
          }
          // Go to next element in chain
          n = s.Successor;
        }

        // Indicate progress
        int newProgress = (int)(100.0 * i / dbLimit);
        if (newProgress > progress)
        {
          if(maybeCancellationToken.HasValue)
          {
            maybeCancellationToken.Value.ThrowIfCancellationRequested();
          }

          double s = (DateTime.UtcNow - dtStart).TotalSeconds;
          double expected = s * (dbLimit - i) / i;
          ProgressEventArgs.RaiseEvent(progressIndicator, newProgress, string.Format("ADB: i {0} Time Used (min) {1:N} Estimated time Left (min) {2:N}", i, s/60.0, expected/60.0));
          progress = newProgress;
        }
      }
      );

      creationProperties["Create.FinishTimeUtc"] = DateTime.UtcNow.ToString();
      creationProperties["Create.Seconds"] = (DateTime.UtcNow - dtStart).TotalSeconds.ToString("N2");

      return new AliquotDatabase(links, creationProperties);
    }

    public void SaveAs(string path)
    {
      string tempPath = System.IO.Path.GetTempFileName();
      try
      {
        // Write file out to temporary location
        Utilities.WriteCompressedFile(tempPath, Writer);

        // Finally rename
        File.Move(tempPath, path);
      }
      finally
      {
        try
        {
          File.Delete(tempPath);
        }
        catch (Exception) { }
      }
    }
    private void Writer(BinaryWriter writer)
    {
      // format
      writer.Write("adb.1");

      // # properties and the properties themselves
      var properties = new Dictionary<string, string>(CreationProperties);
      properties["Count"] = Links.Count.ToString();
      properties["WriteTimeUtc"] = DateTime.UtcNow.ToString();
      properties["WriteUser"] = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
      properties["WriteFile"] = Utilities.GetUnderlyingFileName(writer);
      WriteProperties(writer, properties);

      // data
      foreach(var from in Links.Keys)
      {
        var aqs = Links[from];
        writer.Write((UInt64)aqs.Current);
        writer.Write((UInt64)aqs.Successor);
        aqs.Factorisation.Write(writer);
      }
    }
    private static void WriteProperties(BinaryWriter writer, IDictionary<string, string> properties)
    {
      writer.Write((UInt64)properties.Count);
      Utilities.LogLine("Property count write [{0}]", properties.Count);
      foreach (string propertyName in properties.Keys)
      {
        writer.Write(propertyName);
        writer.Write(properties[propertyName]);
        Utilities.LogLine("Property write [{0}]=[{1}]", propertyName, properties[propertyName]);
      }
    }

    public static AliquotDatabase Open(
      string path,
      Progress<ProgressEventArgs> progressIndicator = null,
      CancellationToken? maybeCancellationToken = null)
    {
      AliquotDatabase ret = new AliquotDatabase();

      ret.myProgressIndicator = progressIndicator;
      ret.myMaybeCancellationToken = maybeCancellationToken;
      
      Utilities.ReadCompressedFile(path, ret.Reader);
      return ret;
    }
    public void Reader(BinaryReader reader)
    {
      Links = new Dictionary<BigInteger, AliquotChainLink>();
      CreationProperties = new Dictionary<string, string>(CreationProperties);

      // format
      string adbFormat = reader.ReadString();
      Utilities.LogLine("ADB format: {0}", adbFormat);
      if (adbFormat != "adb.1")
      {
        throw new InvalidDataException(string.Format("Unexpected ADB Format [{0}]", adbFormat));
      }
      // # properties
      UInt64 numProperties = reader.ReadUInt64();
      for(UInt64 ip = 0; ip != numProperties; ++ip)
      {
        string propertyKey = reader.ReadString();
        string propertyValue = reader.ReadString();
        CreationProperties[propertyKey] = propertyValue;
        Utilities.LogLine("ADB property [{0}]=[{1}]", propertyKey, propertyValue);
      }
      if (!CreationProperties.ContainsKey("Count"))
      {
        throw new InvalidDataException("ADB file did not have 'Count' property");
      }
      UInt64 n = UInt64.Parse(CreationProperties["Count"]);
      // read links
      UInt64 n100 = n / 100;
      UInt64 c = 0;
      DateTime dtStart = DateTime.UtcNow;
      for(UInt64 i = 0; i < n; ++i)
      {
        if (c++ == n100)
        {
          c = 0;

          // Check for cancellation
          if (myMaybeCancellationToken.HasValue)
          {
            if (myMaybeCancellationToken.Value.IsCancellationRequested)
            {
              Console.Out.WriteLine("Cancel request made at " + DateTime.Now.ToString("hh:mm:ss"));
            }
            myMaybeCancellationToken.Value.ThrowIfCancellationRequested();
          }

          // Raise progress message
          string message = string.Format("AliquotDB: Read {0:N0} of {1:N0}", i, n);
          BigInteger b_i = i;
          BigInteger b_n = n;
          BigInteger b_percent = b_i * 100 / b_n;
          int percent = int.Parse(b_percent.ToString());
          ProgressEventArgs.RaiseEvent(myProgressIndicator, percent, message);
        }

        UInt64 current = reader.ReadUInt64();
        UInt64 successor = reader.ReadUInt64();
        var pf = PrimeFactorisation.Create(reader);
        Links[current] = new AliquotChainLink(current, successor, pf);
      }
      DateTime dtEnd = DateTime.UtcNow;
      double sec = (dtEnd - dtStart).TotalSeconds;
      Utilities.LogLine("ADB Read Time: {0:N2} sec", sec);
    }

    public void WriteTree(BigInteger treeBase, BigInteger limit, TextWriter writer)
    {
      var numbersInTree = new HashSet<BigInteger>();
      var linksInTree = new HashSet<AliquotChainLink>();
      for (var i = new BigInteger(2); i <= limit; ++i)
      {
        BigInteger n = i;
        var numbersForN = new HashSet<BigInteger>();
        var linksForN = new HashSet<AliquotChainLink>();
        bool areInTree = false;
        // Constuct the chain and see if it hits the tree
        while(true)
        {
          // If we are at the tree base, then we've hit the tree
          if(n == treeBase)
          {
            areInTree = true;
            break;
          }
          // If we've hit a prime (so successor is 1) without hitting
          // the tree base, we've missed the tree
          if(n == 1)
          {
            areInTree = false;
            break;
          }
          // If we've hit a number already in the tree, we've hit the tree
          if(numbersInTree.Contains(n))
          {
            areInTree = true;
            break;
          }
          // If we've hit a number already in the chain, we've hit a perfect
          // number or amicable cycle so we've missed the tree
          if(numbersForN.Contains(n))
          {
            areInTree = false;
            break;
          }
          // If we've gone outside the scope of numbers in the database,
          // we've missed the tree
          if(! Links.ContainsKey(n))
          {
            areInTree = false;
            break;
          }
          // Keep track of numbers and links so far, and move on to successor
          AliquotChainLink link = Links[n];
          numbersForN.Add(n);
          linksForN.Add(link);
          n = link.Successor;
        } // while: true (constructing tree)

        // If we hit the tree then update numbers and links collection
        if(areInTree)
        {
          numbersInTree.UnionWith(numbersForN);
          linksInTree.UnionWith(linksForN);
        }
      }

      // dot -Tpdf file.gv -o file.pdf

      {
        writer.WriteLine("digraph G {");
        int n = linksInTree.Count;
        int n100 = n / 100;
        int c = 0;
        int i = 0;
        foreach (var link in linksInTree)
        {
          i++;
          if (c++ == n100)
          {
            c = 0;

            // Check for cancellation
            if (myMaybeCancellationToken.HasValue)
            {
              myMaybeCancellationToken.Value.ThrowIfCancellationRequested();
            }

            // Raise progress message
            string message = string.Format("Writing Nodes: Read {0:N0} of {1:N0}", i, n);
            int percent = i * 100 / n;
            ProgressEventArgs.RaiseEvent(myProgressIndicator, percent, message);
          }

          WriteBox(writer, link);
        }
        i = 0;
        foreach (var link in linksInTree)
        {
          i++;
          if (c++ == n100)
          {
            c = 0;

            // Check for cancellation
            if (myMaybeCancellationToken.HasValue)
            {
              myMaybeCancellationToken.Value.ThrowIfCancellationRequested();
            }

            // Raise progress message
            string message = string.Format("Writing Arrows: Read {0:N0} of {1:N0}", i, n);
            int percent = i * 100 / n;
            ProgressEventArgs.RaiseEvent(myProgressIndicator, percent, message);
          }

          WriteArrow(writer, link.Current, link.Successor);
        }

        // title
        var colorCounts = new Dictionary<string, int>();
        foreach (var link in linksInTree)
        {
          string color = CalcColor(link.Current, link.Factorisation);
          if(! colorCounts.ContainsKey(color))
          {
            colorCounts[color] = 0;
          }
          colorCounts[color] = 1 + colorCounts[color];
        }
        var sb = new StringBuilder();
        foreach(var e in colorCounts)
        {
          if (sb.Length > 0) { sb.Append(" "); }
          sb.Append(e.Key + ":" + e.Value);
        }
        string sColorCounts = sb.ToString();

        writer.WriteLine("graph[");
        writer.WriteLine("fontsize = 24"); // bottom
        writer.WriteLine("labeljust = \"right\""); // bottom
        writer.WriteLine("labelloc = \"b\""); // bottom
        writer.WriteLine("label = \"Aliquot Tree for " + treeBase + 
          ", limit " + limit +
          " (" + linksInTree.Count + " non-root numbers) (" + sColorCounts + ")\"");
        writer.WriteLine("]");
        writer.WriteLine("}");
      }
    
    }

    public void WriteChain(BigInteger chainStart, TextWriter writer)
    {
      var chainLinks = new List<AliquotChainLink>();
      var chainSet = new HashSet<BigInteger>();

      var current = chainStart;
      while(Links.ContainsKey(current) && ! chainSet.Contains(current))
      {
        var link = Links[current];
        chainLinks.Add(link);
        chainSet.Add(current);
        current = link.Successor;
      }

      // dot -Tpdf file.gv -o file.pdf

      writer.WriteLine("digraph G {");
      int n = chainLinks.Count;
      int n100 = n / 100;
      int c = 0;
      int i = 0;
      foreach (var link in chainLinks)
      {
        i++;
        if (c++ == n100)
        {
          c = 0;

          // Check for cancellation
          if (myMaybeCancellationToken.HasValue)
          {
            myMaybeCancellationToken.Value.ThrowIfCancellationRequested();
          }

          // Raise progress message
          string message = string.Format("Writing Nodes: Read {0:N0} of {1:N0}", i, n);
          int percent = i * 100 / n;
          ProgressEventArgs.RaiseEvent(myProgressIndicator, percent, message);
        }

        WriteBox(writer, link);
      }
      i = 0;
      foreach (var link in chainLinks)
      {
        i++;
        if (c++ == n100)
        {
          c = 0;

          // Check for cancellation
          if (myMaybeCancellationToken.HasValue)
          {
            myMaybeCancellationToken.Value.ThrowIfCancellationRequested();
          }

          // Raise progress message
          string message = string.Format("Writing Arrows: Read {0:N0} of {1:N0}", i, n);
          int percent = i * 100 / n;
          ProgressEventArgs.RaiseEvent(myProgressIndicator, percent, message);
        }

        WriteArrow(writer, link.Current, link.Successor);
      }
      writer.WriteLine("}");

    }

    private static void WriteBox(
      TextWriter writer,
      AliquotChainLink link)
    {
      string node = link.Current.ToString();
      string factors = link.Factorisation.ToString();
      string driver = link.Factorisation.Driver();
      string color = CalcColor(link.Current, link.Factorisation);
      if(driver.Length > 0)
      {
        writer.WriteLine("{0} [shape=record,label=\"<f0>{0}|<f1>{1}|<f2>{2}\",color={3}];", node, factors, driver, color);
      }
      else
      {
        writer.WriteLine("{0} [shape=record,label=\"<f0>{0}|<f1>{1}\",color={2}];", node, factors, color);
      }
    }
    private static string CalcColor(BigInteger current, PrimeFactorisation factorisation)
    {
      if(factorisation.FactorsAndPowers.Count == 1)
      {
        if(factorisation.FactorsAndPowers[0].Power == 1)
        {
          return "black";
        }
      }
      if (factorisation.IsSquare)
      {
        return "blue";
      }
      if (factorisation.Driver().Length > 0)
      {
        return "orange";
      }
      if(current % 2 == 0)
      {
        return "red";
      }
      return "green";
    }
    private static void WriteArrow(
      TextWriter writer,
      BigInteger current,
      BigInteger successor)
    {
      string attributes = "";
      if (successor > current)
      {
        attributes = " [arrowhead=empty,color=gray]";
      }
      writer.WriteLine("{0}->{1}" + attributes, current, successor);
    }

    public enum ExportFormat
    {
      Tsv,
      Csv
    }

    // n, Prime Factors, Aliquot Root, Aliquot Sum
    public void ExportTable(
      TextWriter writer,      
      BigInteger limit,
      ExportFormat exportFormat
      )
    {
      // Get Aliquot Root Sets by iterating over successors. Note that we may not have
      // all elements for a given root in our collection if the chain has gone above
      // the limit.
      string desc = string.Format("Getting {0} Aliquot Root Sets", limit);
      ProgressEventArgs.RaiseEvent(myProgressIndicator, 0, desc);
      var aliquotRootSets = new List<HashSet<BigInteger>>();
      BigInteger limit100 = limit / 100;
      for (BigInteger n = 2; n <= limit; ++n)
      {
        // Progress
        if (n % limit100 == 0)
        {
          // Check for cancellation
          if (myMaybeCancellationToken.HasValue)
          {
            myMaybeCancellationToken.Value.ThrowIfCancellationRequested();
          }
          BigInteger percent = n * 100 / limit;
          int nPercent = (int)percent;
          ProgressEventArgs.RaiseEvent(myProgressIndicator, nPercent, desc);
        }

        if(! Links.ContainsKey(n))
        {
          throw new IndexOutOfRangeException(string.Format("Number {0} not contained in Links for ADB", n));
        }
        BigInteger s = Links[n].Successor;

        bool isFound = false;
        if(s != 1)
        {
          foreach (var aliquotRootSet in aliquotRootSets)
          {
            if (aliquotRootSet.Contains(s))
            {
              aliquotRootSet.Add(n);
              isFound = true;
              break;
            }
          }
        }
        if (!isFound)
        {
          aliquotRootSets.Add(new HashSet<BigInteger> { n });
        }
      }

      // Get the Root for each Root Set and merge things in so we have a single set
      // per root
      ProgressEventArgs.RaiseEvent(myProgressIndicator, 0, "Merging Aliquot Root Sets");
      var aliquotRootSetsByRoot = new Dictionary<BigInteger, HashSet<BigInteger>>();
      foreach(var rootSet in aliquotRootSets)
      {
        BigInteger n = rootSet.First();
        BigInteger root = GetRootOfChain(n);
        if (!aliquotRootSetsByRoot.ContainsKey(root))
        {
          aliquotRootSetsByRoot[root] = new HashSet<BigInteger>();
        }
        aliquotRootSetsByRoot[root].UnionWith(rootSet);
      }

      // Construct lookup array for roots
      var aliquotRoots = new Dictionary<BigInteger, BigInteger>();
      foreach(var rootSetByRoot in aliquotRootSetsByRoot)
      {
        var root = rootSetByRoot.Key;
        foreach(var n in rootSetByRoot.Value)
        {
          aliquotRoots[n] = root;
        }
      }

      string delimiter = "|";
      if(exportFormat == ExportFormat.Csv)
      {
        delimiter = ",";
      }
      else if(exportFormat == ExportFormat.Tsv)
      {
        delimiter = "\t";
      }

      // Output table
      ProgressEventArgs.RaiseEvent(myProgressIndicator, 0, "Output");
      writer.WriteLine("n{0}f{0}r{0}s", delimiter);
      for(BigInteger n = 2; n <= limit; ++n)
      {
        writer.WriteLine("{0}{1}{2}{1}{3}{1}{4}", 
          n, delimiter, 
          Links[n].Factorisation,
          aliquotRoots[n],
          Links[n].Successor);
      }

    }

    private BigInteger GetRootOfChain(BigInteger n)
    {
      var aliquotChain = new HashSet<BigInteger>();
      while (true)
      {
        // No onward link - root is undefined
        if(! Links.ContainsKey(n))
        {
          return BigInteger.Zero;
        } // if: no onward link/successor

        // We are looping - get minimal element of loop
        if (aliquotChain.Contains(n))
        {
          // Gather the loop
          var aliquotLoop = new HashSet<BigInteger>();
          while(! aliquotLoop.Contains(n))
          {
            aliquotLoop.Add(n);
            n = Links[n].Successor;
          }
          // Calculate and return the smallest element
          var minimumInLoop = n;
          foreach(var numberInLoop in aliquotLoop)
          {
            minimumInLoop = BigInteger.Min(minimumInLoop, numberInLoop);
          }
          return minimumInLoop;
        } // if: a loop

        // It's a prime - this is the root
        BigInteger s = Links[n].Successor;
        if (s == 1)
        {
          return n;
        } // if: successor is 1 (so this is prime)

        // add to the chain
        aliquotChain.Add(n);

        // Go to the next element
        n = s;

      } // while: true
    }

  }
}
