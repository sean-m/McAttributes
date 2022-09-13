using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models
{
    public class IssueLogEntry
    {
        [Key]
        public int Id { get; set; }
        public DateTime Created { get; set; }
        public User? TargetUser { get; set; }
        public LogLevel Level { get; set; }
        public int EntryId { get; set; }
        public string? Message { get; set; }
        public bool Acknowledged { get; set; } = false;
        public bool Resolved { get; set;} = false;
    }

    public enum LogLevel
    {
        debug=0,
        info,
        warning,
        error,
        critical
    }
}
