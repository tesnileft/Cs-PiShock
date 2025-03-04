using System.CodeDom;
using System.Reflection.Metadata.Ecma335;
using Newtonsoft.Json.Linq;
/// <summary>
/// Should be overridden
/// </summary>
public abstract class Shocker
{
    //     """Base class for :class:`HTTPShocker <pishock.zap.httpapi.HTTPShocker>` and
    //     :class:`SerialShocker <pishock.zap.serialapi.SerialShocker>`.

    //     Applications which only need access to
    //     :meth:`shock() <pishock.zap.httpapi.HTTPShocker.shock()>`,
    //     :meth:`vibrate() <pishock.zap.httpapi.HTTPShocker.vibrate()>`,
    //     :meth:`beep() <pishock.zap.httpapi.HTTPShocker.beep()>`, and
    //     :meth:`info() <pishock.zap.httpapi.HTTPShocker.info()>` (with
    //     :class:`BasicShockerInfo <pishock.zap.core.BasicShockerInfo>` only) can swap out a
    //     :class:`HTTPShocker <pishock.zap.httpapi.HTTPShocker>` for a
    //     :class:`SerialShocker <pishock.zap.serialapi.SerialShocker>` (with only
    //     initialization changing) to support both APIs.
    //     """

    /// <summary>
    /// 
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="intensity"></param>
    abstract public void Shock(int duration, int intensity);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="intensity"></param>
    abstract public void Vibrate(int duration, int intensity);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="duration"></param>
    abstract public void Beep(int duration);
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    BasicShockerInfo Info()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Used by <c> PiShockAPI.GetShockers()</c> and <c>SerialShocker.Info()</c>.
    /// <para>Calling <c> HttpShocker.info() </c> instead returns a <c> HttpShocker.DetailedShockerInfo </c> instance.</para>
    /// <para>Name: name of this shocker in the web interface (or generated one if it's serial)</para>
    /// <para>ClientId: The ID of the PiShock hub this shocker belongs to</para>
    /// <para>ShockerId: The ID of this shocker</para>
    /// <para>IsPaused: Whether the shocker is currently paused</para>
    /// </summary>
    public class BasicShockerInfo
    {
        public string? Name { get; set; }
        public int ClientId { get; set; }
        public int ShockerId { get; set; }
        public bool IsPaused { get; set; }
        public bool IsSerial { get; set; }
        public BasicShockerInfo() //Dioverridey lol
        { }
        public BasicShockerInfo(JObject data, string? name = null)
        {
            ClientId = (int)data.SelectToken("client_id")!;
            Name = data.SelectToken("name") != null ? name : (string)data.SelectToken("name")!;
            ShockerId = (int)data.SelectToken("id")!;
            IsPaused = (bool)data.SelectToken("paused")!;
        }
        public override string ToString()
        {
            return $"Shocker {Name}, {ShockerId} from {ClientId}";
        }
    }
}
public class NotImplementedException : Exception
{
    public NotImplementedException()
    {
    }

    public NotImplementedException(string message) : base(message)
    {
        
    }
    public NotImplementedException(string message, Exception innerException) : base(message, innerException)
    {
    }

}