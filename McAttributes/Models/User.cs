using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models {
    [Table("azusers")]
    public class User {
        [Key]
        public int Id { get; set; }
        public string? Merged { set; get; } = null;
        public string? Modified { get; set; } = null;
        public string? LastFetched { get; set; } = null;
        public string? Created { get; set; } = null;
        public bool Enabled { get; set; }
        public string? Tenant { get; set; }
        public Guid AadId { get; set; }
        public string? Upn { get; set; }
        public string? Mail { get; set; }
        public string? DisplayName { get; set; }
        public string? EmployeeId { get; set; }
        public string? AdEmployeeId { get; set; }
        public string? HrEmployeeId { get; set; }
        public string? Wid { get; set; }
        public string? CreationType { get; set; }
        public string? Company { get; set; }
        public string? PreferredGivenName { get; set; }
        public string? PreferredSurname { get; set; }
        public string? Articles { get; set; }
        public string? Pronouns { get; set; }
    }
}
