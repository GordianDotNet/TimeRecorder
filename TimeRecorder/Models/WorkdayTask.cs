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

        public void UpdateDuration(TimeSpan timeSpan)
        {
            Duration += timeSpan;
        }

        internal void AddCorrection(TimeSpan timeSpan)
        {
            Correction += timeSpan;
        }
    }
}
