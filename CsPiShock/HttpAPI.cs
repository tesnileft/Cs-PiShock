using System.IO.Ports;
using Newtonsoft.Json;
using System.Text;

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
        private async Task <string> Request(PiCommand command) {
            StringContent jsonString = new (JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");
            var response =await _httpClient.PostAsync("", jsonString);
            return await response.Content.ReadAsStringAsync();
            }

    }
    public class HttpShocker : Shocker
    {
        PiShockHttpApi _api;
        BasicShockerInfo _basicShockerInfo;
        HttpShocker(PiShockHttpApi api)
        {
            _api = api;
            _basicShockerInfo = new BasicShockerInfo();
        }
        public override string ToString()
        {
            return $"{_basicShockerInfo.ToString}";
        }
        public override void Shock(int duration, int intensity)
        {

            throw new System.NotImplementedException();
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