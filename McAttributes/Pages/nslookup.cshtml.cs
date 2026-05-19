using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Net.Sockets;

namespace McAttributes.Pages
{
    public class nslookupModel : PageModel
    {
        [BindProperty]
        public string? Hostname { get; set; }

        [BindProperty]
        public string? DnsServer { get; set; }

        public List<DnsResult> Results { get; set; } = new List<DnsResult>();

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Hostname))
            {
                ErrorMessage = "Hostname is required";
                return Page();
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(DnsServer))
                {
                    ErrorMessage = "Custom DNS server lookup requires system tools. Using system DNS instead.";
                }

                await LookupWithSystemDns(Hostname);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error: {ex.Message}";
            }

            return Page();
        }

        private async Task LookupWithSystemDns(string hostname)
        {
            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(hostname);

                // Separate IPv4 and IPv6 addresses
                var ipv4Addresses = hostEntry.AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString())
                    .ToList();

                var ipv6Addresses = hostEntry.AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                    .Select(ip => ip.ToString())
                    .ToList();

                if (ipv4Addresses.Any())
                {
                    Results.Add(new DnsResult
                    {
                        QueryType = "A (IPv4)",
                        Hostname = hostEntry.HostName,
                        Addresses = ipv4Addresses
                    });
                }

                if (ipv6Addresses.Any())
                {
                    Results.Add(new DnsResult
                    {
                        QueryType = "AAAA (IPv6)",
                        Hostname = hostEntry.HostName,
                        Addresses = ipv6Addresses
                    });
                }

                // Add aliases if any
                if (hostEntry.Aliases.Length > 0)
                {
                    Results.Add(new DnsResult
                    {
                        QueryType = "Aliases (CNAME)",
                        Hostname = hostname,
                        Addresses = hostEntry.Aliases.ToList()
                    });
                }

                if (!Results.Any())
                {
                    ErrorMessage = "No DNS records found";
                }
            }
            catch (SocketException ex)
            {
                ErrorMessage = $"DNS lookup failed: {ex.Message}";
            }
        }

        public class DnsResult
        {
            public string QueryType { get; set; } = string.Empty;
            public string Hostname { get; set; } = string.Empty;
            public List<string> Addresses { get; set; } = new List<string>();
            public string? Ttl { get; set; }
        }
    }
}


