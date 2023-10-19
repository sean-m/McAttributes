using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models
{
    [Table("employeeidrecord")]
    public class EmployeeIdRecord : RowVersionedModel
    {
        public string CloudSourceAnchor { get; set; }
        public string UserPrincipalName { get; set; }
        public string? EmployeeId { get; set; }
        public string? AdEmployeeId { get; set; }
    }
}
