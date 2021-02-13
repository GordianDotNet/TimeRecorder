using System;
using System.Text.Json.Serialization;

namespace TimeRecorder.Models
{
    public class WorkdayTask : ViewModelBase
    {
        private Project _Project = new Project { ProjectName = "NotSet" };
        public Project Project
        {
            get => _Project;
            set => SetField(ref _Project, value);
        }

        private string _Description = string.Empty;
        public string Description
        {
            get => _Description;
            set => SetField(ref _Description, value);
        }

        private DateTimeOffset _Created = DateTimeOffset.Now;
        public DateTimeOffset Created
        {
            get => _Created;
            set => SetField(ref _Created, value);
        }

        private DateTimeOffset _LastUsed = DateTimeOffset.Now;
        public DateTimeOffset LastUsed
        {
            get => _LastUsed;
            set => SetField(ref _LastUsed, value);
        }

        private TimeSpan _Correction;
        public TimeSpan Correction
        {
            get => _Correction;
            set { SetField(ref _Correction, value); OnPropertyChanged(nameof(TotalDuration)); }
        }

        private TimeSpan _Duration;
        public TimeSpan Duration
        {
            get => _Duration;
            set { SetField(ref _Duration, value); OnPropertyChanged(nameof(TotalDuration)); }
        }

        [JsonIgnore]
        public TimeSpan TotalDuration
        {
            get => Duration + Correction;
        }

        private bool _IsActive;
        [JsonIgnore]
        public bool IsActive
        {
            get => _IsActive;
            set => SetField(ref _IsActive, value);
        }

        public void UpdateDuration(TimeSpan timeSpan)
        {
            var now = DateTimeOffset.Now;
            if (now.Second == 0)
            {
                // Update LastUsed every minute and if second between 0 - 2 too avoid unwanted updates
                LastUsed = now;
            }
            Duration += timeSpan;
        }
    }
}
