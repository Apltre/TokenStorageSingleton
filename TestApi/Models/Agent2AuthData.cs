using Microsoft.AspNetCore.Mvc;

namespace TestApi.Models
{
    public class Agent2AuthData : StandardAuthData
    {
        [BindProperty(Name = "field_1")]
        public string Field1 { get; set; }

        [BindProperty(Name = "field_2")]
        public string Field2 { get; set; }
    }
}