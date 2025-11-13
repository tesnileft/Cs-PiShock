namespace CsPiShock;

public class SocketShocker : Shocker
{
    private WebSocketAPIv2 _api;
    BasicShockerInfo _info;
    internal SocketShocker(WebSocketAPIv2 api,int id, string name)
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

    //TODO
    internal void Call(char mode, int duration, int? intensity)
    {
        throw new System.NotImplementedException();
    }
    public override void Shock(int duration, int intensity)
    {
        throw new System.NotImplementedException();
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