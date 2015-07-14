using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

    private class ActionMakePrimes
    {
      private string myPrimesFile;
      private string myPrimesLimit;
      private Progress<ProgressEventArgs> myHandler;
      public ActionMakePrimes(
        string primesFile,
        string primesLimit,
        Progress<ProgressEventArgs> handler)
      {
        myPrimesFile = primesFile;
        myPrimesLimit = primesLimit;
        myHandler = handler;
      }
      public void Run()
      {
        int sieveLimit = Convert.ToInt32(myPrimesLimit);
        PrimesGeneratorSieveErat.Generate(myPrimesFile, sieveLimit, myHandler);
      }
    }

    private class ActionMakeAliquotDb
    {
      private string myPrimesFile;
      private string myAdbLimit;
      private string myAdbFile;
      private Progress<ProgressEventArgs> myHandler;
      public ActionMakeAliquotDb(
        string primesFile,
        string adbLimit,
        string adbFile,
        Progress<ProgressEventArgs> handler)
      {
        myPrimesFile = primesFile;
        myAdbLimit = adbLimit;
        myAdbFile = adbFile;
        myHandler = handler;
      }
      public void Run()
      {
        IPrimes p = new PrimesFromFile(myPrimesFile, myHandler);
        int dbLimit = int.Parse(myAdbLimit);
        var adb = AliquotDatabase.Create(p, dbLimit, myHandler);
        adb.SaveAs(myAdbFile);
      }
    }

    private class ActionReadPrimesFromFile
    {
      public IPrimes PrimesFromFile { get; private set; }
      private string myPrimesFile;
      private Progress<ProgressEventArgs> myHandler;
      public ActionReadPrimesFromFile(
        string primesFile,
        Progress<ProgressEventArgs> handler)
      {
        myPrimesFile = primesFile;
        myHandler = handler;
      }
      public void Run()
      {
        PrimesFromFile = new PrimesFromFile(myPrimesFile, myHandler);
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
        var a = new ActionReadPrimesFromFile(this.textPrimesFile.Text, w.CreateProgressReporter());
        w.LaunchModal(a.Run, "Read Primes");
        if(a.PrimesFromFile == null)
        {
          throw new ApplicationException("No Primes File was opened for " + this.textPrimesFile.Text);
        }
        else
        {
          MessageBox.Show(a.PrimesFromFile.ToString());
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
        var a = new ActionMakePrimes(
          this.textPrimesFile.Text,
          this.textPrimesLimit.Text,
          w.CreateProgressReporter());
        w.Launch(a.Run, "Make Primes");
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
        var a = new ActionMakeAliquotDb(
          this.textPrimesFile.Text,
          this.textAdbLimit.Text,
          this.textAdbFile.Text,
          w.CreateProgressReporter());
        w.Launch(a.Run, "Make Aliquot DB");
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
        MessageBox.Show("File written to " + gvOut + ".svg", "Aliquot Tree Creation", MessageBoxButton.OK, MessageBoxImage.Information);
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
          string.Format("File up to {0} written to {1}", sExportLimit, exportFile),
          "Aliquot DB Table Export", 
          MessageBoxButton.OK, 
          MessageBoxImage.Information);
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

  }
}
