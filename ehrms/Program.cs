using HtmlAgilityPack;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        var client = new HttpClient();
        //1057292
        // https://ehrms.upsdc.gov.in//ReportSummary/PublicReports/PtwoDetails?empcd=1057292&deptid=UPD0003&type=2
        var baseUrl = "https://ehrms.upsdc.gov.in//ReportSummary/PublicReports/PtwoDetails?empcd={0}&deptid=UPD0003&type=2";
        int startEmpcd = 1057293; // Starting empcd
        int endEmpcd = 1057293;   // 2 million records from start
        string outputFile = "employee_data.csv";

        // CSV headers for specified fields
        var headers = new List<string>
        {
            "Name", "eHRMS Code", "Father's Name", "Home District", "Date of Birth", "Cadre", "Gender",
            "Service Start Date", "Spouse eHRMS Code", "Current Status", "Appointment Date",
            "Permanent Address", "Local Address", "District", "Office Name", "Designation", "Post Name", "Joining Date",
            "All Table Data"
        };

        // Write headers if file doesn't exist
        if (!File.Exists(outputFile))
        {
            File.WriteAllText(outputFile, string.Join(",", headers.Select(h => $"\"{h}\"")) + "\n");
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
            await Task.Delay(1000); // 1-second delay
        }
    }

    static Dictionary<string, string> ExtractData(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var data = new Dictionary<string, string>();
        var allTableData = new StringBuilder();

        // Extract personal details from the table inside div#FullpagePrint
        var personalTable = doc.DocumentNode.SelectSingleNode("//div[@id='FullpagePrint']//table//table");
        if (personalTable != null)
        {
            var rows = personalTable.SelectNodes(".//tr");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("td|th");
                    if (cells != null)
                    {
                        // Collect all <td> and <th> values
                        var cellValues = cells.Select(c => c.InnerText.Trim()).ToList();
                        allTableData.AppendLine(string.Join("|", cellValues));

                        // Map specific fields based on labels
                        if (cells.Count >= 4)
                        {
                            var label = cells[1].InnerText.Trim().Replace("\u00A0", " ");
                            var value = cells[3].InnerText.Trim();
                            switch (label)
                            {
                                case "Name": data["Name"] = value; break;
                                case "eHRMS Code": data["eHRMS Code"] = value; break;
                                case "Father's Name": data["Father's Name"] = value; break;
                                case "Home District": data["Home District"] = value; break;
                                case "Date of Birth": data["Date of Birth"] = value; break;
                                case "Cadre": data["Cadre"] = value; break;
                                case "Gender": data["Gender"] = value; break;
                                case "Service Start Date": data["Service Start Date"] = value; break;
                                case "Spouse eHRMS Code": data["Spouse eHRMS Code"] = value; break;
                                case "Current Status": data["Current Status"] = value; break;
                                case "Appointment Date": data["Appointment Date"] = value; break;
                            }
                        }
                        // Check second set of columns if present
                        if (cells.Count >= 8)
                        {
                            var label = cells[5].InnerText.Trim().Replace("\u00A0", " ");
                            var value = cells[7].InnerText.Trim();
                            switch (label)
                            {
                                case "Name": data["Name"] = value; break;
                                case "eHRMS Code": data["eHRMS Code"] = value; break;
                                case "Father's Name": data["Father's Name"] = value; break;
                                case "Home District": data["Home District"] = value; break;
                                case "Date of Birth": data["Date of Birth"] = value; break;
                                case "Cadre": data["Cadre"] = value; break;
                                case "Gender": data["Gender"] = value; break;
                                case "Service Start Date": data["Service Start Date"] = value; break;
                                case "Spouse eHRMS Code": data["Spouse eHRMS Code"] = value; break;
                                case "Current Status": data["Current Status"] = value; break;
                                case "Appointment Date": data["Appointment Date"] = value; break;
                            }
                        }
                    }
                }
            }
        }

        // Extract present posting details from the first table#tblDEStatus
        var postingTables = doc.DocumentNode.SelectNodes("//table[@id='tblDEStatus']");
        if (postingTables != null && postingTables.Count > 0)
        {
            var postingTable = postingTables[0];
            var headers = postingTable.SelectNodes(".//thead//tr//th");
            if (headers != null)
            {
                allTableData.AppendLine("Present Posting Headers: " + string.Join("|", headers.Select(h => h.InnerText.Trim())));
            }
            var rows = postingTable.SelectNodes(".//tbody//tr");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("td");
                    if (cells != null && cells.Count >= 5)
                    {
                        allTableData.AppendLine("Present Posting: " + string.Join("|", cells.Select(c => c.InnerText.Trim())));
                        data["District"] = cells[0].InnerText.Trim();
                        data["Office Name"] = cells[1].InnerText.Trim();
                        data["Designation"] = cells[3].InnerText.Trim();
                        data["Post Name"] = cells[3].InnerText.Trim();
                        data["Joining Date"] = cells[4].InnerText.Trim();
                    }
                }
            }
        }

        // Extract qualifications from the second table#tblDEStatus
        if (postingTables != null && postingTables.Count > 1)
        {
            var qualTable = postingTables[1];
            var headers = qualTable.SelectNodes(".//thead//tr//th");
            if (headers != null)
            {
                allTableData.AppendLine("Qualifications Headers: " + string.Join("|", headers.Select(h => h.InnerText.Trim())));
            }
            var rows = qualTable.SelectNodes(".//tbody//tr");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("td");
                    if (cells != null)
                    {
                        allTableData.AppendLine("Qualifications: " + string.Join("|", cells.Select(c => c.InnerText.Trim())));
                    }
                }
            }
        }

        // Extract past posting details from the third table#tblDEStatus
        if (postingTables != null && postingTables.Count > 2)
        {
            var pastTable = postingTables[2];
            var headers = pastTable.SelectNodes(".//thead//tr//th");
            if (headers != null)
            {
                allTableData.AppendLine("Past Posting Headers: " + string.Join("|", headers.Select(h => h.InnerText.Trim())));
            }
            var rows = pastTable.SelectNodes(".//tbody//tr");
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("td");
                    if (cells != null)
                    {
                        allTableData.AppendLine("Past Posting: " + string.Join("|", cells.Select(c => c.InnerText.Trim())));
                    }
                }
            }
        }

        // Extract Permanent Address
        var permAddressNode = doc.DocumentNode.SelectSingleNode("//b[contains(text(), 'Permanent Address')]");
        if (permAddressNode != null)
        {
            var addressTable = permAddressNode.Ancestors("tr").FirstOrDefault()
                ?.SelectSingleNode("following-sibling::tr[2]//table");
            if (addressTable != null)
            {
                var cells = addressTable.SelectNodes(".//td");
                if (cells != null)
                {
                    data["Permanent Address"] = cells[0].InnerText.Trim();
                    allTableData.AppendLine("Permanent Address: " + cells[0].InnerText.Trim());
                }
            }
        }

        // Extract Local Address
        var localAddressNode = doc.DocumentNode.SelectSingleNode("//b[contains(text(), 'Local Address')]");
        if (localAddressNode != null)
        {
            var addressTable = localAddressNode.Ancestors("tr").FirstOrDefault()
                ?.SelectSingleNode("following-sibling::tr[2]//table");
            if (addressTable != null)
            {
                var cells = addressTable.SelectNodes(".//td");
                if (cells != null)
                {
                    data["Local Address"] = cells[0].InnerText.Trim();
                    allTableData.AppendLine("Local Address: " + cells[0].InnerText.Trim());
                }
            }
        }

        // Store all table data in a single field
        data["All Table Data"] = allTableData.ToString().Replace("\n", "; ").Replace("\r", "");

        return data;
    }
}