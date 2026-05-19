using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Identity.Client;

namespace McAttributes.Models {
    public class whoami {
        public static string Region { get; } = Environment.GetEnvironmentVariable("REGION") ?? "unknown";
        public static string Replica { get; } = Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME")
                    ?? Environment.MachineName
                    ?? "unknown";
        public static string Revision { get; } = Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION") ?? "unknown";

        public static whoami Instance { get; } = new whoami(); // These values don't change anyhow
    }
}
