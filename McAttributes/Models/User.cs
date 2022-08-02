using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models {
    [Table("User")]
    public class User {
        public int Id { get; set; }
        public Guid AadId { get; set; }

        private bool _deleted = false;
        public bool Deleted { get => _deleted; set => _deleted = value; }
        public string? DisplayName { get; set; }
        public string? EmployeeId { get; set; }
        public string? GivenName { get; set; }
        public string? Surname { get; set; }
        public string? Pronouns { get; set; }
        public string? Suffix { get; set; }
        
    }
}
