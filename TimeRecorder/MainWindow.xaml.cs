using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
using TimeRecorder.Models;

namespace TimeRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public MainWindow()
        {
            InitializeComponent();

            _jsonOptions.Converters.Add(new TimeSpanConverter());

            var currentWorkDay = LoadCurrentWorkday();
            currentWorkDay.StartTimer();

            currentWorkDay.CurrentProject = currentWorkDay.AllProjects.LastOrDefault();
            currentWorkDay.CurrentWorkdayTask = currentWorkDay.WorkdayTasks.LastOrDefault();

            DataContext = currentWorkDay;

            StartTimer();
        }

        private readonly TimeSpan TICK_DURATION = TimeSpan.FromSeconds(10);

        public string GetWorkdayPath()
        {
            return GetWorkdayPath(DateTimeOffset.Now);
        }

        public string GetWorkdayPath(DateTimeOffset date)
        {
            return $"{date:yyyyMMdd}_Workday.json";
        }

        public void StartTimer()
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimerTick);
            dispatcherTimer.Interval = TICK_DURATION;
            dispatcherTimer.Start();
        }

        private void DispatcherTimerTick(object? sender, EventArgs e)
        {
            SaveCurrentWorkdayAsync();
        }

        private Workday LoadCurrentWorkday()
        {
            var path = GetWorkdayPath();
            if (File.Exists(path))
            {
                try
                {
                    var content = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<Workday>(content, _jsonOptions);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    File.Copy(path, $"{path}.Error{DateTimeOffset.Now:yyyyMMddHHmmss}.json");
                    return new Workday();
                }
            }
            else
            {
                return new Workday();
            }
        }

        private Task SaveCurrentWorkdayAsync()
        {
            try
            {
                var content = JsonSerializer.Serialize(Data, _jsonOptions);
                return File.WriteAllTextAsync(GetWorkdayPath(), content);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return Task.CompletedTask;
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

        private void OnRemoveWorkEntryClick(object sender, RoutedEventArgs e)
        {
            Data?.RemoveWorkdayTaskCommand.Execute(null);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SaveCurrentWorkdayAsync().Wait();
        }
    }
}
