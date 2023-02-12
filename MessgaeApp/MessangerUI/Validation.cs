using System.Management;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace MessangerUI
{
    internal class ClientValidation
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("expiry_date")]
        public DateTime ExpiryDate { get; set; }
    }

    internal static class Validation
    {
        private static string getMachineDetails()
        {
            List<string> hardware = new List<string>();
            foreach (var properties in new Dictionary<string, string[]>
                {
                    { "Win32_DiskDrive", new[] { "Model", "Manufacturer", "Signature", "TotalHeads" } },
                    { "Win32_Processor", new[] { "UniqueId", "ProcessorId", "Name", "Manufacturer" } },
                    { "Win32_BaseBoard", new[] { "Model", "Manufacturer", "Name", "SerialNumber" } }
                })
            {
                var managementClass = new ManagementClass(properties.Key);
                var managementObject = managementClass.GetInstances().Cast<ManagementBaseObject>().First();

                foreach (var prop in properties.Value)
                {
                    if (null != managementObject[prop])
                        hardware.Add(managementObject[prop].ToString());
                }
            }

            var hash = new SHA256Managed().ComputeHash(Encoding.UTF8.GetBytes(string.Join("", hardware)));
            return string.Join("", hash.Select(x => x.ToString("X2")));
        }

        private static string base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        private static string base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        private static void encodeJson()
        {
            var json = "[\r\n    {\r\n        \"allow_email\": \"Y\",\r\n        \"expiry_date\": \"2023-02-11\",\r\n        \"id\": \"47AFED26A54435DC853796E6F09BEF39F461D0522AA03B3DF18B35D6C6EB7B8B\"\r\n    },\r\n    {\r\n        \"allow_email\": \"Y\",\r\n        \"expiry_date\": \"2023-02-15\",\r\n        \"id\": \"xyz\"\r\n    },\r\n    {\r\n        \"allow_email\": \"Y\",\r\n        \"expiry_date\": \"2023-02-15\",\r\n        \"id\": \"xyz\"\r\n    }\r\n]";

            var encoded = base64Encode(json);
        }

        private static string getKeyFromHtml(string inputHtml)
        {
            var tdStartIndex = inputHtml.LastIndexOf("<td");
            var tdEndIndex = inputHtml.LastIndexOf("</td>");

            var tdData = inputHtml.Substring(tdStartIndex, tdEndIndex - tdStartIndex);
            var angularClosingIndex = tdData.IndexOf(">")+1;

            var resultKey = tdData.Substring(angularClosingIndex, tdData.Length - angularClosingIndex);

            return resultKey;
        }

        internal static (bool, string) ValidateUserMachine()
        {
            try
            {
                //encodeJson();
                var httpClient = new HttpClient();
                var contentsJson = httpClient.GetStringAsync("https://github.com/amitverma80/chatDetails/blob/main/chatApp.txt");

                var result = contentsJson.Result;

                string jsonString = base64Decode(getKeyFromHtml(result));

                var clientDetails = JsonSerializer.Deserialize<IList<ClientValidation>>(jsonString);

                var currentMachineId = getMachineDetails();

                var machineDetail = clientDetails.Where(x => x.Id.Equals(currentMachineId)).FirstOrDefault();

                if(machineDetail == null)
                {
                    return (false, $"Machine not registered yet. Contact Owner and share key - {currentMachineId}");
                }

                if(machineDetail.ExpiryDate.CompareTo(DateTime.Now.Date)<0) 
                {
                    return (false, "Machine license expired. Contact Owner");
                }


            }
            catch (Exception ex)
            { 
                return (false, ex.Message);
            }
            return (true, string.Empty);
        }
    }
}
