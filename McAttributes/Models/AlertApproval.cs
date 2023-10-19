using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models {
    [Table("alertapproval")]
    public class AlertApproval : RowVersionedModel {
        [ForeignKey(nameof(User.Id))]
        public long UserId { get; set; }

        public long AlertId { get; set; }

        public string? Status { get; set; }
    }
}
