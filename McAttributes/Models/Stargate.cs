using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace McAttributes.Models {
    [Table("stargate")]
    [Index(nameof(LocalId), nameof(Partition))]
    public class Stargate : RowVersionedModel {

        [Column("recordtype", TypeName="integer")]
        public StargateType RecordType { get; set; }

        [ForeignKey("id")]
        [Column("globalid")]
        public long? GlobalId { get; set; }

        [Column("localid")]
        [StringLength(256)]
        public string? LocalId { get; set; }

        [Column("upn")]
        [StringLength(256)]
        public string? Upn  { get; set; }

        [Column("partition")]
        [StringLength(256)]
        public string? Partition { get; set; }

        [Column("joinseed")]
        [StringLength(256)]
        public string? Joinseed { get; set; }
    }


    public enum StargateType {
        prime = 0,
        native = 10,
        guest = 20,
        privileged = 30,
        userGroup = 100
    }
}
