﻿using System;
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
    private string myDescription = null;
    private CancellationTokenSource myCancellationTokenSource;

    public ProgressWindow()
    {
      myCancellationTokenSource = new CancellationTokenSource();

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
        MessageBox.Show(sb.ToString(), "Exception Found In Task '" + myDescription + "'", MessageBoxButton.OK, MessageBoxImage.Exclamation);
      }
      this.Dispatcher.Invoke(this.Close);
    }

    public Task LaunchAsync(Action action, string description)
    {
      var task = Task.Run(action);
      this.myDescription = description ?? "(null description passed)";
      task.ContinueWith(this.UponTaskCompletion);
      this.Show();
      this.Activate();
      return task;
    }

    public Task<T> LaunchAsync<T>(Func<T> func, string description)
    {
      var task = Task.Run(func);
      this.myDescription = description ?? "(null description passed)";
      this.Show();
      this.Activate();
      task.ContinueWith(this.UponTaskCompletion);
      return task;
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
  
    public CancellationToken GetCancellationToken()
    {
      return myCancellationTokenSource.Token;
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
