using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog.Targets;

namespace CsPiShock;

public class WebSocketAPI : ApiBase
{
    private string _apiKey;
    private string _userId;
    private string _userName;
    internal bool enableWarning = false;
    private ClientWebSocket _webSocket;
    public WebSocketAPI(string apiKey, string username)
    {
        _userName = username;
        _apiKey = apiKey;
        string userIdRequest =
            $"https://auth.pishock.com/Auth/GetUserIfAPIKeyValid?apikey={apiKey}&username={username}";
        var client = new HttpClient();
        var result = client.GetAsync(userIdRequest).Result;
        JObject jsonResponse = JObject.Parse(result.Content.ReadAsStringAsync().Result);
        string? userId = jsonResponse.Property("UserId")?.ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            _userId = userId;
        }
        else
        {
            throw new Exception("Could not verify credentials");
        }
        Console.WriteLine("Verified Credentials:");
        Console.WriteLine(userId);

        _webSocket = new ClientWebSocket();
        {
            
        }
        _webSocket.ConnectAsync(new Uri($"wss://broker.pishock.com/v2?Username={_userName}&ApiKey={_apiKey}"), CancellationToken.None).Wait();
        Console.WriteLine("Connected Websocket");
    }
    

    Task<WebSocketReceiveResult> Operate(SocketCommand cmd)
    {
        var message = JsonConvert.SerializeObject(cmd);
        //Console.WriteLine($"Sending message:\n{message}");
        _webSocket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, default).Wait();
        var response = new ArraySegment<byte>(new Byte[1024]);
        return _webSocket.ReceiveAsync(response, default).WaitAsync(TimeSpan.FromSeconds(5));
    }
    
    public async Task<bool> Ping()
    {
        try
        {
            if (Operate(new SubscribeCommand("PING")).Result.GetType() == typeof(WebSocketReceiveResult))
            {
                return true;
            }
        }
        catch (AggregateException e)
        {
            Console.WriteLine("Ping Failed: " + e.InnerException.Message);
        }
        return false;
    }

    class SocketCommand
    {
        public string Operation { get; set; }

        protected SocketCommand(string command)
        {
            Operation = command;
        }
    }

    class SubscribeCommand : SocketCommand
    {
        public string[]? Targets { get; set; }

        internal SubscribeCommand(string command) : base(command)
        {
            
        }
    }

    class PublishCommand : SocketCommand
    {
        
        internal PublishCommand(CommandBody[] commands) :base("PUBLISH")
        {
            PublishCommands = commands;
        }
        CommandBody[] PublishCommands { get; set; }
        
    }

    /// <summary>
    /// This is a bit weird, but because of the hierarchy of the Json required for the API it just works
    /// </summary>
    /// <returns></returns>
    internal Source GetSource()
    {
        return new Source()
        {
            u = _userId,
            ty = "api", //Since we're just using the API key, I'm not dealing with opening a window rn.
            w = enableWarning,
            h = false,
            o = _userName
        };
    }

    public override SocketShocker CreateShocker(string shockerId)
    {
        throw new System.NotImplementedException();
    }
    
}
class CommandBody
{
    public int id { get; set; }     //Shocker ID
    public char m { get; set; }        // 'v', 's', 'b', or 'e'
    public int? i { get; set; }        // Could be vibIntensity, shockIntensity or a randomized value
    public int d { get; set; }         // Calculated duration in milliseconds
    public bool r { get; set; } = true;// true or false, always set to true.
    public Source l { get; set; }

    internal CommandBody(int shockerId, char mode, int duration, int? intensity, Source source)
    {
        id = shockerId;
        m = mode;
        d = duration;
        i = intensity;
        l = source;
    }
}

class Source
{
    public string u { get; set; }  // User ID from first step
    public string ty { get; set; } // 'sc' for ShareCode, 'api' for Normal
    public bool w { get; set; }    // true or false, if this is a warning vibrate, it affects the logs
    public bool h { get; set; }    // true if button is held or continuous is being sent.
    public string o { get; set; }  //Set the name shown in the logs
}

public class SocketShocker : Shocker
{
    private WebSocketAPI _api;
    BasicShockerInfo _info;
    internal SocketShocker(WebSocketAPI api,int id, string name)
    {
        _api = api;
        _info = new BasicShockerInfo()
        {
            IsSerial = false,
            IsPaused = false,
            Name = name,
            ShockerId = id
        };
    }

    internal void Call(char mode, int duration, int? intensity)
    {
        new CommandBody(_info.ShockerId, mode, duration, intensity, _api.GetSource());
    }
    public override void Shock(int duration, int intensity)
    {
        Call('s', duration, intensity);
    }

    public override void Vibrate(int duration, int intensity)
    {
        throw new System.NotImplementedException();
    }

    public override void Beep(int duration)
    {
        
        throw new System.NotImplementedException();
    }
}