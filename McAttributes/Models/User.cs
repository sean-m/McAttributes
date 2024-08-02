using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using IndexAttribute = Microsoft.EntityFrameworkCore.IndexAttribute;

namespace McAttributes.Models {
    [Table("azusers")]
    [Index(nameof(AadId), nameof(Mail), nameof(EmployeeId))]
    public class User : RowVersionedModel {

        [Column("lastfetched")]
        public DateTime? LastFetched { get; set; } = null;

        [Column("merged")]
        public DateTime? Merged { set; get; } = null;

        [Column("modified")]
        public DateTime? Modified { get; set; } = null;

        [Column("created")]
        public DateTime? Created { get; set; } = null;

        [Column("enabled")]
        public bool Enabled { get; set; }

        [Column("deleted")]
        public bool Deleted { get; set; }

        [Column("tenant")]
        public string? Tenant { get; set; }

        [Column("aadid")]
        public Guid AadId { get; set; }

        [Column("upn")]
        public string? Upn { get; set; }

        [Column("mail")]
        public string? Mail { get; set; }

        [Column("displayname")]
        public string? DisplayName { get; set; }

        [Column("employeeid")]
        public string? EmployeeId { get; set; }

        [Column("ademployeeid")]
        public string? AdEmployeeId { get; set; }

        [Column("hremployeeid")]
        public string? HrEmployeeId { get; set; }

        [Column("wid")]
        public string? Wid { get; set; }

        [Column("creationtype")]
        public string? CreationType { get; set; }

        [Column("company")]
        public string? Company { get; set; }

        [Column("title")]
        public string? Title { get; set; }

        [Column("preferredgivenname")]
        public string? PreferredGivenName { get; set; }

        [Column("preferredsurname")]
        public string? PreferredSurname { get; set; }

        [Column("articles")]
        [StringLength(64)]
        public string? Articles { get; set; }

        [Column("pronouns")]
        [StringLength(24)]
        public string? Pronouns { get; set; }

        [Column("onpremisedn")]
        public string? OnPremiseDn { get; set; }

        [Column("signinactivity", TypeName = "jsonb")]
        public JsonDocument? SigninActivityJson { get; set; }

        public Dictionary<string,string?> SigninActivity {
            get {
                var result = new Dictionary<string,string?>();
                if (SigninActivityJson == null) return result;
                var root = SigninActivityJson.RootElement;
                switch (root.ValueKind) {
                    case JsonValueKind.Object:
                        var enumerator = root.EnumerateObject();
                        foreach (var n in enumerator) {
                            result.Add(n.Name, n.Value.ToString());
                        }
                        break;
                    default:
                        result.Add("Value", root.ToString());
                        break;
                }
                return result;
            }
        }
    }
}
