using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.IO;
using System.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        var client = new HttpClient();
        var baseUrl = "https://ehrms.upsdc.gov.in//ReportSummary/PublicReports/PtwoDetails?empcd={0}&deptid=UPD0003&type=2";
        int startEmpcd = 2200165;  // Starting empcd
        int endEmpcd = 2200169;    // 2 million records from start
        string outputFile = "employee_data.csv";

        // CSV headers
        var headers = new List<string>
        {
            "Name", "eHRMS Code", "Father's Name", "Home District", "Date of Birth", "Cadre", "Gender",
            "Service Start Date", "Spouse eHRMS Code", "Current Status", "Appointment Date",
            "Permanent Address", "Local Address", "District", "Office Name", "Designation", "Post Name", "Joining Date"
        };

        // Write headers if file doesn't exist
        if (!File.Exists(outputFile))
        {
            File.WriteAllText(outputFile, string.Join(",", headers) + "\n");
        }

        // Crawl data
        for (int empcd = startEmpcd; empcd <= endEmpcd; empcd++)
        {
            var url = string.Format(baseUrl, empcd);
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync();
                    var data = ExtractData(html);
                    // Append to CSV
                    var row = headers.Select(h => $"\"{data.GetValueOrDefault(h, "").Replace("\"", "\"\"")}\"").ToArray();
                    await File.AppendAllTextAsync(outputFile, string.Join(",", row) + "\n");
                    Console.WriteLine($"Processed empcd: {empcd}");
                }
                else
                {
                    Console.WriteLine($"Failed to fetch empcd: {empcd}, Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error for empcd {empcd}: {ex.Message}");
            }
            await Task.Delay(1000);  // 1-second delay
        }
    }

    static Dictionary<string, string> ExtractData(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var data = new Dictionary<string, string>();

        // Extract personal details
        var personalTable = doc.DocumentNode.SelectSingleNode("//div[@id='FullpagePrint']//table");
        var rows = personalTable.SelectNodes("tr");
        var values = new List<string>();
        foreach (var tr in rows)
        {
            var tds = tr.SelectNodes("td");
            if (tds != null)
            {
                if (tds.Count >= 4) values.Add(tds[3].InnerText.Trim());
                if (tds.Count >= 8) values.Add(tds[7].InnerText.Trim());
            }
        }

        if (values.Count >= 17)
        {
            data["Name"] = values[0];
            data["eHRMS Code"] = values[1];
            data["Father's Name"] = values[2];
            data["Home District"] = values[6];
            data["Date of Birth"] = values[4];
            data["Cadre"] = values[8];
            data["Gender"] = values[10];
            data["Service Start Date"] = values[12];
            data["Spouse eHRMS Code"] = values[14];
            data["Current Status"] = values[16];
            data["Appointment Date"] = values[11];
        }

        // Extract present posting details
        var postingTables = doc.DocumentNode.SelectNodes("//table[@id='tblDEStatus']");
        if (postingTables != null && postingTables.Count > 0)
        {
            var postingRow = postingTables[0].SelectSingleNode("tbody/tr");
            if (postingRow != null)
            {
                var tds = postingRow.SelectNodes("td");
                if (tds != null && tds.Count >= 5)
                {
                    data["District"] = tds[0].InnerText.Trim();
                    data["Office Name"] = tds[1].InnerText.Trim();
                    data["Designation"] = tds[3].InnerText.Trim();
                    data["Post Name"] = tds[3].InnerText.Trim();
                    data["Joining Date"] = tds[4].InnerText.Trim();
                }
            }
        }

        // Extract Permanent Address
        var permAddressB = doc.DocumentNode.SelectSingleNode("//b[text()='Permanent Address']");
        if (permAddressB != null)
        {
            var addressTable = permAddressB.Ancestors("tr").FirstOrDefault()
                ?.NextSibling?.NextSibling?.SelectSingleNode("td/table");
            if (addressTable != null)
            {
                data["Permanent Address"] = addressTable.SelectSingleNode("tbody/tr/td")?.InnerText.Trim() ?? "";
            }
        }

        // Extract Local Address
        var localAddressB = doc.DocumentNode.SelectSingleNode("//b[text()='Local Address']");
        if (localAddressB != null)
        {
            var addressTable = localAddressB.Ancestors("tr").FirstOrDefault()
                ?.NextSibling?.NextSibling?.SelectSingleNode("td/table");
            if (addressTable != null)
            {
                data["Local Address"] = addressTable.SelectSingleNode("tbody/tr/td")?.InnerText.Trim() ?? "";
            }
        }

        return data;
    }
}