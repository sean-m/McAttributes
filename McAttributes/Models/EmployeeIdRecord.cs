using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models
{
    [Table("EmployeeIdRecord")]
    public class EmployeeIdRecord
    {
        [Key]
        public int Id { get; set; }
        public string CloudSourceAnchor { get; set; }
        public string UserPrincipalName { get; set; }
        public string? EmployeeId { get; set; }
        public string? AdEmployeeId { get; set; }
    }
}
