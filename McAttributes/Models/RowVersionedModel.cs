using System.ComponentModel.DataAnnotations;

namespace McAttributes.Models {
    public class RowVersionedModel {

        // This is the conncurrency ID when using PostgresSQL or other databases
        // that don't support concurrency same as the SQL Server client.
        // NOTE change this datatype to byte[] if you plan to use this with SQL Server
        [Timestamp]
        public uint Version { get; set; }
    }
}
