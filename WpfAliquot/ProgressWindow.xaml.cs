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
using System.Windows.Shapes;

namespace WpfAliquot
{
  /// <summary>
  /// Interaction logic for Progress.xaml
  /// </summary>
  public partial class ProgressWindow : Window
  {
    private Task myTask = null;
    private string myDescripton = null;

    public ProgressWindow()
    {
      InitializeComponent();
    }

    private void UponTaskCompletion(Task task)
    {
      if(task.Exception != null)
      {
        var sb = new StringBuilder();
        sb.AppendLine(task.Exception.Message);
        foreach(var ex in task.Exception.InnerExceptions)
        {
          sb.AppendLine("-" + ex.Message);
        }
        MessageBox.Show(sb.ToString(), "Exception Found In Task '" + myDescripton + "'", MessageBoxButton.OK, MessageBoxImage.Exclamation);
      }
      else
      {
        MessageBox.Show("Task Completed Successfully", "Task '" + myDescripton + "'", MessageBoxButton.OK, MessageBoxImage.Information);
      }
      this.Dispatcher.Invoke(this.Close);
    }

    public void Launch(Action action, string description)
    {
      this.myTask = Task.Run(action);
      this.myDescripton = description ?? "(null description passed)";
      myTask.ContinueWith(this.UponTaskCompletion);
      this.Show();
      this.Activate();
    }

    public void LaunchModal(Action action, string description)
    {
      this.myTask = Task.Run(action);
      this.myDescripton = description ?? "(null description passed)";
      myTask.ContinueWith(this.UponTaskCompletion);
      this.ShowDialog();
    }

    public Progress<Aliquot.Common.ProgressEventArgs> CreateProgressReporter()
    {
      return new Progress<Aliquot.Common.ProgressEventArgs>(this.ReportProgress);
    }
    private void ReportProgress(Aliquot.Common.ProgressEventArgs args)
    {
      this.progressbar.Value = args.Percent;
      this.textboxDescription.Text = args.Message;
    }
  
  }
}
