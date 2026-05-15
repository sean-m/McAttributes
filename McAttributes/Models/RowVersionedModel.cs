using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models {
    public class RowVersionedModel {
        [Key]
        [Column("id")]
        public long Id { get; set; }

    }
}
