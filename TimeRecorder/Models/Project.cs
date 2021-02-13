using System;
using System.Text.Json.Serialization;

namespace TimeRecorder.Models
{
    public class Project : ViewModelBase
    {
        private Guid _ProjectId = Guid.NewGuid();
        public Guid ProjectId
        {
            get => _ProjectId;
            set => SetField(ref _ProjectId, value);
        }

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

        public readonly static Guid PauseId = new Guid("10000000-0000-0000-0000-000000000000");
        public readonly static Guid NotAtWorkId = new Guid("20000000-0000-0000-0000-000000000000");
    }
}
