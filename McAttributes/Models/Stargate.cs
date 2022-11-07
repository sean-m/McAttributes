using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models {
    [Table("stargate")]
    public class Stargate {
        [Key]
        public long id { get; set; }
        [Column("recordtype")]
        public StargateType recordType { get; set; }
        [ForeignKey("id")]
        [Column("globalid")]
        public long? globalId { get; set; }
        [Column("localid")]
        public string localId { get; set; }
        public string? upn  { get; set; }
        public string? partition { get; set; }
        public string? joinseed { get; set; }
    }


    public enum StargateType {
        prime = 0,
        natvie = 1,
        guest = 2,
        privileged = 3,
        userGroup = 10
    }
}
