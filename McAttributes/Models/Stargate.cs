using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace McAttributes.Models {
    [Table("stargate")]
    [Index(nameof(LocalId), nameof(Partition))]
    public class Stargate {
        [Key]
        [Column("id")]
        public long Id { get; set; }

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


        // This is the conncurrency ID when using PostgresSQL or other databases
        // that don't support concurrency same as the SQL Server client.
        [Timestamp]
        public uint Version { get; set; }
    }


    public enum StargateType {
        prime = 0,
        natvie = 1,
        guest = 2,
        privileged = 3,
        userGroup = 10
    }
}
