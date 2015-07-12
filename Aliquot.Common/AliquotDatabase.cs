using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Numerics;

namespace Aliquot.Common
{
  public class AliquotDatabase
  {
    public Dictionary<BigInteger, AliquotSuccessor> Links { get; private set; }
    public Dictionary<string, string> CreationProperties { get; private set; }

    private AliquotDatabase()
    {
      Links = new Dictionary<BigInteger, AliquotSuccessor>();
      CreationProperties = new Dictionary<string, string>();
    }
    public AliquotDatabase(
      Dictionary<BigInteger, AliquotSuccessor> links,
      Dictionary<string, string> creationProperties)
    {
      Links = links;
      CreationProperties = creationProperties;
    }

    public static AliquotDatabase Create(IPrimes p, int dbLimit)
    {
      var creationProperties = new Dictionary<string, string>();
      creationProperties["Create.ChainStartLimit"] = dbLimit.ToString();

      BigInteger upperLimit = BigInteger.Parse("1000000000000000"); // 10^15
      Utilities.LogLine("AliquotDatabase: Create to {0}, Successor Limit {1} ({2} digits)", dbLimit, upperLimit, upperLimit.ToString().Length);
      creationProperties["Create.UpperLimit"] = upperLimit.ToString();

      var links = new Dictionary<BigInteger, AliquotSuccessor>();
      DateTime dtStart = DateTime.UtcNow;

      int progress = 0;
      for (int i = 1; i <= dbLimit; ++i)
      {
        BigInteger n = i;
        var seq = new HashSet<BigInteger>();
        seq.Add(n);
        while(n > 1 && n < upperLimit && seq.Count < 200)
        {
          var s = new AliquotSuccessor(p, n);
          if (s.Successor > upperLimit) { break; }
          links[n] = s;
          if (seq.Contains(s.Successor)) { break; }
          seq.Add(s.Successor);
          n = s.Successor;
        }

        int newProgress = (int)(100.0 * i / dbLimit);
        if (newProgress > progress)
        {
          DateTime dtNow = DateTime.UtcNow;
          var diff = dtNow - dtStart;
          double s = diff.TotalSeconds;
          double expected = s * (dbLimit - i) / i;
          Utilities.LogLine("ADB: {0}% i {1} Time Used (min) {2:N} Estimated time Left (min) {3:N}", newProgress, i, s/60.0, expected/60.0);
          progress = newProgress;
        }
      }

      creationProperties["Create.FinishTimeUtc"] = DateTime.UtcNow.ToString();
      creationProperties["Create.Seconds"] = (DateTime.UtcNow - dtStart).TotalSeconds.ToString("N2");

      return new AliquotDatabase(links, creationProperties);
    }

