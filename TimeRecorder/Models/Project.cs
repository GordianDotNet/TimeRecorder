using System;

namespace TimeRecorder.Models
{
    public class Project : ViewModelBase
    {
        private string _ProjectName = string.Empty;
        public string ProjectName
        {
            get => _ProjectName;
            set => SetField(ref _ProjectName, value);
        }

        private DateTimeOffset _LastUsed = DateTimeOffset.Now;
        public DateTimeOffset LastUsed
        {
            get => _LastUsed;
            set => SetField(ref _LastUsed, value);
        }
    }
}
