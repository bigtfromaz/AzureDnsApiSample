using System.Text;
using Azure.Identity;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using Azure.ResourceManager.Resources;
using Microsoft.Extensions.Configuration;
using AzureDnsApiSandBox;
using Azure.ResourceManager.Dns.Models;
using Azure;
using System.Reflection.Metadata;

namespace DnsApiSandBox;



internal class Program
{
    static void Main(string[] args)
    {
        const string DEFAULT_HOSTNAME = "abc01zyx";

        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        AppSettings settings = builder.Get<AppSettings>()
                    ?? throw new ArgumentException("Could not find AppSettings. Are they in your project secrets?");

        // Create the client secret credential
        var clientSecretCredential = new ClientSecretCredential(settings.AzureDNSCreds.TenantId, settings.AzureDNSCreds.ClientId, settings.AzureDNSCreds.ClientSecret);

        // Create the ARM client
        ArmClient armClient = new(clientSecretCredential);

        // Get the subscription
        ResourceIdentifier subscriptionIdentifier = SubscriptionResource.CreateResourceIdentifier(settings.AzureDNSCreds.SubscriptionID);

        // Get the subscription resource
        SubscriptionResource subscriptionResource = armClient.GetSubscriptionResource(subscriptionIdentifier).Get();

        // Get the subscription's data/information
        SubscriptionData data = subscriptionResource.Data;
        Console.WriteLine($"Opened Subscription " + $"Named: {data.DisplayName} - Id {data.SubscriptionId}\n");

        // Get all the DNS Zones in the subscription
        List<DnsZoneResource> dnsZones = subscriptionResource.GetDnsZones().ToList<DnsZoneResource>();
        _ = subscriptionResource.GetDnsZones().ToList<DnsZoneResource>();

        // Print out the zones
        Console.WriteLine($"Found {dnsZones.Count} DNS Zones");
        dnsZones.ForEach(zone => Console.WriteLine($"   {zone.Data.Name}"));

        // Get a single Zone    
        DnsZoneResource? currentZone = dnsZones.FirstOrDefault(zone => zone.Data.Name == settings.ZoneName) ?? throw new Exception("Zone not found");

        Console.WriteLine($"\nCurrent Zone Name is: {currentZone.Data.Name}");

        // Create a new host name
        string newHostName = DEFAULT_HOSTNAME;

        // Create a metadata dictionary
        var metadata = new Dictionary<string, string>
                {
                    { "OUR_HOST_KEY", Environment.MachineName },
                    { "Requestor-IPaddress", "192.0.2.4" },
                    { "datetime", $"{DateTime.Now.ToShortDateString()}-{DateTime.Now.ToShortTimeString()}" }
                };

        // Create a new  Data record object. Interestingly, the host name is readonly here. 
        // The host name is set in the CreateOrUpdate method
        var newDnsARecordData = ArmDnsModelFactory.DnsARecordData(metadata: metadata);
        newDnsARecordData.TtlInSeconds = 60;

        // Creates two new A Data records and adds them to the A record data object
        newDnsARecordData.DnsARecords.Add(NewARecord("192.0.2.5"));
        newDnsARecordData.DnsARecords.Add(NewARecord("192.0.2.6"));

        // Set a tag for the zone, and remove it
        Console.WriteLine($"\nTagging {currentZone.Data.Name} with a \"sample\" tag. This will take several seconds...");
        currentZone.AddTag("sample", "true");
        Console.WriteLine($"\nTagged {currentZone.Data.Name} with a \"sample\" tag.\n");

        // Refresh the zone so the tag can be gotten
        dnsZones = subscriptionResource.GetDnsZones().ToList<DnsZoneResource>();
        currentZone = dnsZones.FirstOrDefault(zone => zone.Data.Name == settings.ZoneName) ?? throw new Exception("Zone not found");

        // Get the tag
        currentZone.Data.Tags.TryGetValue("sample", out var value);

        // Remove the tag
        Console.WriteLine($"\nRemoving the \"sample\" tag from {currentZone.Data.Name}. This will take several seconds...");
        currentZone.RemoveTag("sample");
        Console.WriteLine($"\nRemoved the \"sample\" tag from {currentZone.Data.Name}.\n");


        // Stores the new A record data object in the zone
        currentZone.GetDnsARecords().CreateOrUpdate(WaitUntil.Completed, newHostName, newDnsARecordData);
        Console.WriteLine($"\nCreated new {newHostName} A record in {currentZone.Data.Name}");

        // Create a new AAAA reco
        DnsAaaaRecordData newDnsAaaaRecordData = ArmDnsModelFactory.DnsAaaaRecordData(metadata: metadata);
        newDnsAaaaRecordData.TtlInSeconds = 60;

        // Creates two new AAAA Data records and adds them to the AAAA record data object
        newDnsAaaaRecordData.DnsAaaaRecords.Add(NewAaaaRecord("2001:DB8::1:1:1:2"));

        // Stores the new Aaaa record data object in the zone
        currentZone.GetDnsAaaaRecords().CreateOrUpdate(WaitUntil.Completed, newHostName, newDnsAaaaRecordData);
        Console.WriteLine($"\nCreated new {newHostName} AAAA record in {currentZone.Data.Name}");


        PrintOutZone(currentZone);

        // Sample fetch of a zone record
        Console.WriteLine($"\nAttemting to fetch recently created host: {newHostName}");
        Response<DnsARecordResource> newHostRecord = currentZone.GetDnsARecord(newHostName);
        Console.WriteLine(newHostRecord != null ? $"Located {newHostRecord.Value.Data.Fqdn}" : $"Error: Could not locate {newHostName}");

        // Delete the host
        DeleteHost(currentZone, newHostName);
        Console.WriteLine($"Deleted host {newHostName} from {currentZone.Data.Name}");
    }
    /// <summary>given an IPv4 address, create a new A record</summary>
    /// <param name="ipv4Address"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static DnsARecordInfo NewARecord(string ipv4Address)
    {
        System.Net.IPAddress ipAddr = System.Net.IPAddress.Parse(ipv4Address);
        if (ipAddr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new ArgumentException("Not an IPv4 address");
        }
        DnsARecordInfo newIPv4DnsRecord = new()
        {
            IPv4Address = ipAddr
        };
        return newIPv4DnsRecord;
    }
    /// <summary>
    /// Get a list of all the hosts in the zone
    /// </summary>
    /// <param name="dnsARecords"></param>
    /// <param name="dnsAaaaRecords"></param>
    /// <returns></returns>
    private static List<string> GetHostList(List<DnsARecordResource> dnsARecords, List<DnsAaaaRecordResource> dnsAaaaRecords)
    {
        List<string> hostList = new();
        foreach (var record in dnsARecords)
        {
            hostList.Add(record.Data.Name);
        }
        foreach (var record in dnsAaaaRecords)
        {
            if (!hostList.Contains(record.Data.Name))
            {
                hostList.Add(record.Data.Name);
            }
        }
        return hostList;
    }
    /// <summary>
    /// Deletes all the records having the given host name. This method deletes both A and AAAA records.
    /// </summary>
    /// <param name="zone"></param>
    /// <param name="hostName"></param>
    private static void DeleteHost(DnsZoneResource zone, string hostName)
    {
        DnsARecordCollection aRecords = zone.GetDnsARecords();
        foreach (var record in aRecords)
        {
            if (record.Data.Name == hostName)
                record.Delete(WaitUntil.Completed);
        }

        DnsAaaaRecordCollection aaaaRecords = zone.GetDnsAaaaRecords();
        foreach (var record in aaaaRecords)
        {
            if (record.Data.Name == hostName)
                record.Delete(WaitUntil.Completed);
        }
    }
    /// <summary>
    /// Creates a new AAAA record given an IPv6 address string
    /// </summary>
    /// <param name="ipv6Address"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static DnsAaaaRecordInfo NewAaaaRecord(string ipv6Address)
    {
        System.Net.IPAddress ipAddr = System.Net.IPAddress.Parse(ipv6Address);
        if (ipAddr.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            throw new ArgumentException("Not an IPv6 address");
        }
        DnsAaaaRecordInfo newIPv6DnsRecord = new()
        {
            IPv6Address = ipAddr
        };
        return newIPv6DnsRecord;
    }

