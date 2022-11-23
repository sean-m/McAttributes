using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models {
    [Table("stargate")]
    public class Stargate {
        [Key]
        [Column("id")]
        public long Id { get; set; }
        [Column("recordtype")]
        public StargateType RecordType { get; set; }
        [ForeignKey("id")]
        [Column("globalid")]
        public long? GlobalId { get; set; }
        [Column("localid")]
        public string? LocalId { get; set; }
        [Column("upn")]
        public string? Upn  { get; set; }
        [Column("partition")]
        public string? Partition { get; set; }
        [Column("joinseed")]
        public string? Joinseed { get; set; }
    }


    public enum StargateType {
        prime = 0,
        natvie = 1,
        guest = 2,
        privileged = 3,
        userGroup = 10
    }
}
