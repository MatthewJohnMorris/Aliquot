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
      using(new DisposableAction(() => FileUtils.DeleteNoThrow(tempPath)))
      {
        // Write file out to temporary location
        Utilities.WriteCompressedFile(tempPath, Writer);

        // Finally rename
        File.Move(tempPath, path);
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

    private int GetTreeHeight(HashSet<AliquotChainLink> links, BigInteger treeBase)
    {
      var allHeights = new Dictionary<BigInteger, int>();
      foreach(var link in links)
      {
        int height = 0;
        var n = link.Current;
        while(n != treeBase)
        {
          height++;
          n = Links[n].Successor;
          if(allHeights.ContainsKey(n))
          {
            height += allHeights[n];
            break;
          }
        }
        allHeights[link.Current] = height;
      }

      int maxHeight = 0;
      foreach (var e in allHeights)
      {
        maxHeight = Math.Max(maxHeight, e.Value);
      }
      return maxHeight;
    }

    private static void WriteRuler(string rulername, int height, TextWriter writer)
    {
      for (int h = 0; h <= height; ++h)
      {
        writer.WriteLine("{0}{1} [shape=record,label=\"<f0>{1}\"];", rulername, h);
      }
      for (int h = 0; h < height; ++h)
      {
        writer.WriteLine("{0}{1}->{0}{2}", rulername, h + 1, h);
      }
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

        int height = GetTreeHeight(linksInTree, treeBase);

        // left-hand ruler
        writer.WriteLine("subgraph rulerleft {");
        WriteRuler("rulerleft", height, writer);
        writer.WriteLine("}");

        writer.WriteLine("subgraph main {");

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
        writer.WriteLine("}"); // main subgraph

        // right-hand ruler
        writer.WriteLine("subgraph rulerright {");
        WriteRuler("rulerright", height, writer);
        writer.WriteLine("}");

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

    /// <summary>
    /// Export information about numbers catered for by the Aliquot DB
    /// </summary>
    /// <param name="writer">Stream that info is written to</param>
    /// <param name="rangeFrom">First number in range</param>
    /// <param name="rangeTo">Last number in range</param>
    /// <param name="exportFormat">Which delimiter: Csv:",", Tsv:tab, Psv"|" (default)</param>
    public void ExportTable(
      TextWriter writer,      
      BigInteger rangeFrom,
      BigInteger rangeTo,
      ExportFormat exportFormat
      )
    {
      if(rangeFrom < 2)
      {
        throw new ArgumentOutOfRangeException("rangeFrom",
          string.Format("rangeFrom ({0}) below minimum (2)", rangeFrom));
      }
      if (rangeFrom > rangeTo)
      {
        throw new ArgumentOutOfRangeException("rangeFrom",
          string.Format("rangeFrom ({0}) above rangeTo ({1})", rangeFrom, rangeTo));
      }

      // Get the Aliquot Roots for each element in the range. Here we are
      // only following links in the Aliquot DB rather than factorising, so
      // this works *very* quickly.
      var aliquotRoots = new Dictionary<BigInteger, BigInteger>();
      var aliquotAscendingLowests = new Dictionary<BigInteger, BigInteger>();
      BigInteger rangeSize = 1 + rangeTo - rangeFrom;
      BigInteger limit100 = rangeSize / 100;
      if (limit100 == 0) { limit100 = 1; }
      for (BigInteger n = rangeFrom; n <= rangeTo; ++n)
      {
        // Progress
        if (n % limit100 == 0)
        {
          // Check for cancellation
          if (myMaybeCancellationToken.HasValue)
          {
            myMaybeCancellationToken.Value.ThrowIfCancellationRequested();
          }
          BigInteger percent = (n - rangeFrom) * 100 / rangeSize;
          int nPercent = (int)percent;
          ProgressEventArgs.RaiseEvent(myProgressIndicator, nPercent, "Getting Roots");
        }

        var root = GetRootOfChain(n);
        aliquotRoots[n] = root;

        // If no root, keep track of lowest element in the ascending chain
        if(root == 0)
        {
          BigInteger chainElement = n;
          if (!aliquotAscendingLowests.ContainsKey(chainElement))
          {
            // First, get the entire ascending chain
            var ascendingChain = new HashSet<BigInteger>();
            ascendingChain.Add(chainElement);
            BigInteger lowestInAscendingChain = chainElement;
            while (Links.ContainsKey(chainElement))
            {
              var s = Links[chainElement].Successor;
              lowestInAscendingChain = BigInteger.Min(lowestInAscendingChain, s);
              ascendingChain.Add(s);
              chainElement = s;
            }

            // Now get the lowest element that's either in the chain
            // or is already at-lowest for another chain hitting this chain.
            BigInteger lowestInOrLeadingToAscendingChain = lowestInAscendingChain;
            foreach (var i in ascendingChain)
            {
              if(aliquotAscendingLowests.ContainsKey(i))
              {
                lowestInOrLeadingToAscendingChain =
                  BigInteger.Min(
                    lowestInOrLeadingToAscendingChain,
                    aliquotAscendingLowests[i]);
              } // if: element has already appeared in another ascending chain
            } // foreach: element in ascending chain

            // Now update every chain element 
            foreach (var i in ascendingChain)
            {
              aliquotAscendingLowests[i] = lowestInOrLeadingToAscendingChain;
            } // foreach: element in ascending chain
          } // if: element has not already been processed in another ascending chain
        } // if: element is in ascending chain

      } // for: all numbers

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
      // 0: x         the number
      // 2: p.q        the prime structure eg 2.3
      // 3: f        the number of factors (prod (power+1))
      // 4: c       Complexity rating - see wiki
      // 5: a        Aliquot sum
      // 6: t        Tree root
      // 7: l        Length of aliquot sequence
      // 8: d        Difference between x and a, the number and the aliquot sum
      // 9: %      d as a percentage to x
      // 10: g    Geometry, whether the number is a triangular, square or pentagonal number etc
      // 11: m    Min in ascending chain
      ProgressEventArgs.RaiseEvent(myProgressIndicator, 0, "Output");
      writer.WriteLine("x{0}p.q{0}f{0}c{0}a{0}t{0}l{0}d{0}%{0}g{0}m", delimiter);

      // Set up "Geometry Enumerators": triangular, square, pentagonal etc
      var geometryEnumerators = new Dictionary<int, IEnumerator<BigInteger>>();
      for(int i = 3; i <= 10; ++i)
      {
        IEnumerator<BigInteger> e = GeometryEnumerator.Create(i);
        e.MoveNext();
        geometryEnumerators[i] = e;
      }

      for (BigInteger n = rangeFrom; n <= rangeTo; ++n)
      {
        // See which geometric numbers we are matching
        string geometries = "";
        foreach(var i in geometryEnumerators.Keys)
        {
          var e = geometryEnumerators[i];
          while (e.Current < n) { e.MoveNext(); }
          if (e.Current == n)
          {
            if (geometries.Length > 0) { geometries += ":"; }
            geometries += i;
          }
        }

        // Calculate chain lengths. While this *could* be done at the
        // same time as calculating Aliquot Roots, in practice the time
        // involved is negligable as we are simply following links in a
        // hash table.
        int chainLength = 0;
        BigInteger aliquotAscendingLowest = BigInteger.Zero;
        BigInteger root = aliquotRoots[n];
        if(root != 0)
        {
          if(root != n)
          {
            BigInteger i = n;
            while(i != root)
            {
              chainLength++;
              i = Links[i].Successor;
            }
          }
        }
        else
        {
          aliquotAscendingLowest = aliquotAscendingLowests[n];
        }

        BigInteger aliquotSum = Links[n].Successor;
        BigInteger aliquotDiff = aliquotSum - n;
        BigInteger diffPercent100 = 10000 * aliquotDiff / n;
        var fac = Links[n].Factorisation;
        int dfc = fac.DistinctFactorCount;
        int c = fac.ComplexityRating;
        double diffPercent = double.Parse(diffPercent100.ToString()) / 100.0;
        writer.WriteLine(
          "{0}{1}{2}{1}{3}{1}{4}{1}{5}{1}{6}{1}{7}{1}{8}{1}{9:##0.00}{1}{10}{1}{11}",
          n,                       // 0:x
          delimiter,
          Links[n].Factorisation,  // 2:p.q
          dfc - 2,                 // 3:f
          c,                       // 4:c
          Links[n].Successor,      // 5:a
          root,                    // 6:t
          chainLength,             // 7:l
          aliquotDiff,             // 8:t
          diffPercent,             // 9:t
          geometries,              // 10:g
          aliquotAscendingLowest); // 11:m
      }

    }

    /// <summary>
    /// This finds the "Aliquot Root": the terminating element of the onward
    /// Aliquot Chain from the number supplied. It works with the Aliquot DB links.
    /// It will deal correctly with arbitrary sized loops, returning the lowest
    /// element in the loop as the "root".
    /// </summary>
    /// <param name="n">Starting point</param>
    /// <returns>Aliquot root, or 0 if no root can be found</returns>
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
