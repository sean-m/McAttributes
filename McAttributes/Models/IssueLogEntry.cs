using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models
{
    [Table("idalerts")]
    public class IssueLogEntry
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("attrname")]
        public string? AttrName { get; set; }
        [Column("alerthash")]
        public string? AlertHash { get; set; }
        [Column("created")]
        public DateTime? Created { get; set; }
        [Column("status")]
        public string? Status { get; set; }
        [Column("description")]
        public string? Description { get; set; }
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
