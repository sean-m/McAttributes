namespace McAttributes.Models {
    public class whoami {
        public string Region { get; } = Environment.GetEnvironmentVariable("REGION") ?? "unknown";
        public string Replica { get; } = Environment.GetEnvironmentVariable("CONTAINER_APP_REPLICA_NAME")
                    ?? Environment.MachineName
                    ?? "unknown";
        public string Revision { get; } = Environment.GetEnvironmentVariable("CONTAINER_APP_REVISION") ?? "unknown";
    }
}
