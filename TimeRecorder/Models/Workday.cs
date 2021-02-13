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
            CurrentProject = LastUsedProject;            
        }

        private readonly TimeSpan TICK_DURATION = TimeSpan.FromSeconds(1);

        public void StartTimer()
        {
            WorkdayTasks = new ObservableCollection<WorkdayTask>(WorkdayTasks.OrderBy(x => x.Created).ToList());

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimerTick);
            dispatcherTimer.Interval = TICK_DURATION;
            dispatcherTimer.Start();
        }

        public void SetActiveWorkingTask(WorkdayTask newActiveWorkTask)
        {
            var activeWorkingTask = ActiveWorkingTask;
            if (activeWorkingTask != null)
            {
                activeWorkingTask.IsActive = false;
            }
            if (newActiveWorkTask != null)
            {
                newActiveWorkTask.IsActive = true;
                ActiveWorkingTask = newActiveWorkTask;
            }
        }

        private void DispatcherTimerTick(object? sender, EventArgs e)
        {
            var now = DateTimeOffset.Now;
            ActiveWorkingTask?.UpdateDuration(TICK_DURATION);

            var time = PlannedWorkingHours - (now - WorkBegin);
            RequiredWorkingHours = new TimeSpan(time.Hours, time.Minutes, time.Seconds);

            var timeDone = now - WorkBegin;
            DoneWorkingHours = new TimeSpan(timeDone.Hours, timeDone.Minutes, timeDone.Seconds);

            var workingdayTasks = WorkdayTasks;
            if (workingdayTasks.Count > 0)
            {
                BookedWorkingHours = TimeSpan.FromMilliseconds(workingdayTasks.Sum(x => (long)x.TotalDuration.TotalMilliseconds));
            }
            else
            {
                BookedWorkingHours = TimeSpan.Zero;
            }

            WorkEnd = now;
        }

        private TimeSpan _PlannedWorkingHours = new TimeSpan(8, 45, 0);
        public TimeSpan PlannedWorkingHours
        {
            get => _PlannedWorkingHours;
            set { SetField(ref _PlannedWorkingHours, value); OnPropertyChanged(nameof(PlannedWorkEnd)); }
}

        private TimeSpan _BookedWorkingHours = TimeSpan.Zero;

        internal void UpdateProjectReferences()
        {
            var workingDayTasks = WorkdayTasks;
            foreach (var workindDayTask in WorkdayTasks)
            {
                workindDayTask.Project = AllProjects.Where(x => x.ProjectId == workindDayTask.Project.ProjectId).FirstOrDefault() ?? workindDayTask.Project;
            }
        }

        [JsonIgnore]
        public TimeSpan BookedWorkingHours
        {
            get => _BookedWorkingHours;
            set { SetField(ref _BookedWorkingHours, value); OnPropertyChanged(nameof(NotBookedWorkingHours)); }
        }

        [JsonIgnore]
        public TimeSpan NotBookedWorkingHours
        {
            get => DoneWorkingHours - BookedWorkingHours;
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

        private DateTimeOffset _WorkBegin = DateTime.Now;
        public DateTimeOffset WorkBegin
        {
            get => _WorkBegin;
            set { SetField(ref _WorkBegin, value); OnPropertyChanged(nameof(PlannedWorkEnd)); }
        }

        [JsonIgnore]
        public DateTimeOffset? PlannedWorkEnd
        {
            get => WorkBegin + PlannedWorkingHours;
        }

        private DateTimeOffset? _WorkEnd;

        public DateTimeOffset? WorkEnd
        {
            get => _WorkEnd;
            set => SetField(ref _WorkEnd, value);
        }

        private ObservableCollection<WorkdayTask> _WorkdayTasks = new ObservableCollection<WorkdayTask>();
        public ObservableCollection<WorkdayTask> WorkdayTasks
        {
            get => _WorkdayTasks;
            set => SetField(ref _WorkdayTasks, value);
        }

        public WorkdayTask LastUsedWorkdayTask
        {
            get => WorkdayTasks.OrderByDescending(x => x.LastUsed).FirstOrDefault();
        }

        private WorkdayTask? _CurrentWorkdayTask;
        [JsonIgnore]
        public WorkdayTask? CurrentWorkdayTask
        {
            get => _CurrentWorkdayTask;
            set => SetField(ref _CurrentWorkdayTask, value);
        }

        private WorkdayTask? _ActiveWorkingTask;
        [JsonIgnore]
        public WorkdayTask? ActiveWorkingTask
        {
            get => _ActiveWorkingTask;
            set => SetField(ref _ActiveWorkingTask, value);
        }

        private ObservableCollection<Project> _AllProjects = new ObservableCollection<Project>(new List<Project> { new Project { ProjectName = "Unknown" } });
        public ObservableCollection<Project> AllProjects
        {
            get => _AllProjects;
            set => SetField(ref _AllProjects, value);
        }

        public Project LastUsedProject
        {
            get => AllProjects.OrderByDescending(x => x.LastUsed).FirstOrDefault();
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
            if (ActiveWorkingTask == workdayTask)
            {
                SetActiveWorkingTask(LastUsedWorkdayTask);
            }
            CurrentWorkdayTask = LastUsedWorkdayTask;
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
            SetActiveWorkingTask(workdayTask);
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
            AddDefaultProject(Project.PauseId, "Pause");
        }

        private ICommand? _SetAbsentCommand;
        [JsonIgnore]
        public ICommand SetAbsentCommand { get { return _SetAbsentCommand ??= new CommandHandler(SetAbsent); } }
        private void SetAbsent()
        {
            AddDefaultProject(Project.NotAtWorkId, "Pause - Not at work (Private matter)");
        }

        private void AddDefaultProject(Guid projectId, string projectName)
        {
            var project = AllProjects.Where(x => x.ProjectId == projectId).FirstOrDefault();
            if (project == null)
            {
                project = new Project { ProjectId = projectId, ProjectName = projectName };
                AllProjects.Add(project);
            }
            CurrentProject = project;

            AddWorkdayTask();
        }

        #endregion
    }
}
