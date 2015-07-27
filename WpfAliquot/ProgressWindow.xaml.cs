using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Windows.Shapes;

namespace WpfAliquot
{
  /// <summary>
  /// Interaction logic for Progress.xaml
  /// </summary>
  public partial class ProgressWindow : Window
  {
    public string Description { get; private set; }

    public CancellationTokenSource myCancellationTokenSource;
    public CancellationToken CancellationToken { get { return myCancellationTokenSource.Token; } }

    public Progress<Aliquot.Common.ProgressEventArgs> ProgressReporter { get; private set; }

    public ProgressWindow()
    {
      Description = "(not set)";
      myCancellationTokenSource = new CancellationTokenSource();
      ProgressReporter = new Progress<Aliquot.Common.ProgressEventArgs>(this.ReportProgress);

      InitializeComponent();
    }

    public static ProgressWindow CreateWithDescription(string description)
    {
      ProgressWindow ret = new ProgressWindow();
      ret.Description = description ?? "(null description passed)";
      ret.Show();
      ret.Activate();
      return ret;
    }

    private void ReportProgress(Aliquot.Common.ProgressEventArgs args)
    {
      this.progressbar.Value = args.Percent;
      this.textboxDescription.Text = args.Message;
    }
  
    private void buttonCancel_Click(object sender, RoutedEventArgs e)
    {
      myCancellationTokenSource.Cancel();
    }

    private void Window_Closed(object sender, EventArgs e)
    {
      myCancellationTokenSource.Cancel();
    }

  }
}
