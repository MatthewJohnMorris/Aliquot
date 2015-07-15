﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Numerics;
using System.Text;
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

    static Action CreateActionMakePrimes(
      string primesFile,
      string primesLimit,
      Progress<ProgressEventArgs> handler)
    {
      Action ret = delegate
      {
        int sieveLimit = Convert.ToInt32(primesLimit);
        PrimesGeneratorSieveErat.Generate(primesFile, sieveLimit, handler);
      };
      return ret;
    }

    static Action CreateActionMakeAliquotDb(
      string primesFile,
      string adbLimit,
      string adbFile,
      Progress<ProgressEventArgs> handler)
    {
      Action ret = delegate
      {
        IPrimes p = new PrimesFromFile(primesFile, handler);
        int dbLimit = int.Parse(adbLimit);
        var adb = AliquotDatabase.Create(p, dbLimit, handler);
        adb.SaveAs(adbFile);
      };
      return ret;
    }

    private class ActionReadPrimesFromFile
    {
      public string PrimesFile { get; private set; }
      public Progress<ProgressEventArgs> Handler { get; private set; }
      public PrimesFromFile Result { get; private set; }
      public ActionReadPrimesFromFile(
        string primesFile,
        Progress<ProgressEventArgs> handler)
      {
        PrimesFile = primesFile;
        Handler = handler;
      }
      public void Run()
      {
        Result = new PrimesFromFile(PrimesFile, Handler);
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

    private void Try_Button_Click(object sender, RoutedEventArgs e)
    { 
      if(sender == this.buttonReadPrimes)
      {
        ProgressWindow w = new ProgressWindow();
        var a = new ActionReadPrimesFromFile(
          this.textPrimesFile.Text, 
          w.CreateProgressReporter());
        w.LaunchModal(a.Run, "Read Primes");
        if(a.Result == null)
        {
          throw new ApplicationException("No Primes File was opened for " + this.textPrimesFile.Text);
        }
        else
        {
          MessageBox.Show(a.Result.ToString());
        }
      }
      else if (sender == this.buttonMakePrimes)
      {
        if(! GetUserConfirmationOfAction(
          string.Format(
            "This will generate primes up to {0:N0} into {1}. Are you sure you want to do this?",
            this.textPrimesLimit.Text, this.textPrimesFile.Text)))
        {
          return;
        }
        ProgressWindow w = new ProgressWindow();
        var a = CreateActionMakePrimes(
          this.textPrimesFile.Text,
          this.textPrimesLimit.Text,
          w.CreateProgressReporter());
        w.LaunchModal(a, "Make Primes");
      }
      else if (sender == this.buttonReadAliquotDB)
      {
        var db = AliquotDatabase.Open(this.textAdbFile.Text);
        var sb = new StringBuilder();
        foreach(var key in db.CreationProperties.Keys)
        {
          sb.AppendLine(string.Format("{0}={1}", key, db.CreationProperties[key]));
        }
        MessageBox.Show(sb.ToString(), "Aliquot DB " + this.textAdbFile.Text, MessageBoxButton.OK, MessageBoxImage.Information);
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
        ProgressWindow w = new ProgressWindow();
        var a = CreateActionMakeAliquotDb(
          this.textPrimesFile.Text,
          this.textAdbLimit.Text,
          this.textAdbFile.Text,
          w.CreateProgressReporter());
        w.LaunchModal(a, "Make Aliquot DB");
      }
      else if (sender == this.buttonFindGvDotExe)
      {
        GraphViz.FindDotExe(GetUserInput_Int32_Gui);
        if(GraphViz.HasDotExeLocation())
        {
          MessageBox.Show("New GraphViz Dot.Exe Location: " + GraphViz.GetDotExeLocation());
        }
      }
      else if (sender == this.buttonMakeTree)
      {
        string adbName = this.textAdbFile.Text;
        string sTreeRoot = this.textTreeRoot.Text;
        BigInteger treeRoot = BigInteger.Parse(sTreeRoot);
        string sTreeLimit = this.textTreeLimit.Text;
        BigInteger treeLimit = BigInteger.Parse(sTreeLimit);
        var db = AliquotDatabase.Open(adbName);
        string gvOut = this.textTreeFile.Text;
        using (var writer = new StreamWriter(gvOut + ".gv"))
        {
          db.WriteTree(treeRoot, treeLimit, writer);
        }
        GraphViz.RunDotExe(gvOut, "svg");
        MessageBox.Show("File written to " + System.IO.Path.GetFullPath(gvOut + ".svg"), "Aliquot Tree Creation", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      else if (sender == this.buttonExportAdb)
      {
        string adbName = this.textAdbFile.Text;
        var db = AliquotDatabase.Open(adbName);
        string sExportLimit = this.textExportLimit.Text;
        BigInteger exportLimit = BigInteger.Parse(sExportLimit);
        string exportFile = this.textExportFile.Text;
        AliquotDatabase.ExportFormat exportFormat = AliquotDatabase.ExportFormat.Tsv;
        if(exportFile.EndsWith("csv", StringComparison.OrdinalIgnoreCase))
        {
          exportFormat = AliquotDatabase.ExportFormat.Csv;
        }
        using(var writer = new StreamWriter(exportFile))
        {
          db.ExportTable(writer, exportLimit, exportFormat);
        }
        MessageBox.Show(
          string.Format("File up to {0} written to {1}", sExportLimit, System.IO.Path.GetFullPath(exportFile)),
          "Aliquot DB Table Export", 
          MessageBoxButton.OK, 
          MessageBoxImage.Information);
      }
      else if (sender == this.buttonOpenExplorer)
      {
        string directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        System.Diagnostics.Process.Start(directory);
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
