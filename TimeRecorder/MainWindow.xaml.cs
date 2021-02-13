using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TimeRecorder.Models;

namespace TimeRecorder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly static JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _jsonOptions.Converters.Add(new TimeSpanConverter());

                var currentWorkDay = LoadCurrentWorkday();
                currentWorkDay.StartTimer();

                currentWorkDay.CurrentProject = currentWorkDay.LastUsedProject;
                currentWorkDay.CurrentWorkdayTask = currentWorkDay.LastUsedWorkdayTask;
                currentWorkDay.SetActiveWorkingTask(currentWorkDay.LastUsedWorkdayTask);

                DataContext = currentWorkDay;

                StartTimer();
            }
            catch (Exception ex)
            {
                ShowError(ex, true);
            }
        }

        private static void ShowError(Exception ex, bool exit = false, [CallerMemberName] string memberName = "ERROR!")
        {
            var msg = JsonSerializer.Serialize(new {
                Timestamp = DateTimeOffset.Now,
                MemberName = memberName,
                Message = ex.Message,
                Stacktrace = ex?.StackTrace?.ToString(),
                InnerMessage = ex?.InnerException?.Message,
                InnerStacktrace = ex?.InnerException?.StackTrace?.ToString()
            }, _jsonOptions);

            Clipboard.SetText(msg);

            MessageBox.Show($"{msg}", "ERROR - Remember: Error is copied to Clipboard!");
            File.WriteAllText($"{DateTimeOffset.Now.ToFileTime()}_ErrorLog.txt", msg);

            Clipboard.SetText(msg);

            if (exit)
            {
                Environment.Exit(1);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            SaveCurrentWorkdayAsync().Wait();
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
            var workday = LoadWorkday(path, true);

            if (workday != null)
            {
                return workday;
            }

            var loginDate = DateTimeOffset.Now.AddMilliseconds(-Environment.TickCount);
            // Use time from login if from today
            loginDate = (loginDate.Date == DateTimeOffset.Now.Date) ? loginDate : DateTimeOffset.Now;

            var orderedFiles = new DirectoryInfo(".")
                .EnumerateFiles("*_Workday.json", new EnumerationOptions { IgnoreInaccessible = true })
                .OrderByDescending(x => x.Name);

            foreach (var file in orderedFiles)
            {
                var oldWorkday = LoadWorkday(file.FullName, false);
                if (oldWorkday != null)
                {
                    // load old projects
                    return new Workday() {
                        WorkBegin = loginDate,
                        AllProjects = oldWorkday.AllProjects, 
                        PlannedWorkingHours = oldWorkday.PlannedWorkingHours,
                    };
                }
            }

            // no file found
            return new Workday()
            {
                WorkBegin = loginDate,
            };
        }

        private Workday? LoadWorkday(string path, bool copyFailedFile)
        {
            if (File.Exists(path))
            {
                try
                {
                    var content = File.ReadAllText(path);
                    Workday? workday = JsonSerializer.Deserialize<Workday>(content, _jsonOptions);

                    try
                    {
                        var backupDir = $"backup/{DateTimeOffset.Now.DayOfWeek}";
                        var backupDirInfo = new DirectoryInfo(backupDir);
                        if (backupDirInfo.Exists && backupDirInfo.CreationTimeUtc < DateTimeOffset.UtcNow.Date)
                        {
                            Directory.Delete(backupDir);
                        }
                        Directory.CreateDirectory(backupDir);
                        File.Copy(path, Path.Combine(backupDir, $"{Path.GetFileName(path)}.{DateTimeOffset.Now.ToFileTime()}"), true);
                    }
                    catch (Exception ex)
                    {
                        ShowError(ex);
                    }

                    workday?.UpdateProjectReferences();
                    return workday;
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                    if (copyFailedFile)
                    {
                        File.Copy(path, $"{path}.Error{DateTimeOffset.Now:yyyyMMddHHmmss}.json");
                    }
                    return null;
                }
            }

            return null;
        }

        private Task SaveCurrentWorkdayAsync()
        {
            try
            {
                var data = Data;
                if (data != null)
                {
                    var content = JsonSerializer.Serialize(Data, _jsonOptions);
                    return File.WriteAllTextAsync(GetWorkdayPath(), content);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
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
