using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net.Http;
using System.Runtime.Remoting.Messaging;
using CsvHelper;
using System.Globalization;
using System.Security.Cryptography;
using System.Xml;

namespace ConsoleApp_Test
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //string soapServiceUrl = "http://www.dneonline.com/calculator.asmx"; // SOAP endpoint
            //string soapAction = "http://www.dneonline.com/calculator.asmx?op=Add"; // SOAP action
           
            string filePath = "C:\\Users\\puahe\\Downloads\\Book1.csv"; // File path for CSV            
            string logFilePath = "C:\\Users\\puahe\\Downloads\\soap_service_log.txt"; // Log file path

            // Create an HttpClient to send SOAP requests
            using (var httpClient = new HttpClient())
            {
                // Create a StreamWriter to log output into a file
                using (var writer = new StreamWriter(logFilePath, append: true)) // append: true means appending to the file
                {
                    // Read CSV file and process each row
                    using (var reader = new StreamReader(filePath))
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        // Read records from the CSV file
                        var records = csv.GetRecords<CSVRecord>().ToList();

                        foreach (var record in records)
                        {
                            writer.WriteLine($"Parameter: {record.Column1}, {record.Column2}");
                            Console.WriteLine($"Parameter: {record.Column1}, {record.Column2}");

                            // Prepare the SOAP request for each parameter
                            string soapRequest = GenerateSoapRequest(record.Column1, record.Column2);

                            // Send SOAP request to web service endpoint
                            var response = await SendSoapRequestAsync(httpClient, soapRequest);

                            // Extract the value of the <Result> tag from the response
                            string resultValue = ExtractResultFromXml(response);

                            // Log only the value of the <Result> tag
                            writer.WriteLine($"Response: {resultValue} \n");
                            Console.WriteLine($"Response: {resultValue} \n");
                        }
                    }
                }
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

       
        // Method to generate a SOAP request body
        private static string GenerateSoapRequest(string param1, string param2)
        {
            return $@"
            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                              xmlns:tem=""http://tempuri.org/"">
                <soapenv:Header/>
                <soapenv:Body>
                    <tem:Add>
                        <tem:intA>{param1}</tem:intA>
                        <tem:intB>{param2}</tem:intB>
                    </tem:Add>
                </soapenv:Body>
            </soapenv:Envelope>";
        }

        // Method to send the SOAP request
        private static async Task<string> SendSoapRequestAsync(HttpClient httpClient, string soapRequest)
        {
            // Set up the HTTP request message
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "http://www.dneonline.com/calculator.asmx?op=Add")
            {
                Content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml")
            };

            // Send the SOAP request and get the response
            var response = await httpClient.SendAsync(requestMessage);
            string result = await response.Content.ReadAsStringAsync();

            // Read the response content
            if (response.IsSuccessStatusCode)
            {
                return result;
            }
            else
            {
                Console.WriteLine($"Failed to call SOAP service for param: {soapRequest}");
                return result;
            }
            
        }

        // Method to extract the <Result> tag value from the XML response
        private static string ExtractResultFromXml(string xmlResponse)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlResponse);

                // Find the <Result> tag in the XML response
                XmlNode resultNode = doc.GetElementsByTagName("AddResult").Cast<XmlNode>().FirstOrDefault();

                return resultNode?.InnerText ?? "Result tag not found";  // Return the value inside <Result> or a default message if not found
            }
            catch (Exception ex)
            {
                // In case of error, log the error message
                Console.WriteLine($"Error extracting Result tag: {ex.Message}");
                return "Error extracting Result";
            }
        }

        // Class to map CSV records
        public class CSVRecord
        {
            public string Column1 { get; set; }
            public string Column2 { get; set; }
        }

    }
}
