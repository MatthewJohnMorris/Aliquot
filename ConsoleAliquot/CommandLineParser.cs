using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAliquot
{
  internal class CommandLineParser
  {
    public IDictionary<OptionName, string> OptionValues { get; private set; }
    public enum OptionName
    {
      Undefined,
      AdbFile,
      AdbLimit,
      ExportTable,
      GvFindDot,
      GvOut,
      GvTree,
      Init,
      MakeAdb,
      MakePrimes,
      PrimesFile,
      PrimesLimit,
      ShowAdb,
    }

    public CommandLineParser(string[] args)
    {
      OptionValues = new Dictionary<OptionName, string>();
      foreach(string arg in args)
      {
        if (!arg.StartsWith("-"))
        {
          throw new ArgumentException("Argument [" + arg + "] does not start with dash");
        }
        if (arg.Length < 2)
        {
          throw new ArgumentException("Argument [" + arg + "] is only a dash with no other information");
        }
        string nameAndValue = arg.Substring(1);
        string[] a = nameAndValue.Split('=');
        string sOptionName = a[0];
        string optionValue = (a.Length > 1) ? a[1] : "";
        OptionName optionName = OptionName.Undefined;
        if (!Enum.TryParse<OptionName>(sOptionName, true, out optionName))
        {
          throw new ArgumentException("Argument [" + arg + "] has unrecognised option name [" + sOptionName + "]");
        }
        OptionValues[optionName] = optionValue;
      }
    }

    public bool HasOption(OptionName optionName)
    {
      return OptionValues.ContainsKey(optionName);
    }

    public string OptionValue(OptionName optionName, string defaultOptionValue)
    {
      if (!OptionValues.ContainsKey(optionName)) { return defaultOptionValue; }
      string optionValue = OptionValues[optionName];
      if (optionValue.Length == 0) { return defaultOptionValue; }
      return optionValue;
    }
  }
}
