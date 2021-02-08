using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace TimeRecorder.Models
{
    public class Workday : ViewModelBase
    {
        public Workday()
        {
            CurrentProject = AllProjects.FirstOrDefault();
            WorkEnd = WorkBegin + PlannedWorkingHours;
        }

        private readonly TimeSpan TICK_DURATION = TimeSpan.FromSeconds(1);

        public void StartTimer()
        {
            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimerTick);
            dispatcherTimer.Interval = TICK_DURATION;
            dispatcherTimer.Start();
        }

        private void DispatcherTimerTick(object? sender, EventArgs e)
        {
            CurrentWorkdayTask?.UpdateDuration(TICK_DURATION);

            var time = PlannedWorkingHours - (DateTimeOffset.Now - WorkBegin);
            RequiredWorkingHours = new TimeSpan(time.Hours, time.Minutes, time.Seconds);

            var timeDone = DateTimeOffset.Now - WorkBegin;
            DoneWorkingHours = new TimeSpan(timeDone.Hours, timeDone.Minutes, timeDone.Seconds);
        }

        private TimeSpan _PlannedWorkingHours = new TimeSpan(8, 45, 0);
        public TimeSpan PlannedWorkingHours
        {
            get => _PlannedWorkingHours;
            set => SetField(ref _PlannedWorkingHours, value);
        }

        private DateTime _WorkBegin = DateTime.Now;
        public DateTime WorkBegin
        {
            get => _WorkBegin;
            set => SetField(ref _WorkBegin, value);
        }

        private DateTime? _WorkEnd;
        public DateTime? WorkEnd
        {
            get => _WorkEnd;
            set => SetField(ref _WorkEnd, value);
        }

        private TimeSpan _RequiredWorkingHours;
        public TimeSpan RequiredWorkingHours
        {
            get => _RequiredWorkingHours;
            set => SetField(ref _RequiredWorkingHours, value);
        }

        private TimeSpan _DoneWorkingHours;
        public TimeSpan DoneWorkingHours
        {
            get => _DoneWorkingHours;
            set => SetField(ref _DoneWorkingHours, value);
        }

        private ObservableCollection<WorkdayTask> _WorkdayTasks = new ObservableCollection<WorkdayTask>();
        public ObservableCollection<WorkdayTask> WorkdayTasks
        {
            get => _WorkdayTasks;
            set => SetField(ref _WorkdayTasks, value);
        }

        private WorkdayTask? _CurrentWorkdayTask;
        [JsonIgnore]
        public WorkdayTask? CurrentWorkdayTask
        {
            get => _CurrentWorkdayTask;
            set => SetField(ref _CurrentWorkdayTask, value);
        }

        private ObservableCollection<Project> _AllProjects = new ObservableCollection<Project>(new List<Project> { new Project { ProjectName = "Unknown" } });
        public ObservableCollection<Project> AllProjects
        {
            get => _AllProjects;
            set => SetField(ref _AllProjects, value);
        }

        private Project? _CurrentProject;
        [JsonIgnore]
        public Project? CurrentProject
        {
            get => _CurrentProject;
            set => SetField(ref _CurrentProject, value);
        }

        private string _ProjectNameFilter = string.Empty;
        public string ProjectNameFilter
        {
            get => _ProjectNameFilter;
            set { SetField(ref _ProjectNameFilter, value); UpdateProjectList(); }
        }

        public List<Project> FilteredProjects
        {
            get
            {
                var filter = ProjectNameFilter;
                if (string.IsNullOrWhiteSpace(filter))
                {
                    return AllProjects.OrderByDescending(x => x.LastUsed).ToList();
                }
                else
                {
                    var pattern = "(.*" + filter.Replace(" ", ".*)|(.*") + ".*)";
                    pattern = pattern.Replace("|(.*.*)", "");
                    return AllProjects.Where(x => Regex.IsMatch(x.ProjectName, pattern, RegexOptions.IgnoreCase)).OrderByDescending(x => x.LastUsed).ToList();
                }
            }
        }

        private void UpdateProjectList()
        {
            OnPropertyChanged(nameof(FilteredProjects));
        }

        #region Commands Project

        private ICommand? _ResetProjectFilterCommand;
        [JsonIgnore]
        public ICommand ResetProjectFilterCommand { get { return _ResetProjectFilterCommand ??= new CommandHandler(ResetProjectFilter); } }
        private void ResetProjectFilter() => ProjectNameFilter = string.Empty;

        private ICommand? _AddProjectCommand;
        [JsonIgnore]
        public ICommand AddProjectCommand { get { return _AddProjectCommand ??= new CommandHandler(AddProject); } }
        private void AddProject()
        {
            ResetProjectFilter();
            var project = new Project { ProjectName = "NewProjectName", LastUsed = DateTimeOffset.Now };
            AllProjects.Add(project);
            CurrentProject = project;
            UpdateProjectList();
        }

        private ICommand? _RemoveProjectCommand;
        [JsonIgnore]
        public ICommand RemoveProjectCommand { get { return _RemoveProjectCommand ??= new CommandHandler(RemoveProject, () => CurrentProject != null); } }
        private void RemoveProject()
        {
            var project = CurrentProject;
            if (project != null)
            {
                AllProjects.Remove(project);
            }
            CurrentProject = AllProjects.FirstOrDefault();
            UpdateProjectList();
        }

        #endregion

        #region Commands WorkdayTask

        private ICommand? _RemoveWorkdayTaskCommand;
        [JsonIgnore]
        public ICommand RemoveWorkdayTaskCommand { get { return _RemoveWorkdayTaskCommand ??= new CommandHandler(RemoveWorkdayTask, () => CurrentWorkdayTask != null); } }
        private void RemoveWorkdayTask()
        {
            var workdayTask = CurrentWorkdayTask;
            if (workdayTask != null)
            {
                WorkdayTasks.Remove(workdayTask);
            }
            CurrentWorkdayTask = WorkdayTasks.LastOrDefault();
        }

        private ICommand? _AddWorkdayTaskCommand;
        [JsonIgnore]
        public ICommand AddWorkdayTaskCommand { get { return _AddWorkdayTaskCommand ??= new CommandHandler(AddWorkdayTask); } }
        private void AddWorkdayTask()
        {
            var project = CurrentProject;
            if (project == null)
                return;
            project.LastUsed = DateTimeOffset.Now;
            var workdayTask = new WorkdayTask { Project = project };
            WorkdayTasks.Add(workdayTask);
            CurrentWorkdayTask = workdayTask;
            UpdateProjectList();
        }

        private ICommand? _AddWorkdayTaskDurationCommand;
        [JsonIgnore]
        public ICommand AddWorkdayTaskDurationCommand { get { return _AddWorkdayTaskDurationCommand ??= new CommandHandler<string>(AddWorkdayTaskDuration); } }
        private void AddWorkdayTaskDuration(string strParam)
        {
            var workdayTask = CurrentWorkdayTask;
            if (workdayTask != null && int.TryParse(strParam, out var duration))
            {
                workdayTask.AddCorrection(TimeSpan.FromMinutes(duration));
            }
        }

        #endregion

        #region Add default project (Pause, Absent, ...)

        private ICommand? _SetPauseCommand;
        [JsonIgnore]
        public ICommand SetPauseCommand { get { return _SetPauseCommand ??= new CommandHandler(SetPause); } }
        private void SetPause()
        {
            AddDefaultProject("Pause");
        }

        private ICommand? _SetAbsentCommand;
        [JsonIgnore]
        public ICommand SetAbsentCommand { get { return _SetAbsentCommand ??= new CommandHandler(SetAbsent); } }
        private void SetAbsent()
        {
            AddDefaultProject("Absent");
        }

        private void AddDefaultProject(string projectName)
        {
            var project = AllProjects.Where(x => x.ProjectName == projectName).FirstOrDefault();
            if (project == null)
            {
                project = new Project { ProjectName = projectName };
                AllProjects.Add(project);
            }
            CurrentProject = project;

            AddWorkdayTask();
        }

        #endregion
    }
}
