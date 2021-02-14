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
            WorkEnd = now;

            ActiveWorkingTask?.UpdateDuration(TICK_DURATION);

            var timeDone = (WorkEnd - WorkBegin) - BookedPauseTime;
            DoneWorkingHours = new TimeSpan(timeDone.Hours, timeDone.Minutes, timeDone.Seconds);

            RequiredWorkingHours = PlannedWorkingHours - DoneWorkingHours;

            var workingdayTasks = WorkdayTasks;
            if (workingdayTasks.Count > 0)
            {
                BookedWorkingHours = TimeSpan.FromMilliseconds(workingdayTasks
                    .Where(x => x.Project.ProjectId != Project.PauseId && x.Project.ProjectId != Project.NotAtWorkId)
                    .Sum(x => (long)x.TotalDuration.TotalMilliseconds));
                BookedPauseTime = TimeSpan.FromMilliseconds(workingdayTasks
                    .Where(x => x.Project.ProjectId == Project.PauseId || x.Project.ProjectId == Project.NotAtWorkId)
                    .Sum(x => (long)x.TotalDuration.TotalMilliseconds));
            }
            else
            {
                BookedWorkingHours = TimeSpan.Zero;
                BookedPauseTime = TimeSpan.Zero;
            }
        }

        internal void UpdateProjectReferences()
        {
            var workingDayTasks = WorkdayTasks;
            foreach (var workindDayTask in WorkdayTasks)
            {
                workindDayTask.Project = AllProjects.Where(x => x.ProjectId == workindDayTask.Project.ProjectId).FirstOrDefault() ?? workindDayTask.Project;
            }
        }

        private TimeSpan _PlannedWorkingHours = new TimeSpan(8, 0, 0);
        public TimeSpan PlannedWorkingHours
        {
            get => _PlannedWorkingHours;
            set { SetField(ref _PlannedWorkingHours, value); OnPropertyChanged(nameof(PlannedWorkEnd)); }
        }

        private TimeSpan _PlannedPauseTime = new TimeSpan(0, 45, 0);
        public TimeSpan PlannedPauseTime
        {
            get => _PlannedPauseTime;
            set { SetField(ref _PlannedPauseTime, value); OnPropertyChanged(nameof(PlannedWorkEnd)); OnPropertyChanged(nameof(NotBookedPauseTime)); }
        }

        private TimeSpan _BookedWorkingHours = TimeSpan.Zero;
        public TimeSpan BookedWorkingHours
        {
            get => _BookedWorkingHours;
            set { SetField(ref _BookedWorkingHours, value); OnPropertyChanged(nameof(NotBookedWorkingHours)); }
        }

        private TimeSpan _BookedPauseTime = TimeSpan.Zero;
        public TimeSpan BookedPauseTime
        {
            get => _BookedPauseTime;
            set { SetField(ref _BookedPauseTime, value); OnPropertyChanged(nameof(NotBookedPauseTime)); OnPropertyChanged(nameof(PlannedWorkEnd)); }
        }

        public TimeSpan NotBookedWorkingHours
        {
            get => DoneWorkingHours - BookedWorkingHours;
        }

        public TimeSpan NotBookedPauseTime
        {
            get => PlannedPauseTime - BookedPauseTime;
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

        private DateTimeOffset _WorkBegin = DateTimeOffset.Now;
        public DateTimeOffset WorkBegin
        {
            get => _WorkBegin;
            set { SetField(ref _WorkBegin, value); OnPropertyChanged(nameof(PlannedWorkEnd)); }
        }

        [JsonIgnore]
        public DateTimeOffset? PlannedWorkEnd
        {
            get => WorkBegin + PlannedWorkingHours + (PlannedPauseTime > BookedPauseTime ? PlannedPauseTime : BookedPauseTime);
        }

        private DateTimeOffset _WorkEnd = DateTimeOffset.Now;

        public DateTimeOffset WorkEnd
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

        private ObservableCollection<Project> _AllProjects = new ObservableCollection<Project>(new List<Project> { });
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
        [JsonIgnore]
        public string ProjectNameFilter
        {
            get => _ProjectNameFilter;
            set { SetField(ref _ProjectNameFilter, value); UpdateProjectList(); }
        }

        [JsonIgnore]
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

        #region Commands config

        private bool _ChangeStartOfWork = true;
        [JsonIgnore]
        public bool ChangeStartOfWork
        {
            get => _ChangeStartOfWork;
            set => SetField(ref _ChangeStartOfWork, value);
        }

        private bool _ChangeEndOfWork;
        [JsonIgnore]
        public bool ChangeEndOfWork
        {
            get => _ChangeEndOfWork;
            set => SetField(ref _ChangeEndOfWork, value);
        }

        private bool _ChangePlannedWorkHours;
        [JsonIgnore]
        public bool ChangePlannedWorkHours
        {
            get => _ChangePlannedWorkHours;
            set => SetField(ref _ChangePlannedWorkHours, value);
        }

        private bool _ChangePlannedPauseTime;
        [JsonIgnore]
        public bool ChangePlannedPauseTime
        {
            get => _ChangePlannedPauseTime;
            set => SetField(ref _ChangePlannedPauseTime, value);
        }

        private ICommand? _AddConfigTimeCommand;
        [JsonIgnore]
        public ICommand AddConfigTimeCommand { get { return _AddConfigTimeCommand ??= new CommandHandler<string>(AddConfigTime); } }
        private void AddConfigTime(string strParam)
        {
            if (int.TryParse(strParam, out var duration))
            {
                if (ChangeStartOfWork)
                    WorkBegin += TimeSpan.FromMinutes(duration);
                else if (ChangeEndOfWork)
                    WorkEnd += TimeSpan.FromMinutes(duration);
                else if (ChangePlannedWorkHours)
                    PlannedWorkingHours += TimeSpan.FromMinutes(duration);
                else if (ChangePlannedPauseTime)
                    PlannedPauseTime += TimeSpan.FromMinutes(duration);
            }
        }

        #endregion

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

        private bool _ChangeWorkdayCorrection = true;
        [JsonIgnore]
        public bool ChangeWorkdayCorrection
        {
            get => _ChangeWorkdayCorrection;
            set => SetField(ref _ChangeWorkdayCorrection, value);
        }

        private bool _ChangeWorkdayCreated;
        [JsonIgnore]
        public bool ChangeWorkdayCreated
        {
            get => _ChangeWorkdayCreated;
            set => SetField(ref _ChangeWorkdayCreated, value);
        }

        private bool _ChangeWorkdayLastUsed;
        [JsonIgnore]
        public bool ChangeWorkdayLastUsed
        {
            get => _ChangeWorkdayLastUsed;
            set => SetField(ref _ChangeWorkdayLastUsed, value);
        }

        private ICommand? _AddSelectedWorkdayTaskTimeCommand;
        [JsonIgnore]
        public ICommand AddSelectedWorkdayTaskTimeCommand { get { return _AddSelectedWorkdayTaskTimeCommand ??= new CommandHandler<string>(AddSelectedWorkdayTaskTime); } }
        private void AddSelectedWorkdayTaskTime(string strParam)
        {
            var workdayTask = CurrentWorkdayTask;
            if (workdayTask != null && int.TryParse(strParam, out var duration))
            {
                if (ChangeWorkdayCorrection)
                    workdayTask.Correction += TimeSpan.FromMinutes(duration);
                else if (ChangeWorkdayCreated)
                    workdayTask.Created += TimeSpan.FromMinutes(duration);
                else if (ChangeWorkdayLastUsed)
                    workdayTask.LastUsed += TimeSpan.FromMinutes(duration);
            }
        }

        #endregion

        #region Add default project (Pause, Absent, ...)

        private ICommand? _AddPauseCommand;
        [JsonIgnore]
        public ICommand AddPauseCommand { get { return _AddPauseCommand ??= new CommandHandler(AddPause); } }
        private void AddPause()
        {
            AddDefaultProject(Project.PauseId, "Pause");
        }

        private ICommand? _AddNotAtWorkCommand;
        [JsonIgnore]
        public ICommand AddNotAtWorkCommand { get { return _AddNotAtWorkCommand ??= new CommandHandler(AddNotAtWork); } }
        private void AddNotAtWork()
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
