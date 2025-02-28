using Newtonsoft.Json.Linq;

namespace CsPiShock;
public class SerialShocker : Shocker
    {
        private BasicShockerInfo info;
        private PiShockSerialApi api;

        public BasicShockerInfo Info { get; private set; }
        
        internal SerialShocker(int shockerId, PiShockSerialApi api)
        {
            this.api = api;
            JToken shocker = api.Info.SelectToken("shockers")!.First(x => (int)x.SelectToken("id")! == shockerId);
            if (shocker.SelectToken("id") == null | shocker.SelectToken("id")!.Value<int>() != shockerId)
            {
                throw new Exception("Shocker not found");
            }
            this.info = new BasicShockerInfo()
            {
                IsSerial = true,
                Name = "Serial Shocker " + shockerId,
                ClientId = (int)api.Info.SelectToken("clientId")!,
                ShockerId = shockerId,
                IsPaused = shocker.SelectToken("paused")!.Value<bool>(),
            };
        }
        public override string ToString()
        {
            return $"Serial shocker {info.ShockerId} ({api.ComPort})";
        }
        public override void Shock(int duration, int intensity)
        {
            api.Operate(info.ShockerId, ApiBase.SerialOperation.SHOCK, duration, intensity);
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Vibrate(int duration, int intensity)
        {
            api.Operate(info.ShockerId, ApiBase.SerialOperation.VIBRATE, duration, intensity);
        }
        public override void Beep(int duration)
        {
            api.Operate(info.ShockerId, ApiBase.SerialOperation.BEEP, duration);
        }
        /// <summary>
        /// End the currently running operation
        /// </summary>
        public void End()
        {
            api.Operate(info.ShockerId, ApiBase.SerialOperation.END);
        }
    }