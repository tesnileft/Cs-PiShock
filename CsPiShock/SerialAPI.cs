namespace CsPiShock
{
    using System.Diagnostics;
    using System.Management;
    using System.IO.Ports;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Collections.Concurrent;
    using Newtonsoft.Json;
    using Microsoft.Win32;
    using Newtonsoft.Json.Linq;
    using static CsPiShock.ApiBase;


    /// <summary>
    /// Low level access to PiShock serial functionality
    /// </summary>
    public class PiShockSerialApi : ApiBase
    {
        //Possible VID and PID port values for the PiShock
        static List<(int, int)> USB_IDS = new List<(int, int)>(){
            (0x1A86, 0x7523),  //CH340, PiShock Next
            (0x1A86, 0x55D4),  //CH9102, PiShock Lite
         };

        enum DeviceType
        {
            NEXT = 4,
            LITE = 3
        }
        const string TERMINAL_INFO = "TERMINALINFO: ";
        //Class variables
        public string? ComPort;
        SerialPort _serialPort = null!;
        const int InfoTimeout = 20;
        ConcurrentQueue<PiCommand> _command_queue = new ConcurrentQueue<PiCommand>();
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        List<SerialShocker> serialShockers = new List<SerialShocker>();

        /// <summary>
        /// Mainly for debugging the development of this, you should probably run this on a seperate thread
        /// </summary>
        public void EnableDebug()
        {
            _serialPort.DataReceived += (s, e) => Console.WriteLine("Data received: " + _serialPort.ReadLine());
        }
        public PiShockSerialApi(string? providedPort = null)
        {
            ComPort = GetPort(providedPort);    //Get the com port
            Connect();                          //Initialize the serial port
            StartThread();
        }

        /// <summary>
        /// Initializes the port that will be used for serial
        /// </summary>
        /// <param name="providedPort"> Only needs to be provided if the OS is not Windows, but if you have a custom port you can provide it </param>
        private string GetPort(string? providedPort = null)
        {
            if (providedPort == null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Console.WriteLine(ComPort);
                    return GetComPort();
                }
                else
                {
                    Debug.WriteLine("No port provided, defaulting to COM3, fuck you Mac users <3");
                    return "COM3";
                }
            }
            else
            {
                return providedPort;
            }
        }
        /// <summary>
        /// Initializes the connection to the PiShock over serial
        /// </summary>
        private void Connect()
        {
            _serialPort = new SerialPort
            {
                PortName = ComPort,
                BaudRate = 115200
            };
            _serialPort.Open();
            Console.WriteLine("Connected to " + ComPort);
        }

        public SerialShocker GetShocker(int shockerId)
        {
            SerialShocker serialShocker = new SerialShocker(shockerId, this);
            return serialShocker;

        }

        /// <summary>
        /// Sends an operation to the PiShock
        /// </summary>
        private void SendCommand(PiCommand piCommand)
        {
            _command_queue.Enqueue(piCommand);
        }
        private void SendCommand(string command)
        {
            PiCommand piCommand = new PiCommand
            {
                cmd = command
            };
            _command_queue.Enqueue(piCommand);
        }

        private string BuildCommand(PiCommand piCommand)
        {
            return JsonConvert.SerializeObject(piCommand);
        }

        public JObject Info(int timeOut = InfoTimeout, bool debug = false)
        {
            SendCommand("info");
            return JObject.Parse(WaitInfo(timeOut, debug));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeOut"></param>
        /// <param name="debug"></param>
        /// <returns>JSON string of the info</returns>
        /// <exception cref="TimeoutException"></exception>
        private string WaitInfo(int timeOut = InfoTimeout, bool debug = false)
        {
            int count = 0;
            while (timeOut > count)
            {
                Thread.Sleep(5);
                string line = _serialPort.ReadLine();

                if (debug)
                    Console.WriteLine(line);

                if (line.StartsWith(TERMINAL_INFO))
                {
                    return line[TERMINAL_INFO.Length..];
                }
                count++;
            }
            Dispose();
            throw new TimeoutException("Timed out waiting for info, make sure the given device is indeed a PiShock");
        }

        public void AddNetwork(string ssid, string pass)
        {
            PiCommand networkCommand = new PiCommand
            {
                cmd = "addnetwork",
                value = new NetworkValues
                {
                    ssid = ssid,
                    password = pass
                }
            };
            SendCommand(networkCommand);
        }

        public void RemoveNetwork(string ssid)
        {
            PiCommand networkCommand = new PiCommand
            {
                cmd = "removenetwork",
                value = new NetworkValues
                {
                    ssid = ssid,
                }
            };
            SendCommand(networkCommand);
        }
        public void TryConnect(string ssid, string pass)
        {
            PiCommand networkCommand = new PiCommand
            {
                cmd = "connect",
                value = new NetworkValues
                {
                    ssid = ssid,
                    password = pass
                }
            };
            SendCommand(networkCommand);
        }
        public void Restart()
        {
            SendCommand("restart");
        }

        private static string GetComPort()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetComPortLin();
            }
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetComPortWin();
            }

            return "COM 3";
        }
        /// <summary>
        /// Windows specific method of obtaining the COM port the PiShock is connected to
        /// </summary>
        /// <returns> COM port of the PiShock</returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NullReferenceException"></exception>
        [SupportedOSPlatform("windows")]
        private static string GetComPortWin()
        {
            using var searcher = new ManagementObjectSearcher(
                $"Select * From Win32_PnPEntity where PNPDeviceID Like '%{Convert.ToString(USB_IDS[0].Item1, 16)}%' AND (PNPDeviceID Like '%{Convert.ToString(USB_IDS[0].Item2, 16)}%' OR PNPDeviceID Like '%{Convert.ToString(USB_IDS[1].Item2, 16)}%')");

            using ManagementObjectCollection collection = searcher.Get();
            //Console.WriteLine("Found " + collection.Count + " devices");
            if (collection.Count > 1)
            {
                throw new Exception("Multiple devices found");
            }

            if (collection.Count == 0)
            {
                throw new Exception("No devices found");
            }

            ManagementBaseObject PiShock = collection.Cast<ManagementBaseObject>().First();
            if (PiShock != null)
            {
                Console.WriteLine("Found PiShock: " + PiShock["Caption"]);
                string CUR_CTRL = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\";

                String PiShockPort = Registry.GetValue(CUR_CTRL + "Enum\\" + PiShock["PnpDeviceId"] + "\\Device Parameters", "PortName", "")!.ToString()!;
                return PiShockPort;
            }

            else
            {
                throw new NullReferenceException("The PiShock was found but it doesn't exist, this shouldn't happen");
            }
        }

        [SupportedOSPlatform("linux")]
        private static string GetComPortLin()
        {
            return LinuxDeviceManager.GetDeviceUsbPort(USB_IDS);
        }
        //Checks the device file in Linux for the vendor and device ID
        bool PiShockCheckerLin(string portName)
        {
            LinuxDeviceManager.GetDeviceUsbPort(USB_IDS);
            return true;
        }
        /// <summary>
        /// Starts the thread that reads commands from the input queue and sends them to the PiShock
        /// </summary>
        private void StartThread()
        {
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            new Thread(() =>
            {
                while (true)
                {
                    if (_command_queue.TryDequeue(out PiCommand command))
                    {
                        string jsonString = BuildCommand(command);
                        _serialPort.WriteLine(jsonString);
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }).Start();
        }

        /// <summary>
        /// Dispose of all resources used by the class (use this to properly close the ports/thread)
        /// </summary>
        public void Dispose()
        {
            //We probably want to set all the shockers to be off by this point
            foreach (SerialShocker shocker in serialShockers)
            {
                shocker.End();
                Thread.Sleep(100); //Wait 100 ms to ensure every command is not executed too fast (and thus is actually picked up by the PiShock)
            }
            
            //Close our port and thread
            _serialPort.Close();
            _cancellationTokenSource.Cancel();
        }

        public void Operate(int shockerId, SerialOperation operation, int? duration = null, int? intensity = null)
        {
            OperationValues values = new OperationValues(shockerId, operation, duration, intensity);
            PiCommand cmd = new PiCommand("operate", values);
            SendCommand(cmd);
        }
        /// <summary>
        /// Help struct that is sent to the processing thread to then be sent to the pishock async from the main thread.
        /// </summary>
        private struct ShockerCommand
        {
            public string cmd { get; set; }
            public int? id { get; set; }
            public SerialOperation? op { get; set; }
            public int? duration { get; set; }
            public int? intensity { get; set; }
            public ShockerCommand(string command, int? id = null, SerialOperation? op = null, int? duration = null, int? intensity = null)
            {
                cmd = command;
                this.id = id;
                this.op = op;
                this.duration = duration;
                this.intensity = intensity;
            }
        }

    }
    public class SerialShocker : Shocker
    {
        private BasicShockerInfo info;
        private PiShockSerialApi api;

        public SerialShocker(int shockerId, PiShockSerialApi api)
        {
            this.api = api;
            this.info = _Info(shockerId);
        }
        public override string ToString()
        {
            return $"Serial shocker {info.ShockerId} ({api.ComPort})";
        }
        public override void Shock(int duration, int intensity)
        {
            api.Operate(info.ShockerId, SerialOperation.SHOCK, duration, intensity);
        }
        /// <summary>
        /// 
        /// </summary>
        public override void Vibrate(int duration, int intensity)
        {
            api.Operate(info.ShockerId, SerialOperation.VIBRATE, duration, intensity);
        }
        public override void Beep(int duration)
        {
            api.Operate(info.ShockerId, SerialOperation.BEEP, duration);
        }
        /// <summary>
        /// End the currently running operation
        /// </summary>
        public void End()
        {
            api.Operate(info.ShockerId, SerialOperation.END);
        }
        private BasicShockerInfo _Info(int shockerId)
        {
            JObject terminalInfo = api.Info();
            JToken shocker = terminalInfo.SelectToken("shockers")!.First(x => (int)x.SelectToken("id")! == shockerId);
            if (shocker.SelectToken("id") == null | shocker.SelectToken("id")!.Value<int>() != shockerId)
            {
                throw new Exception("Shocker not found");
            }
            BasicShockerInfo info = new BasicShockerInfo()
            {
                IsSerial = true,
                Name = "Serial Shocker " + shockerId,
                ClientId = (int)terminalInfo.SelectToken("clientId")!,
                ShockerId = shockerId,
                IsPaused = shocker.SelectToken("paused")!.Value<bool>(),
            };
            return info;
        }
        /// <summary>
        /// Get the basic information of the Shocker
        /// </summary>
        /// <returns> A <c>BasicShockerInfo</c> instance</returns>
        public BasicShockerInfo Info()
        {
            return info;
        }
    }
}