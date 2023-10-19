using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models {
    
    [Table("cohorts")]
    public class CohortDescription : RowVersionedModel {
        public ResolutionStatus Status { get; set; } = ResolutionStatus.pending;
        public bool AssociateCohorts { get; set; } = false;
        public IEnumerable<CohortDescriptionMember> CohortMembers { get; set; } = Enumerable.Empty<CohortDescriptionMember>();
        public string? Description { get; set; }
        public CohortValidation Validate() {

            if (CohortMembers == null || 0 == CohortMembers.Count()) {
                return new CohortValidation { Message = "No cohort members defined." };
            }

            foreach (var member in CohortMembers) {
                if (String.IsNullOrEmpty(member.Id)) {
                    return new CohortValidation { Message = $"Cohort member invalid. Missing member id." };
                }
                if (member.CohortBucket < 0) {
                    return new CohortValidation { Message = $"Cohort member {member.Id} invalid. CohortBucket on the member is initialized to -1 and should be set to 0 or greater when processing. Note: 0 is the null cohort and will not be associated with other members." };
                }
            }

            var buckets = CohortMembers.Select(m => m.CohortBucket).Distinct().ToList();
            if (AssociateCohorts && buckets.Count < 2) {
                return new CohortValidation { Succeeded = true, Message = "Validation succeeded but AssociateCohorts was set to true while only a single cohort bucket is specified." };
            }

            return new CohortValidation { Succeeded = true };
        }
    }

    public enum ResolutionStatus {
        pending,
        processing,
        failed,
        completed,
    }

    [Table("cohortmembers")]
    public class CohortDescriptionMember : RowVersionedModel {
        [Key]
        public string? Id { get; set; }

        [ForeignKey(nameof(CohortDescription.Id))]
        public long CohortBucket { get; set; }
    }

    public class CohortValidation {
        internal bool Succeeded { get; set; } = false;
        internal string Message { get; set; } = String.Empty;
    }
}
