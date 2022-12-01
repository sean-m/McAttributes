using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace McAttributes.Models {
    [Table("azusers")]
    [Index(nameof(AadId))]
    public class User {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("lastfetched")]
        public DateTime? LastFetched { get; set; } = null;
        [Column("merged")]
        public DateTime? Merged { set; get; } = null;
        [Column("modified")]
        public DateTime? Modified { get; set; } = null;
        
        [Column("created")]
        public DateTime? Created { get; set; } = null;
        [Column("enabled")]
        public bool Enabled { get; set; }
        [Column("tenant")]
        public string? Tenant { get; set; }
        [Column("aadid")]
        public Guid AadId { get; set; }
        [Column("upn")]
        public string? Upn { get; set; }
        [Column("mail")]
        public string? Mail { get; set; }
        [Column("displayname")]
        public string? DisplayName { get; set; }
        [Column("employeeid")]
        public string? EmployeeId { get; set; }
        [Column("ademployeeid")]
        public string? AdEmployeeId { get; set; }
        [Column("hremployeeid")]
        public string? HrEmployeeId { get; set; }
        [Column("wid")]
        public string? Wid { get; set; }
        [Column("creationtype")]
        public string? CreationType { get; set; }
        [Column("company")]
        public string? Company { get; set; }
        [Column("preferredgivenname")]
        public string? PreferredGivenName { get; set; }
        [Column("preferredsurname")]
        public string? PreferredSurname { get; set; }
        [Column("articles")]
        public string? Articles { get; set; }
        [Column("pronouns")]
        public string? Pronouns { get; set; }
    }
}