    /// <summary>
    /// Print out the zone information
    /// </summary>
    /// <param name="currentZone"></param>
    /// <remarks>
    /// This method takes a DnsZoneResource object as a parameter. This object represents a DNS zone in Azure. The method prints out various details about the DNS zone and its records.
    /// </remarks>
    private static void PrintOutZone(DnsZoneResource currentZone)
    {
        StringBuilder sb = new();
        sb.AppendLine("\nZone Information");
        sb.AppendLine($"\nZone Name: {currentZone.Data.Name}");
        sb.AppendLine($"Zone Type: {currentZone.Data.ZoneType}");
        sb.AppendLine($"Zone Id: {currentZone.Data.Id}");
        sb.AppendLine($"Zone Location: {currentZone.Data.Location}");
        sb.AppendLine($"Zone Tags: {currentZone.Data.Tags}");

        List<DnsARecordResource> dnsARecords = currentZone.GetDnsARecords().ToList<DnsARecordResource>();
        List<DnsAaaaRecordResource> dnsAaaaRecords = currentZone.GetDnsAaaaRecords().ToList<DnsAaaaRecordResource>();
        List<string> hostNames = GetHostList(dnsARecords, dnsAaaaRecords);

        sb.AppendLine($"\nFound {hostNames.Count} host namess in the zone");

        List<DnsRecordData> recordSets = currentZone.GetAllRecordData().OrderBy(recData => recData.Name).ToList<DnsRecordData>();
        sb.AppendLine($"\nFound {dnsARecords.Count} A (IPv4) recordsets in the zone");
        sb.AppendLine($"\nFound {dnsAaaaRecords.Count} AAAA (IPv6) recordsets in the zone");

        bool includeAll = true;

        sb.AppendLine($"\nListing address records found in the zone\n");
        foreach (DnsRecordData record in recordSets)
        {
            includeAll = true;
            //if (record.Name != currentHostName) { includeAll = false; } // Uncomment this line to only show the current host
            if (includeAll)
            {
                if (record.DnsARecords.Count > 0)
                {
                    sb.AppendLine($"\n   Host Name: {record.Name} - TTL: {record.TtlInSeconds}   IPv4 Count: {record.DnsARecords.Count}");
                    foreach (var aRecord in record.DnsARecords)
                    {
                        sb.AppendLine($"      IPv4 Address: {aRecord.IPv4Address}");
                    }
                }
                if (record.DnsAaaaRecords.Count > 0)
                {
                    sb.AppendLine($"\n   Host Name: {record.Name} - TTL: {record.TtlInSeconds}   IPv6 Count: {record.DnsAaaaRecords.Count}");
                    foreach (var aRecord in record.DnsAaaaRecords)
                    {
                        sb.AppendLine($"      IPv6 Address: {aRecord.IPv6Address}");
                    }
                }
            }
        }

        Console.Write(sb.ToString());
    }
}

