using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TimeRecorder.Models;

namespace TimeRecorder.Views
{
    /// <summary>
    /// Interaction logic for TaskList.xaml
    /// </summary>
    public partial class TaskList : UserControl
    {
        public TaskList()
        {
            InitializeComponent();
        }

        private Workday? Data
        {
            get => DataContext as Workday;
            set { DataContext = value; }
        }

        private void OnRemoveWorkEntryClick(object sender, RoutedEventArgs e)
        {
            Data?.RemoveWorkdayTaskCommand.Execute(null);
        }

        private void SetActiveWorkdayTask(object sender, MouseButtonEventArgs e)
        {
            var workday = Data;
            var workdayTask = Data?.CurrentWorkdayTask;
            if (workday != null && workdayTask != null)
            {
                workday.SetActiveWorkingTask(workdayTask);
            }
        }

        private void OnChangeWorkdayTaskProjectClick(object sender, RoutedEventArgs e)
        {
            var project = Data?.CurrentProject;
            var workdayTask = Data?.CurrentWorkdayTask;
            if (workdayTask != null && project != null)
            {
                workdayTask.Project = project;
            }
        }
    }
}
