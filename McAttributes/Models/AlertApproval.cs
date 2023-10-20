using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models {
    [Table("alertapproval")]
    public class AlertApproval : AlertApprovalBase {

        [Column("userid")]
        [ForeignKey(nameof(Principal))]
        public new long UserId { get; set; }
        public virtual User Principal { get; set; }

        [Column("alertid")]
        [ForeignKey(nameof(Alert))]
        public new long AlertId { get; set; }
        public virtual AlertLogEntry Alert { get; set; }

        [Column("status")]
        public new string? Status { get; set; }
    }

    public class AlertApprovalBase : RowVersionedModel {
        public long UserId { get; set; }

        public long AlertId { get; set; }

        public string? Status { get; set; }

        public AlertApproval ToAlertApproval() {
            return new AlertApproval {
                UserId = this.UserId,
                AlertId = this.AlertId,
                Status = this.Status,
            };
        }
    }
}
