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
    /// Interaction logic for ProjectList.xaml
    /// </summary>
    public partial class ProjectList : UserControl
    {
        public ProjectList()
        {
            InitializeComponent();
        }

        private Workday? Data
        {
            get => DataContext as Workday;
            set { DataContext = value; }
        }

        private void StartWorkEntry(object sender, MouseButtonEventArgs e)
        {
            Data?.AddWorkdayTaskCommand.Execute(null);
        }

        private void OnProjectNameKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddNewProject();
            }
        }

        private void OnNewProjectClick(object sender, RoutedEventArgs e)
        {
            AddNewProject();
        }

        private void AddNewProject()
        {
            Data?.AddProjectCommand.Execute(null);
            ProjectNameText.Focus();
            ProjectNameText.SelectAll();
        }

        private void OnRemoveProjectClick(object sender, RoutedEventArgs e)
        {
            Data?.RemoveProjectCommand.Execute(null);
        }
    }
}
