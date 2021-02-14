using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TimeRecorder.Models;
using TimeRecorder.Properties;

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

                if (string.IsNullOrWhiteSpace(Settings.Default.WorkdayPath))
                {
                    Settings.Default.WorkdayPath = Path.GetFullPath(".");
                }

                WorkdayPathMenuItem.Header = Settings.Default.WorkdayPath;
                ProjectListPathMenuItem.Header = Settings.Default.ProjectListPath;
                ImportProjectsAtStartup.IsChecked = Settings.Default.ImportProjectsAtStartup;
                
                if (Settings.Default.ImportProjectsAtStartup)
                {
                    if (File.Exists(Settings.Default.ProjectListPath))
                    {
                        ImportProjectList(Settings.Default.ProjectListPath, currentWorkDay);
                    }
                }

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

        private void ImportProjectList(string path, Workday workday)
        {
            try
            {
                var content = File.ReadAllText(path);
                var importedProjectList = JsonSerializer.Deserialize<List<Project>?>(content, _jsonOptions);

                Settings.Default.ProjectListPath = path;
                Settings.Default.Save();
                ProjectListPathMenuItem.Header = Settings.Default.ProjectListPath;

                if (importedProjectList != null && importedProjectList.Count > 0)
                {
                    foreach (var importedProject in importedProjectList)
                    {
                        var foundProject = workday.AllProjects.Where(x => x.ProjectId == importedProject.ProjectId).FirstOrDefault();
                        if (foundProject != null)
                        {
                            // update
                            foundProject.ProjectName = importedProject.ProjectName;
                        }
                        else
                        {
                            // insert
                            workday.AllProjects.Add(importedProject);
                        }
                    }

                    workday.UpdateProjectReferences();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private Task ExportProjectList(string path)
        {
            try
            {
                var data = Data;
                if (data != null)
                {
                    var content = JsonSerializer.Serialize(data.AllProjects, _jsonOptions);
                    return File.WriteAllTextAsync(path, content);
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }

            return Task.CompletedTask;
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
            File.WriteAllText(Path.Combine(Settings.Default.WorkdayPath, $"{DateTimeOffset.Now.ToFileTime()}_ErrorLog.txt"), msg);

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

        public string WorkdayPath => GetWorkdayPath(DateTimeOffset.Now);

        public string GetWorkdayPath(DateTimeOffset date) => Path.Combine(GetWorkdayDirectory(date), $"{date:yyyyMMdd}.workday.json");

        public string GetWorkdayDirectory(DateTimeOffset? date = null)
        {
            date ??= DateTimeOffset.Now;

            var dir = Settings.Default.WorkdayPath;
            if (string.IsNullOrWhiteSpace(dir))
            {
                dir = ".";
            }

            dir = Path.Combine(dir, $"{date:yyyy}");
            Directory.CreateDirectory(dir);

            return dir;
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
            var path = WorkdayPath;
            var workday = LoadWorkday(path, true);

            if (workday != null)
            {
                return workday;
            }

            var loginDate = DateTimeOffset.Now.AddMilliseconds(-Environment.TickCount);
            // Use time from login if from today
            loginDate = (loginDate.Date == DateTimeOffset.Now.Date) ? loginDate : DateTimeOffset.Now;

            var directory = GetWorkdayDirectory();
            var orderedFiles = new DirectoryInfo(directory)
                .EnumerateFiles("*.workday.json", new EnumerationOptions { IgnoreInaccessible = true })
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
                    var workday = JsonSerializer.Deserialize<Workday?>(content, _jsonOptions);

                    try
                    {
                        var backupDir = Path.Combine(GetWorkdayDirectory(), "backup", DateTimeOffset.Now.DayOfWeek.ToString());
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
                    var content = JsonSerializer.Serialize(data, _jsonOptions);
                    return File.WriteAllTextAsync(WorkdayPath, content);
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

        private void ImportProjectsAtStartup_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.ImportProjectsAtStartup = !Settings.Default.ImportProjectsAtStartup;
            Settings.Default.Save();
            ImportProjectsAtStartup.IsChecked = Settings.Default.ImportProjectsAtStartup;
        }

        private void ImportProjectListClick(object sender, RoutedEventArgs e)
        {
            var data = Data;
            if (data == null)
            {
                MessageBox.Show("No workday loaded!");
                return;
            }

            var openFileDialog = CreateDialog<OpenFileDialog>(Settings.Default.ProjectListPath, "projects");
            if (openFileDialog.ShowDialog() == true)
            {
                ImportProjectList(openFileDialog.FileName, data);
            }
        }

        private void ExportProjectListClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = CreateDialog<SaveFileDialog>(Settings.Default.ProjectListPath, "projects");
            if (openFileDialog.ShowDialog() == true)
            {
                ExportProjectList(openFileDialog.FileName);
            }
        }

        private static T CreateDialog<T>(string path, string ext)
            where T: FileDialog, new()
        {
            var dialog = new T
            {
                Filter = $"Project list (*.{ext}.json)|*.{ext}.json|All files (*.*)|*.*",
                DefaultExt = "projects.json",
                FileName = Path.GetFileName(path),
                InitialDirectory = Path.GetDirectoryName(path)
            };
            return dialog;
        }

        private void SetStorageLocationClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = CreateDialog<OpenFileDialog>(Settings.Default.WorkdayPath, "workday");
            openFileDialog.DefaultExt = "";
            openFileDialog.CheckFileExists = false;
            if (openFileDialog.ShowDialog() == true)
            {
                Settings.Default.WorkdayPath = Path.GetDirectoryName(openFileDialog.FileName);
                Settings.Default.Save();
                WorkdayPathMenuItem.Header = Settings.Default.WorkdayPath;
            }
        }
    }
}
