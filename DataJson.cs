using Newtonsoft.Json;
using System.Collections.Generic;

namespace Drive
{
    public class EmployeesData
    {
        [JsonProperty("employees")]
        public List<Employee> Employees { get; set; }
    }

    public class Employee
    {
        [JsonProperty("employeesID")]
        public string EmployeesID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("Position")]
        public string Position { get; set; }

        [JsonProperty("image")]
        public string Image { get; set; }
    }
}