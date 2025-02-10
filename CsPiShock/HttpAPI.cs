using System.IO.Ports;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;

namespace CsPiShock
{
    public class PiShockHttpApi : ApiBase
    {
        const string DefaultName = "C# Script";
        string _name;
        string _apiKey;
        string _scriptName;
        private HttpClient _httpClient;

        public PiShockHttpApi(string name, string apiKey, string scriptName = DefaultName)
        {
            _name = name;
            _apiKey = apiKey;
            _scriptName = scriptName;
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri("https://do.pishock.com/api/");
            _httpClient = httpClient;

        }
        async Task <string> Request(PiCommand command) 
        {
            StringContent jsonString = new (JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");
            var response =await _httpClient.PostAsync("", jsonString);
            return await response.Content.ReadAsStringAsync();
        }

        public override HttpShocker CreateShocker(int shockerId)
        {
            return new HttpShocker(this, shockerId.ToString());
        }
        

    }
    public class HttpShocker : Shocker
    {
        PiShockHttpApi _api;
        BasicShockerInfo _basicShockerInfo;
        
        internal HttpShocker(PiShockHttpApi api, string? name = "")
        {
            _api = api;
            JObject basicShockerInfo = new JObject(); //Should request from the pishock server
            
            _basicShockerInfo = new BasicShockerInfo(basicShockerInfo);
        }
        public override string ToString()
        {
            return $"{_basicShockerInfo.ToString}";
        }
        public override void Shock(int duration, int intensity)
        {
            
        }
        public override void Vibrate(int duration, int intensity)
        {

        }
        public override void Beep(int duration)
        {
            
        }
        public void End()
        {
             
        }
    }

} 