using System;
using System.Collections.Generic;
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
    private void Try_Button_Click(object sender, RoutedEventArgs e)
    {
      if(sender == this.buttonMakeAliquotDB)
      {
        IPrimes p = new PrimesFromFile(this.textPrimesFile.Text, PrimesFromFile.ShowLoadProgress.No);
        int dbLimit = int.Parse(this.textAdbLimit.Text);
        var adb = AliquotDatabase.Create(p, dbLimit);
        adb.SaveAs(this.textAdbFile.Text);
      }
      else if(sender == this.buttonMakePrimes)
      {
        PrimesSieveErat p = new PrimesSieveErat(100000);
        string primesFile = this.textPrimesFile.Text;
        p.WriteToFile(primesFile);
      }
    }
  }
}
