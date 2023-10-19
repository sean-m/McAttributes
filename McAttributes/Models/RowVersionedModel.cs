using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McAttributes.Models {
    public class RowVersionedModel {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        // This is the conncurrency ID when using PostgresSQL or other databases
        // that don't support concurrency same as the SQL Server client.
        // NOTE change this datatype to byte[] if you plan to use this with SQL Server
        [Timestamp]
        public uint Version { get; set; }
    }
}
