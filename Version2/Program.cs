using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

class Program
{
    static void Main()
    {
        // HTML content provided
        string htmlContent = File.ReadAllText("C:\\Users\\p.k.lw6s\\OneDrive - Fareportal, Inc\\Desktop\\PDetails - Copy.html"); // Replace with the provided HTML content

        // Parse HTML
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(htmlContent);

        // Initialize JSON structure
        var jsonOutput = new Dictionary<string, List<Dictionary<string, object>>>
        {
            { "tables", new List<Dictionary<string, object>>() }
        };

        // Find all tables
        var tables = htmlDoc.DocumentNode.SelectNodes("//table");
        int tableIdx = 1;

        foreach (var table in tables)
        {
            var tableData = new Dictionary<string, object>
            {
                { "table_id", $"table_{tableIdx++}" },
                { "title", "" },
                { "columns", new List<string>() },
                { "rows", new List<Dictionary<string, string>>() }
            };

            // Get table title from preceding <b> tag with <u>
            var prevTr = table;
            while (( prevTr = prevTr.PreviousSibling ) != null)
            {
                var bTag = prevTr.SelectSingleNode(".//b[u]");
                if (bTag != null)
                {
                    tableData["title"] = bTag.InnerText.Trim();
                    break;
                }
            }

            // Handle the first table (employee details) differently
            if (tableIdx - 1 == 1) // First table
            {
                tableData["title"] = "Employee Details";
                tableData["columns"] = new List<string> { "Field", "Value" };
                var rows = table.SelectNodes(".//tr");
                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells != null && cells.Count == 8)
                        {
                            // Left side (Field 1: cells[1], Value 1: cells[3])
                            if (!string.IsNullOrEmpty(cells[1].InnerText.Trim()) && !string.IsNullOrEmpty(cells[3].InnerText.Trim()))
                            {
                                ( ( List<Dictionary<string, string>> )tableData["rows"] ).Add(new Dictionary<string, string>
                                {
                                    { "Field", cells[1].InnerText.Trim() },
                                    { "Value", cells[3].InnerText.Trim() }
                                });
                            }
                            // Right side (Field 2: cells[5], Value 2: cells[7])
                            if (!string.IsNullOrEmpty(cells[5].InnerText.Trim()) && !string.IsNullOrEmpty(cells[7].InnerText.Trim()))
                            {
                                ( ( List<Dictionary<string, string>> )tableData["rows"] ).Add(new Dictionary<string, string>
                                {
                                    { "Field", cells[5].InnerText.Trim() },
                                    { "Value", cells[7].InnerText.Trim() }
                                });
                            }
                        }
                    }
                }
            }
            else
            {
                // Extract headers for other tables
                var headers = table.SelectNodes(".//th");
                var columns = headers?.Select(h => h.InnerText.Trim()).ToList() ?? new List<string>();

                if (columns.Any())
                {
                    tableData["columns"] = columns;
                }

                // Extract rows
                var rows = table.SelectSingleNode(".//tbody")?.SelectNodes(".//tr")
                    ?? table.SelectNodes(".//tr")?.Skip(headers != null ? 1 : 0);

                if (rows != null)
                {
                    foreach (var row in rows)
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells != null)
                        {
                            if (cells.Count == columns.Count && columns.Any())
                            {
                                var rowData = new Dictionary<string, string>();
                                for (int i = 0; i < columns.Count; i++)
                                {
                                    rowData[columns[i]] = cells[i].InnerText.Trim();
                                }
                                ( ( List<Dictionary<string, string>> )tableData["rows"] ).Add(rowData);
                            }
                            else if (cells.Count == 1 && !columns.Any()) // Handle single-cell tables
                            {
                                tableData["columns"] = new List<string> { "Address" };
                                ( ( List<Dictionary<string, string>> )tableData["rows"] ).Add(
                                    new Dictionary<string, string> { { "Address", cells[0].InnerText.Trim() } });
                            }
                        }
                    }
                }
            }

            // Only add table if it has data
            if (( ( List<Dictionary<string, string>> )tableData["rows"] ).Any())
            {
                jsonOutput["tables"].Add(tableData);
            }
        }

        // Handle "No Data Available" sections
        var noDataNodes = htmlDoc.DocumentNode.SelectNodes("//tr[.//span[contains(@style, 'color:red') and text()='No Data Available...']]");
        if (noDataNodes != null)
        {
            foreach (var tr in noDataNodes)
            {
                var prevTr = tr;
                while (( prevTr = prevTr.PreviousSibling ) != null)
                {
                    var bTag = prevTr.SelectSingleNode(".//b[u]");
                    if (bTag != null)
                    {
                        jsonOutput["tables"].Add(new Dictionary<string, object>
                        {
                            { "table_id", $"table_{tableIdx++}" },
                            { "title", bTag.InnerText.Trim() },
                            { "columns", new List<string>() },
                            { "rows", new List<Dictionary<string, string>> { new Dictionary<string, string> { { "message", "No Data Available" } } } }
                        });
                        break;
                    }
                }
            }
        }

        // Save to JSON file
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(jsonOutput, options);
        File.WriteAllText("table_data_csharp.json", jsonString);

        Console.WriteLine("JSON file created successfully: table_data_csharp.json");
    }
}