using System;
using System.Text;

namespace CustomVisionImport.Model
{
    public class TrainReturn
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public string ProjectId { get; set; }
        public bool Exportable { get; set; }
        public object ExportableTo { get; set; }
        public object DomainId { get; set; }
        public object ClassificationType { get; set; }
        public string TrainingType { get; set; }
        public int ReservedBudgetInHours { get; set; }
        public object PublishName { get; set; }
        public object OriginalPublishResourceId { get; set; }
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Id);
            stringBuilder.AppendLine(Name);
            stringBuilder.AppendLine(Status);
            stringBuilder.AppendLine(Created.ToLongDateString());
            stringBuilder.AppendLine(LastModified.ToLongDateString());
            stringBuilder.AppendLine(ProjectId);
            return stringBuilder.ToString();
        }
    }
}