    public void SaveAs(string path)
    {
      Utilities.WriteCompressedFile(path, Writer);
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
      string fileName = "(nofile)";
      FileStream fs = writer.BaseStream as FileStream;
      if(fs != null)
      {
        fileName = fs.Name;
      }
      else
      {
        GZipStream gfs = writer.BaseStream as GZipStream;
        if(gfs != null)
        {
          FileStream fs2 = gfs.BaseStream as FileStream;
          if(fs2 != null)
          {
            fileName = fs2.Name + " (gzipped)";
          }
        }
      }
      properties["WriteFile"] = fileName;
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

    public static AliquotDatabase Open(string path)
    {
      AliquotDatabase ret = new AliquotDatabase();
      Utilities.ReadCompressedFile(path, ret.Reader);
      return ret;
    }
    public void Reader(BinaryReader reader)
    {
      Links = new Dictionary<BigInteger, AliquotSuccessor>();
      CreationProperties = new Dictionary<string, string>(CreationProperties);

      // format
      string adbFormat = reader.ReadString();
      Utilities.LogLine("ADB format: {0}", adbFormat);
      if (adbFormat != "adb.1")
      {
        throw new ApplicationException(string.Format("Unexpected ADB Format [{0}]", adbFormat));
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
        throw new ApplicationException("ADB file did not have 'Count' property");
      }
      UInt64 count = UInt64.Parse(CreationProperties["Count"]);
      DateTime dtStart = DateTime.UtcNow;
      for(UInt64 i = 0; i < count; ++i)
      {
        UInt64 current = reader.ReadUInt64();
        UInt64 successor = reader.ReadUInt64();
        var pf = PrimeFactorisation.Create(reader);
        Links[current] = new AliquotSuccessor(current, successor, pf);
      }
      DateTime dtEnd = DateTime.UtcNow;
      double sec = (dtEnd - dtStart).TotalSeconds;
      Utilities.LogLine("ADB Read Time: {0:N2} sec", sec);
    }

    public void WriteTree(BigInteger treeBase, BigInteger limit, TextWriter writer)
    {
      // Build reverse links
      var reverseLinks = new Dictionary<BigInteger, List<BigInteger>>();
      foreach(BigInteger from in Links.Keys)
      {
        if(from <= limit)
        {
          BigInteger to = Links[from].Successor;
          if(to <= limit)
          {
            if(! reverseLinks.ContainsKey(to))
            {
              reverseLinks[to] = new List<BigInteger>();
            }
            reverseLinks[to].Add(from);
          }
        }
      }

      var allNodesInTree = new HashSet<BigInteger>();
      GatherNodes(reverseLinks, treeBase, allNodesInTree);

      // dot -Tpdf file.gv -o file.pdf

      writer.WriteLine("digraph G {");
      foreach(var node in allNodesInTree)
      {
        string factors = (Links.ContainsKey(node)) ? Links[node].Factorisation.ToString() : "";
        string color = CalcColor(Links[node]);
        writer.WriteLine("{0} [shape=record,label=\"<f0>{0}|<f1>{1}\",color={2}];", node, factors, color);
      }
      OutputLinks(reverseLinks, treeBase, writer);
      writer.WriteLine("}");

    }

    private string CalcColor(AliquotSuccessor s)
    {
      if(s.Factorisation.FactorsAndPowers.Count == 1)
      {
        if(s.Factorisation.FactorsAndPowers[0].Power == 1)
        {
          return "black";
        }
      }
      if(s.Current % 2 == 0)
      {
        return "red";
      }
      return "green";
    }

    private void GatherNodes(
      Dictionary<BigInteger, List<BigInteger>> links, 
      BigInteger current, 
      HashSet<BigInteger> nodes)
    {
      nodes.Add(current);
      if(! links.ContainsKey(current)) { return; }
      foreach(BigInteger predecessor in links[current])
      {
        GatherNodes(links, predecessor, nodes);
      }
    }
    private void OutputLinks(
      Dictionary<BigInteger, List<BigInteger>> links, 
      BigInteger current,
      TextWriter writer)
    {
      if (!links.ContainsKey(current)) { return; }
      foreach(BigInteger predecessor in links[current])
      {
        string attributes = "";
        if(current > predecessor)
        {
          attributes = " [arrowhead=empty]";
        }
        writer.WriteLine("{0}->{1}" + attributes, predecessor, current);
        OutputLinks(links, predecessor, writer);
      }
    }

    // n, Prime Factors, Aliquot Root, Aliquot Sum
    public void ExportTable(
      TextWriter writer,      
      BigInteger limit)
    {
      // Get Aliquot Root Sets by iterating over successors. Note that we may not have
      // all elements for a given root in our collection if the chain has gone above
      // the limit.
      var aliquotRootSets = new List<HashSet<BigInteger>>();
      for (BigInteger n = 2; n <= limit; ++n)
      {
        if(! Links.ContainsKey(n))
        {
          throw new ApplicationException(string.Format("Number {0} not contained in Links for ADB", n));
        }
        BigInteger s = Links[n].Successor;
        bool isFound = false;
        foreach(var aliquotRootSet in aliquotRootSets)
        {
          if (aliquotRootSet.Contains(s))
          {
            aliquotRootSet.Add(n);
            isFound = true;
            break;
          }
        }
        if(! isFound)
        {
          aliquotRootSets.Add(new HashSet<BigInteger> { n });
        }
      }

      // Get the Root for each Root Set and merge things in so we have a single set
      // per root
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

      // Output table
      for(BigInteger n = 2; n <= limit; ++n)
      {
        writer.WriteLine("{0}\t{1}\t{2}\t{3}", n, Links[n].Factorisation, aliquotRoots[n], Links[n].Successor);
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
