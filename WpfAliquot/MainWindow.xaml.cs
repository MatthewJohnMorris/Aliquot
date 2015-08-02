using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Aliquot.Common;
using ProgressEventArgs = Aliquot.Common.ProgressEventArgs;

namespace WpfAliquot
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private bool IsInitialised = false;
    public MainWindow()
    {
      InitializeComponent();

      IsInitialised = true;

      UpdateAccordingToGeneratedFiles();
    }

    private void UpdateFileTextBoxBackground(
      TextBox textbox,
      Button buttonToFixMissingFile,
      Button[] buttonsToDeactivateIfMissingFile,
      string fileName = "")
    {
      if (fileName == "") { fileName = textbox.Text; }
      if (! File.Exists(fileName))
      {
        textbox.Background = Brushes.Pink;
        buttonToFixMissingFile.Background = Brushes.LightGreen;
        foreach(var button in buttonsToDeactivateIfMissingFile)
        {
          button.IsEnabled = false;
        }
      }
      else
      {
        textbox.Background = Brushes.White;
        buttonToFixMissingFile.Background = this.buttonMakeTree.Background;
      }
    }
    private void EnableButtonIfPresent(Button button)
    {
      if(button != null)
      {
        button.IsEnabled = true;
      }
    }
    private void UpdateAccordingToGeneratedFiles()
    {
      if (!IsInitialised) { return; }

      try
      {
        this.textGvDotExeFile.Text = GraphViz.GetDotExeLocation();
      }
      catch (Exception e)
      {
        this.textGvDotExeFile.Text = e.Message;
      }

      EnableButtonIfPresent(this.buttonReadPrimes);
      EnableButtonIfPresent(this.buttonMakeAliquotDB);
      EnableButtonIfPresent(this.buttonReadAliquotDB);
      EnableButtonIfPresent(this.buttonMakeTree);
      EnableButtonIfPresent(this.buttonExportAdb);

      UpdateFileTextBoxBackground(
        this.textPrimesFile,
        this.buttonMakePrimes,
        new Button[] {
          this.buttonReadPrimes,
          this.buttonMakeAliquotDB});
      UpdateFileTextBoxBackground(
        this.textAdbFile,
        this.buttonMakeAliquotDB,
        new Button[] {
          this.buttonReadAliquotDB,
          this.buttonMakeTree,
          this.buttonExportAdb});
      UpdateFileTextBoxBackground(
        this.textGvDotExeFile,
        this.buttonFindGvDotExe,
        new Button[] {
          this.buttonMakeTree },
        GraphViz.FileNameGvDotLocation);
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        Try_Button_Click(sender, e);
      }
      catch(Exception ex)
      {
        MessageBox.Show("Exception: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
      }
    }

    static void MakePrimesFile(
      string primesFile,
      string primesLimit,
      Progress<ProgressEventArgs> handler,
      CancellationToken cancellationToken)
    {
      int sieveLimit = Convert.ToInt32(primesLimit);
      PrimesGeneratorSieveErat.Generate(primesFile, sieveLimit, handler, cancellationToken);
    }

    static void MakeAliquotDb(
      string primesFile,
      string adbLimit,
      string adbFile,
      Progress<ProgressEventArgs> handler,
      CancellationToken cancellationToken)
    {
      IPrimes p = new PrimesFromFile(primesFile, handler, cancellationToken);
      int dbLimit = int.Parse(adbLimit);
      var adb = AliquotDatabase.Create(p, dbLimit, handler, cancellationToken);
      adb.SaveAs(adbFile);
    }

    void MakeTreeGraph(
      string sTreeRoot,
      string sTreeLimit,
      string adbName,
      string gvOut,
      Progress<ProgressEventArgs> handler,
      CancellationToken cancellationToken)
    {
      BigInteger treeRoot = BigInteger.Parse(sTreeRoot);
      BigInteger treeLimit = BigInteger.Parse(sTreeLimit);
      var db = AliquotDatabase.Open(adbName, handler, cancellationToken);
      using (var writer = new StreamWriter(gvOut + ".gv"))
      {
        db.WriteTree(treeRoot, treeLimit, writer);
      }
      GraphViz.RunDotExe(gvOut, "svg", handler, cancellationToken);
    }

    void MakeChainGraph(
      string sChainStart,
      string adbName,
      string gvOut,
      Progress<ProgressEventArgs> handler,
      CancellationToken cancellationToken)
    {
      BigInteger chainStart = BigInteger.Parse(sChainStart);
      var db = AliquotDatabase.Open(adbName, handler, cancellationToken);
      using (var writer = new StreamWriter(gvOut + ".gv"))
      {
        db.WriteChain(chainStart, writer);
      }
      GraphViz.RunDotExe(gvOut, "svg");
    }

    private void ShowInfoDialog(string message, string caption)
    {
      MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Information);
    }
    private void ShowExceptionDialog(Exception e, string caption)
    {
      var sb = new StringBuilder();
      if(e is AggregateException)
      {
        AggregateException ae = e as AggregateException;
        sb.Append(string.Format("Aggregate Exception with {0} member(s)", ae.InnerExceptions.Count));
        var messages = new HashSet<string>();
        foreach (var ie in (e as AggregateException).InnerExceptions)
        {
          if(! messages.Contains(ie.Message))
          {
            messages.Add(ie.Message);
          }
        }
        foreach(var message in messages)
        {
          sb.Append("\n* " + message);
        }
      }
      else
      {
        sb.Append(e.Message);
      }
      var res = MessageBox.Show(sb.ToString(), "Error: " + caption, MessageBoxButton.OK, MessageBoxImage.Error);
      if(res == MessageBoxResult.No)
      {
        Console.Out.WriteLine("ShowExceptionDialog failed on thread " + System.Threading.Thread.CurrentThread.ManagedThreadId);
      }
    }
    private bool GetUserConfirmationOfAction(string description)
    {
      return (MessageBoxResult.Yes == MessageBox.Show(
        description,
        "Confirm Action",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question));
    }

    async private void ReadPrimesAsync()
    {
      ProgressWindow w = ProgressWindow.CreateWithDescription("Read Primes");
      string primesFile = this.textPrimesFile.Text;
      var handler = w.ProgressReporter;
      var ct = w.CancellationToken;
      Func<PrimesFromFile> f = () => new PrimesFromFile(primesFile, handler, ct);
      try
      {
        Console.Out.WriteLine("ReadPrimesAsync launched on thread " + System.Threading.Thread.CurrentThread.ManagedThreadId);
        PrimesFromFile result = await Task.Run(f);
        UpdateAccordingToGeneratedFiles();
        ShowInfoDialog(result.ToString(), "Read Primes");
      }
      catch(Exception e)
      {
        ShowExceptionDialog(e, "Read Primes");
      }
      w.Close();
    }
    async private void MakePrimesAsync()
    {
      ProgressWindow w = ProgressWindow.CreateWithDescription("Make Primes");
      string primesFile = this.textPrimesFile.Text;
      string primesLimit = this.textPrimesLimit.Text;
      var handler = w.ProgressReporter;
      var ct = w.CancellationToken;
      Action a = () => MakePrimesFile(primesFile, primesLimit, handler, ct);
      try
      {
        await Task.Run(a);
        UpdateAccordingToGeneratedFiles();
      }
      catch(Exception e)
      {
        ShowExceptionDialog(e, "Make Primes");
      }
      w.Close();
    }
    async private void ReadAliquotDbAsync()
    {
      ProgressWindow w = ProgressWindow.CreateWithDescription("Read Aliquot DB");

      string adbFile = this.textAdbFile.Text;

      var handler = w.ProgressReporter;
      var ct = w.CancellationToken;
      try
      {
        Func<AliquotDatabase> f = () => AliquotDatabase.Open(adbFile, handler, ct);
        AliquotDatabase db = await Task.Run(f);
        UpdateAccordingToGeneratedFiles();
        var sb = new StringBuilder();
        foreach (var key in db.CreationProperties.Keys)
        {
          sb.AppendLine(string.Format("{0}={1}", key, db.CreationProperties[key]));
        }
        ShowInfoDialog(sb.ToString(), "Aliquot DB " + this.textAdbFile.Text);
      }
      catch (Exception e)
      {
        ShowExceptionDialog(e, "Read Aliquot DB");
      }
      w.Close();

    }
    async private void MakeAliquotDbAsync()
    {
      ProgressWindow w = ProgressWindow.CreateWithDescription("Make Aliquot DB");
      string primesFile = this.textPrimesFile.Text;
      string adbLimit = this.textAdbLimit.Text;
      string adbFile = this.textAdbFile.Text;
      var handler = w.ProgressReporter;
      var ct = w.CancellationToken;
      Action a = () => MakeAliquotDb(primesFile, adbLimit, adbFile, handler, ct);
      try
      {
        await Task.Run(a);
        UpdateAccordingToGeneratedFiles();
      }
      catch (Exception e)
      {
        ShowExceptionDialog(e, "Make Aliquot DB");
      }
      w.Close();
    }
    async private void MakeTreeGraphAsync()
    {
      ProgressWindow w = ProgressWindow.CreateWithDescription("Make Tree");
      string adbName = this.textAdbFile.Text;
      string sTreeRoot = this.textTreeRoot.Text;
      string sTreeLimit = this.textTreeLimit.Text;
      string gvOut = this.textTreeFile.Text;
      var handler = w.ProgressReporter;
      var ct = w.CancellationToken;
      Action a = () => 
        MakeTreeGraph(
          sTreeRoot,
          sTreeLimit,
          adbName,
          gvOut,
          handler, ct);
      try
      {
        await Task.Run(a);
        var fullPath = System.IO.Path.GetFullPath(gvOut + ".svg");
        if (checkboxOpenNewTrees.IsChecked ?? false)
        {
          Process.Start(fullPath);
        }
        else
        {
          ShowInfoDialog("File written to " + fullPath, "Aliquot Tree Creation");
        }
      }
      catch (Exception e)
      {
        ShowExceptionDialog(e, "Make Tree");
      }
      w.Close();
    }
    async private void MakeChainGraphAsync()
    {
      ProgressWindow w = ProgressWindow.CreateWithDescription("Make Tree");
      string adbName = this.textAdbFile.Text;
      string sChainStart = this.textChainStart.Text;
      string gvOut = this.textTreeFile.Text;
      var handler = w.ProgressReporter;
      var ct = w.CancellationToken;
      Action a = () =>
        MakeChainGraph(
          sChainStart,
          adbName,
          gvOut,
          handler,
          ct);
      try
      {
        await Task.Run(a);
        var fullPath = System.IO.Path.GetFullPath(gvOut + ".svg");
        if (checkboxOpenNewTrees.IsChecked ?? false)
        {
          Process.Start(fullPath);
        }
        else
        {
          ShowInfoDialog("File written to " + fullPath, "Aliquot Chain Creation");
        }
      }
      catch (Exception e)
      {
        ShowExceptionDialog(e, "Make Chain");
      }
      w.Close();
    }
    async private void ExportAliquotDbAsync()
    {
      ProgressWindow w = ProgressWindow.CreateWithDescription("Make Tree");
      string adbName = this.textAdbFile.Text;
      string sExportRangeFrom = this.textExportRangeFrom.Text;
      string sExportRangeTo = this.textExportRangeTo.Text;
      string exportFile = this.textExportFile.Text;
      var handler = w.ProgressReporter;
      var ct = w.CancellationToken;
      Action a = () =>
        {
          var db = AliquotDatabase.Open(adbName, handler, ct);
          BigInteger exportRangeFrom = BigInteger.Parse(sExportRangeFrom);
          BigInteger exportRangeTo = BigInteger.Parse(sExportRangeTo);
          AliquotDatabase.ExportFormat exportFormat = AliquotDatabase.ExportFormat.Tsv;
          if (exportFile.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
          {
            exportFormat = AliquotDatabase.ExportFormat.Csv;
          }
          using (var writer = new StreamWriter(exportFile))
          {
            db.ExportTable(writer, exportRangeFrom, exportRangeTo, exportFormat);
          }
        };
      try
      {
        await Task.Run(a);
        ShowInfoDialog(
          string.Format("File for [{0}, {1}] written to {2}", sExportRangeFrom, sExportRangeTo, System.IO.Path.GetFullPath(exportFile)),
          "Aliquot Db Table Export");
      }
      catch (Exception e)
      {
        ShowExceptionDialog(e, "Export Aliquot Db");
      }
      w.Close();
    }

    private void Try_Button_Click(object sender, RoutedEventArgs e)
    { 
      if(sender == this.buttonReadPrimes)
      {
        ReadPrimesAsync();
      }
      else if (sender == this.buttonMakePrimes)
      {
        if (!GetUserConfirmationOfAction(
          string.Format(
            "This will generate primes up to {0:N0} into {1}. Are you sure you want to do this?",
            this.textPrimesLimit.Text, this.textPrimesFile.Text)))
        {
          return;
        }
        MakePrimesAsync();
      }
      else if (sender == this.buttonReadAliquotDB)
      {
        ReadAliquotDbAsync();
      }
      else if(sender == this.buttonMakeAliquotDB)
      {
        if (!GetUserConfirmationOfAction(
          string.Format(
            "This will generate aliquot chains up to {0:N0} into {1}. Are you sure you want to do this?",
            this.textAdbLimit.Text, this.textAdbFile.Text)))
        {
          return;
        }
        MakeAliquotDbAsync();
      }
      else if (sender == this.buttonFindGvDotExe)
      {
        GraphViz.FindDotExe(GetUserInput_Int32_Gui);
        if(GraphViz.HasDotExeLocation())
        {
          ShowInfoDialog("New GraphViz Dot.Exe Location: " + GraphViz.GetDotExeLocation(), "GraphViz Dot.Exe");
        }
      }
      else if (sender == this.buttonMakeTree)
      {
        MakeTreeGraphAsync();
      }
      else if (sender == this.buttonMakeChain)
      {
        MakeChainGraphAsync();
      }
      else if (sender == this.buttonExportAdb)
      {
        ExportAliquotDbAsync();
      }
      else if (sender == this.buttonOpenExplorer)
      {
        string directory = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        Process.Start(directory);
      }
      UpdateAccordingToGeneratedFiles();
      return;
    }

    public static int GetUserInput_Int32_Gui(List<string> choices)
    {
      return 0;
    }

    private void textPrimesFile_TextChanged(object sender, TextChangedEventArgs e)
    {
      UpdateAccordingToGeneratedFiles();
    }

    private void textAdbFile_TextChanged(object sender, TextChangedEventArgs e)
    {
      UpdateAccordingToGeneratedFiles();
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
      string s = ((MenuItem)sender).Header.ToString();
      if(0 == string.Compare(s, "Processor...", StringComparison.OrdinalIgnoreCase))
      {
        MenuCommand_Processor();
      }
    }

    private void MenuCommand_Processor()
    {
      var sb = new StringBuilder();
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
          sb.AppendLine(string.Format("* Processor: {0:N3}GHz [{1} {2} {3}]", dClockSpeedGHz, manufacturer, procName, version));
        }
      }
      if (dMaxClockSpeedGHz == 0.0)
      {
        sb.AppendLine("Sorry: couldn't get any processor speed information!");
      }
      else
      {
        double speedRatio = dMaxClockSpeedGHz / 3.166; // actual speed on this machine!
        sb.AppendLine(string.Format("Processor Max Speed: {0:N3}", dMaxClockSpeedGHz));
        sb.AppendLine(string.Format("Processor Speed Ratio: {0:N3}", speedRatio));
      }
      MessageBox.Show(
        sb.ToString(),
        "Processor Information",
        MessageBoxButton.OK, 
        MessageBoxImage.Information);
      
    }

  }
}
