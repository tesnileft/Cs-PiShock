using System.IO.Ports;
using System.Net;
using System.Reflection;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;

namespace CsPiShock
{
    public class PiShockHttpApi : ApiBase
    {
        const string DefaultName = "C# Script";
        const string DefaultUrl = "https://do.pishock.com/api/";
        
        string _name = "DefaultUsername";
        string _apiKey = "1";
        string _scriptName;
        private HttpClient _httpClient;

        internal enum Operation
        {
            Shock = 0,
            Vibrate = 1,
            Beep = 2
        }

        /// <summary>
        /// Makes the HTTP api object that can be used to send requests to the PiChock servers with proper authentication.
        /// </summary>
        /// <param name="name">The Username for the request</param>
        /// <param name="apiKey">The api key acquired from the pishock website. Should be your own, not the one of whichever share code you received</param>
        /// <param name="scriptName">Optional custom name for the API, shows up in the feed on the website</param>
        public PiShockHttpApi(string name, string apiKey, string scriptName = DefaultName)
        {
            _name = name;
            _apiKey = apiKey;
            _scriptName = scriptName;
            
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(DefaultUrl);
            httpClient.DefaultRequestHeaders.Add("User-Agent", _scriptName + Assembly.GetExecutingAssembly().GetName().Version);
            _httpClient = httpClient;

        }
        HttpResponseMessage Request(HttpPiCommand command, string endpoint) 
        {
            StringContent jsonString = new (JsonConvert.SerializeObject(command), Encoding.UTF8, "application/json");
            var response =  _httpClient.PostAsync(endpoint, jsonString).Result;
            return response;
        }

        public bool VerifyCredentials()
        {
            HttpPiCommand command = GetCommand(); //Makes a raw command that only has credentials
            
            try
            {
                Request(command, "VerifyApiCredentials");
            }
            catch(Exception e)
            {
                if (e.InnerException is HttpRequestException)
                {
                    
                }
                Console.WriteLine("Could not verify credentials.\n Error message: " + e.Message);
                return false;
            }
            return true;
        }

        public override HttpShocker CreateShocker(string shockerId)
        {
            return new HttpShocker(this, shockerId);
        }

        internal HttpPiCommand GetCommand()
        {
            HttpPiCommand command = new HttpPiCommand(_scriptName, _name, _apiKey);
            return command;
        }
        internal void Operate(string code, Operation operation, int? duration, int? intensity)
        {
            var command = GetCommand();
            if (duration.HasValue)
            {
                command.Duration = (float)duration/100; //Go from ms to seconds
            }
            command.Intensity = intensity;
            command.Op = (int)operation;
            command.Code = code;
            command.Clamp();
            Request(command, "apioperate");
        }

        public void TestBuzz()
        {
            HttpPiCommand command = new HttpPiCommand(_scriptName, _name, _apiKey)
            {
                Code = "2ABD4353099",
                Duration = 1,
                Intensity = 50,
                Op = (int)Operation.Vibrate,
            };
            command.Clamp();
            Request(command, "apioperate");
        }
        
        internal class HttpPiCommand
        {
            public string Username { get; set; }
            public string ApiKey { get; set; }
            public string? Code { get; set; }
            public string Name { get; set; }
            public int Op { get; set; }
            public float? Duration { get; set; }
            public int? Intensity { get; set; }
            
            internal HttpPiCommand(string name, string user, string apiKey)
            {
                Name = name;
                Username = user;
                ApiKey = apiKey;
            }

            
            /// <summary>
            /// Clamps duration and intensity to be compliant with the api
            /// </summary>
            public void Clamp()
            {
                if (Duration != null)
                {
                    Duration = float.Clamp(Duration.Value, .1f, 1.5f);
                }
                if (Intensity != null)
                {
                    Intensity = int.Clamp(Intensity.Value, 1, 100);
                }
            }
        }
    }
    public class HttpShocker : Shocker
    {
        PiShockHttpApi _api;
        BasicShockerInfo _basicShockerInfo;
        internal string _code;
        
        
        
        /// <summary>
        /// Makes a new HTTP shocker, needs an API
        /// </summary>
        /// <param name="api"></param>
        /// <param name="shockerCode">Shocker code of the shocker you're controlling</param>
        internal  HttpShocker(PiShockHttpApi api, string shockerCode)
        {
            JObject basicShockerInfo = new JObject(); //Should request from the pishock server
            _api = api;
            _code = shockerCode;
            _basicShockerInfo = new BasicShockerInfo(basicShockerInfo);
        }
        public override string ToString()
        {
            return $"{_basicShockerInfo.ToString}";
        }

        
        internal void Call(PiShockHttpApi.Operation op, int duration = 100, int? intensity = 5)
        {
            _api.Operate(this._code, op, duration, intensity);
            
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
        
    }

    internal static class Extensions
    {
        
    }

} 