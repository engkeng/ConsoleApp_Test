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
using System.Runtime.InteropServices;

namespace ConsoleApp_Test
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //string soapServiceUrl = "http://www.dneonline.com/calculator.asmx"; // SOAP endpoint
            //string soapAction = "http://www.dneonline.com/calculator.asmx?op=Add"; // SOAP action

            string userDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);            

            Console.WriteLine("Example CSV file content structure :");
            Console.WriteLine("ContractAccountNo");
            Console.WriteLine("210001448302");
            Console.WriteLine("210005998103 \n");

            Console.WriteLine("Enter the File name of your CSV File (Example: Book1.csv) :");
            var userInput = Console.ReadLine();

            string filePath = $@"{userDirectory}\\Downloads\\{userInput}";        
            string logFilePath = $@"{userDirectory}\\Downloads\\soap_service_log.txt"; ; // Log file path

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
                            writer.WriteLine($"ContractAccountNo: {record.ContractAccountNo}");
                            Console.WriteLine($"ContractAccountNo: {record.ContractAccountNo}");

                            // Prepare the SOAP request for each parameter
                            string soapRequest = GenerateSoapRequest(record.ContractAccountNo);
                            string soapRequest2 = GenerateSoapRequest2(record.ContractAccountNo);

                            // Send SOAP request to web service endpoint
                            var response = await SendSoapRequestAsync(httpClient, soapRequest);
                            var response2 = await SendSoapRequestAsync2(httpClient, soapRequest2);

                            // Extract the value of the <Result> tag from the response
                            string resultValue = ExtractResultFromXml(response);
                            string resultValue2 = ExtractResultFromXml2(response2);

                            // Log only the value of the <Result> tag
                            writer.WriteLine($"PremiseType: {resultValue}");
                            Console.WriteLine($"PremiseType: {resultValue}");

                            writer.WriteLine($"RateCategory: {resultValue2} \n");                            
                            Console.WriteLine($"RateCategory: {resultValue2} \n");

                        }
                    }
                }
            }

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }


        // Method to generate a SOAP request body
        private static string GenerateSoapRequest(string caNo)
        {
            return $@"
            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                              xmlns:urn=""urn:tnb.com.my:BCRM:po:SSP:DM:TMDCreationAndServiceNotification:1.0"">
                <soapenv:Header/>
                <soapenv:Body>
                    <urn:PremiseReqSend>
                        <CA_No>{caNo}</CA_No>
                    </urn:PremiseReqSend>
                </soapenv:Body>
            </soapenv:Envelope>";
        }

        private static string GenerateSoapRequest2(string caNo)
        {
            return $@"
            <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" 
                              xmlns:urn=""urn:tnb.com.my:BCRM:po:SSP:CS:AccountManagement:1.0"">
                <soapenv:Header/>
                <soapenv:Body>
                    <urn:InstallationDetailsRequest>
                        <InstallationDetails>
                            <ContractAccount>{caNo}</ContractAccount>
                        </InstallationDetails>
                    </urn:InstallationDetailsRequest>
                </soapenv:Body>
            </soapenv:Envelope>";
        }

        // Method to send the SOAP request
        private static async Task<string> SendSoapRequestAsync(HttpClient httpClient, string soapRequest)
        {
            
            // Set up the HttpClientHandler with basic authentication
            var handler = new HttpClientHandler
            {
                Credentials = new System.Net.NetworkCredential("PO_MTNB_G", "8CrM_MTNBT@01!")
            };

            using (httpClient = new HttpClient(handler))
            {
                // Set up the HTTP request message
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "http://bcrmpitci.hq.tnb.com.my:50100/XISOAPAdapter/MessageServlet?senderParty=&senderService=SSP_3RD000_T&receiverParty=&receiverService=&interface=PremiseReqSend_Out&interfaceNamespace=urn:tnb.com.my:BCRM:po:SSP:DM:TMDCreationAndServiceNotification:1.0")
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
        }

        private static async Task<string> SendSoapRequestAsync2(HttpClient httpClient, string soapRequest)
        {

            // Set up the HttpClientHandler with basic authentication
            var handler = new HttpClientHandler
            {
                Credentials = new System.Net.NetworkCredential("PO_MTNB_G", "8CrM_MTNBT@01!")
            };

            using (httpClient = new HttpClient(handler))
            {
                // Set up the HTTP request message
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "http://bcrmpitci.hq.tnb.com.my:50100/XISOAPAdapter/MessageServlet?senderParty=&senderService=SSP_3RD000_T&receiverParty=&receiverService=&interface=InstallationDetailsRequest_Out&interfaceNamespace=urn:tnb.com.my:BCRM:po:SSP:CS:AccountManagement:1.0")
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
        }

        // Method to extract the <Result> tag value from the XML response
        private static string ExtractResultFromXml(string xmlResponse)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlResponse);

                // Find the <Result> tag in the XML response
                XmlNode resultNode = doc.GetElementsByTagName("PremiseType").Cast<XmlNode>().FirstOrDefault();

                return resultNode?.InnerText ?? "Result tag not found";  // Return the value inside <Result> or a default message if not found
            }
            catch (Exception ex)
            {
                // In case of error, log the error message
                Console.WriteLine($"Error extracting Result tag: {ex.Message}");
                return "Error extracting Result";
            }
        }

        private static string ExtractResultFromXml2(string xmlResponse)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlResponse);

                // Find the <Result> tag in the XML response
                XmlNode resultNode = doc.GetElementsByTagName("RateCategory").Cast<XmlNode>().FirstOrDefault();

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
            public string ContractAccountNo { get; set; }
        }

    }
}
