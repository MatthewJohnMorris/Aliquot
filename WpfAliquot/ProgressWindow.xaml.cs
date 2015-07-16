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
    public bool IsLaunchedAsDialog { get; private set; }
    private Task myTask = null;
    private string myDescripton = null;
    private CancellationTokenSource myCancellationTokenSource;

    public ProgressWindow()
    {
      IsLaunchedAsDialog = false;
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
        MessageBox.Show(sb.ToString(), "Exception Found In Task '" + myDescripton + "'", MessageBoxButton.OK, MessageBoxImage.Exclamation);
      }
      else
      {
        if (!IsLaunchedAsDialog)
        {
          MessageBox.Show("Task Completed Successfully", "Task '" + myDescripton + "'", MessageBoxButton.OK, MessageBoxImage.Information);
        }
      }
      this.Dispatcher.Invoke(this.Close);
    }

    public enum LaunchType
    {
      Modal,
      Interactive
    };
    public void Launch(Action action, string description, LaunchType launchType = LaunchType.Modal)
    {
      IsLaunchedAsDialog = (launchType == LaunchType.Modal);
      this.myTask = Task.Run(action);
      this.myDescripton = description ?? "(null description passed)";
      myTask.ContinueWith(this.UponTaskCompletion);
      if(IsLaunchedAsDialog)
      {
        this.ShowDialog();
      }
      else
      {
        this.Show();
        this.Activate();
      }
    }

    public Task<T> LaunchAsync<T>(Func<T> func, string description)
    {
      IsLaunchedAsDialog = false;
      // this.myTask = Task.Run(func);
      var t = Task.Run(func);
      this.myDescripton = description ?? "(null description passed)";
      this.Show();
      this.Activate();
      t.ContinueWith(this.UponTaskCompletion);
      return t;
    }

    public T LaunchModal<T>(Func<T> func, string description)
    {
      IsLaunchedAsDialog = true;
      // this.myTask = Task.Run(func);
      var t = Task.Run(func);
      this.myDescripton = description ?? "(null description passed)";
      t.ContinueWith(this.UponTaskCompletion);
      this.ShowDialog();
      return t.Result;
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
