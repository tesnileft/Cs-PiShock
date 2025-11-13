using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog.Targets;
// ReSharper disable InconsistentNaming

namespace CsPiShock;

public class WebSocketAPIv2 : Api
{
    private string _apiKey;
    private string _userId;
    private string _userName;
    internal bool enableWarning = false;
    private ClientWebSocket _webSocket;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="apiKey"> API key obtained from the pishock website</param>
    /// <param name="username"> The username associated with the API key</param>
    /// <exception cref="UserCredentialException"> If unable to get a proper response from the API with the provided credentials</exception>
    public WebSocketAPIv2(string apiKey, string username)
    {
        _userName = username;
        _apiKey = apiKey;
        string userIdRequest =
            $"https://auth.pishock.com/Auth/GetUserIfAPIKeyValid?apikey={apiKey}&username={username}";
        HttpClient client = new HttpClient();
        HttpResponseMessage result = client.GetAsync(userIdRequest).Result;
        JObject jsonResponse = JObject.Parse(result.Content.ReadAsStringAsync().Result);
        string? userId = jsonResponse.Property("UserId")?.ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            _userId = userId;
        }
        else
        {
            throw new UserCredentialException("Could not verify credentials");
        }
        Console.WriteLine("Verified Credentials:");
        Console.WriteLine(userId);

        _webSocket = new ClientWebSocket();
        {
            
        }
        _webSocket.ConnectAsync(new Uri($"wss://broker.pishock.com/v2?Username={_userName}&ApiKey={_apiKey}"), CancellationToken.None).Wait();
        Console.WriteLine("Connected Websocket");
    }
    

    Task<WebSocketReceiveResult> Operate(SocketOperation cmd)
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
            if ((await Operate(new SocketOperation("PING"))).GetType() == typeof(WebSocketReceiveResult))
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
    
    /// <summary>
    /// Method to subscribe to events of shockers, for logging or pings.
    /// Requires specific strings as input.
    /// Preferably use the subscribe method of the specific shocker you are wanting to subscribe to.
    /// </summary>
    /// <param name="targets">Formatted as {client-id}-log or {client-id}-ping</param>
    /// <returns></returns>
    public async Task<bool> Subscribe(string[] targets)
    {
        await Operate(new SubscribeOperation(targets));
        return true;
    }
    /// <summary>
    /// Method to subscribe to unsubscribe of shockers, for logging or pings.
    /// Requires specific strings as input.
    /// Preferably use the unsubscribe method of the specific shocker you are wanting to unsubscribe from.
    /// </summary>
    /// <param name="targets">Formatted as {client-id}-log or {client-id}-ping</param>
    /// <returns></returns>
    public async Task<bool> Unsubscribe(string[] targets)
    {

        await Operate(new SubscribeOperation(targets, true));
        return true;
    }

    //TODO
    public async Task<bool> Publish()
    {
        await Operate(new PublishOperation([]));
        return true;
    }
    
    struct WebsocketRespone
    {
        public string ErrorCode;
        public string IsError;
        public string Message;
        public string OriginalCommand;
    }
    //Base Socket command
    record SocketOperation
    {
        public string Operation { get; set; }
        internal SocketOperation(string command)
        {
            Operation = command;
        }
    }
    
    /// <summary>
    /// Command for sub- or unsubscribing to/from pings and logs
    /// </summary>
    /// <param name="targets"></param>
    /// <param name="unsub"></param>
    record SubscribeOperation : SocketOperation
    {
        public string[]? Targets { get; set; }
        internal SubscribeOperation(string[] targets, bool unsub = false) : base(unsub ? "SUBSCRIBE" : "UNSUBSCRIBE")
        {
            Targets = targets;
        }
    }

    record PublishOperation : SocketOperation
    {
        CommandBody[] PublishCommands { get; set; }
        internal PublishOperation(CommandBody[] commands) : base("PUBLISH")
        {
            PublishCommands = commands;
        }
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    internal Source GetSource()
    {
        return new Source()
        {
            u = _userId,
            ty = "api", // 'sc' for share code, 'api' for api key
            w = enableWarning,
            h = false,
            o = _userName
        };
    }

    /// <summary>
    /// Creates a shocker registered to this api key/share key/user login info
    /// </summary>
    /// <param name="shockerId">ID of the shocker, found in the website/obtainable with internal tools</param>
    /// <returns>A new instance of a shocker with the given ID</returns>
    public override SocketShocker CreateShocker(string shockerId)
    {
        throw new System.NotImplementedException();
    }
}

struct Command
{
    string Target;
    CommandBody Body;
}
struct CommandBody
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
/// <summary>
/// Struct for sub-information for the command body
/// </summary>
struct Source
{
    public string u { get; set; }  // User ID from first step
    public string ty { get; set; } // 'sc' for ShareCode, 'api' for Normal
    public bool w { get; set; }    // true or false, if this is a warning vibrate, it affects the logs
    public bool h { get; set; }    // true if button is held or continuous is being sent.
    public string o { get; set; }  //Set the name shown in the logs
}

