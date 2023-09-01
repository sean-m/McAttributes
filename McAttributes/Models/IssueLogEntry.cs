using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models
{
    [Table("idalerts")]
    public class IssueLogEntry
    {
        [Key]
        [Column("id")]
        public uint Id { get; set; }
        [Column("attrname")]
        public string? AttrName { get; set; }
        [Column("created")]
        public DateTime? Created { get; set; }
        [Column("lastSeen")]
        public DateTime? LastSeen { get; set; }
        [Column("alerthash")]
        public string? AlertHash { get; set; }
        [Column("status")]
        public string? Status { get; set; }
        [Column("description")]
        public string? Description { get; set; }
        [Column("notes")]
        public string? Notes { get; set; }


        // This is the conncurrency ID when using PostgresSQL or other databases
        // that don't support concurrency same as the SQL Server client.
        [Timestamp]
        public uint Version { get; set; }
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
