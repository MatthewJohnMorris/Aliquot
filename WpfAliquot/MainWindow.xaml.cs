using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public MainWindow()
    {
      InitializeComponent();

      try
      {
        this.textGvDotExeFile.Text = GraphViz.GetDotExeLocation();
      }
      catch(Exception e)
      {
        this.textGvDotExeFile.Text = e.Message;
      }
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
        PrimesSieveErat p = new PrimesSieveErat(sieveLimit, myHandler);
        p.WriteToFile(myPrimesFile);
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

    private void Try_Button_Click(object sender, RoutedEventArgs e)
    { 
      if(sender == this.buttonReadPrimes)
      {
        ProgressWindow w = new ProgressWindow();
        var a = new ActionReadPrimesFromFile(this.textPrimesFile.Text, w.CreateProgressReporter());
        w.LaunchModal(a.Run, "Read Primes");
        MessageBox.Show(a.PrimesFromFile.ToString());
      }
      else if (sender == this.buttonMakePrimes)
      {
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
        ProgressWindow w = new ProgressWindow();
        var a = new ActionMakeAliquotDb(
          this.textPrimesFile.Text,
          this.textAdbLimit.Text,
          this.textAdbFile.Text,
          w.CreateProgressReporter());
        w.Launch(a.Run, "Make Aliquot DB");
      }
    }
  }
}
