using System.Data;
using System.Net.Mime;
using System.Text;
using SerialPortLib;

namespace CsPiShock
{
    using System.Diagnostics;
    using System.Management;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Collections.Concurrent;
    using Newtonsoft.Json;
    using Microsoft.Win32;
    using Newtonsoft.Json.Linq;
    
    /// <summary>
    /// Low level access to PiShock serial functionality
    /// </summary>
    public class PiShockSerialApi : ApiBase
    {
        //Possible VID and PID port values for the PiShock
        static List<(int, int)> USB_IDS = new List<(int, int)>()
        {
            (0x1A86, 0x7523), //CH340, PiShock Next
            (0x1A86, 0x55D4), //CH9102, PiShock Lite
        };

        enum DeviceType
        {
            NEXT = 4,
            LITE = 3
        }
        const string TERMINAL_INFO = "TERMINALINFO: ";
        
        public string? ComPort;
        private SerialPortLib.SerialPortInput _serialPort;
        const int InfoTimeout = 1000;
        
        public JObject Info { get; set; }
        public bool DebugEnabled { get; set; }
        
        ConcurrentQueue<PiCommand> _command_queue = new ConcurrentQueue<PiCommand>();
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        List<SerialShocker> serialShockers = new List<SerialShocker>();
        private ManualResetEvent _infoUpdated = new(false);
        
        private void SerialPort_HandleMessage(object sender, MessageReceivedEventArgs e)
        {
            string piOutput = Encoding.Default.GetString(e.Data);
            Console.WriteLine(piOutput);
            string[] splitOutput = piOutput.Split('\n');
            try
            {
                string terminalInfo = splitOutput.First(x => x.StartsWith(TERMINAL_INFO));
                if (terminalInfo != string.Empty)
                {
                    Info = (JObject)JsonConvert.DeserializeObject(terminalInfo.Substring(TERMINAL_INFO.Length,
                        terminalInfo.Length - TERMINAL_INFO.Length));
                    _infoUpdated.Set();
                }
            }
            catch
            {
                
            }
        }

        public PiShockSerialApi(string? providedPort = null)
        {
            ComPort = GetPort(providedPort); //Get the com port
            _serialPort = new SerialPortInput();
            _serialPort.SetPort(ComPort, 115200);
            _serialPort.Connect();
            
            _serialPort.MessageReceived += SerialPort_HandleMessage;
            Console.WriteLine("Connected to " + ComPort);
            StartQueueThread();
            UpdateInfo(500);
            Console.WriteLine("Got PiShock Info");
            
        }

        /// <summary>
        /// Initializes the port that will be used for serial
        /// </summary>
        /// <param name="providedPort"> Only needs to be provided if the OS is not Windows, but if you have a custom port you can provide it </param>
        private string GetPort(string? providedPort = null)
        {
            if (providedPort == null)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
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

        public override SerialShocker CreateShocker(string shockerId)
        {
            SerialShocker serialShocker = new SerialShocker(int.Parse(shockerId), this);
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

        public void UpdateInfo(int timeOut = InfoTimeout, bool debug = false)
        {
            _infoUpdated.Reset();
            SendCommand("info");
            if (!_infoUpdated.WaitOne(timeOut))
            {
                Console.WriteLine("Failed to update info, retrying...");
                UpdateInfo(timeOut, debug);
            }
            //Info = JObject.Parse();
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
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

            ManagementBaseObject piShock = collection.Cast<ManagementBaseObject>().First();
            if (piShock != null)
            {
                Console.WriteLine("Found PiShock: " + piShock["Caption"]);
                string CUR_CTRL = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\";

                String PiShockPort =
                    Registry.GetValue(CUR_CTRL + "Enum\\" + piShock["PnpDeviceId"] + "\\Device Parameters", "PortName",
                        "")!.ToString()!;
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
        private void StartQueueThread()
        {
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            new Thread(() =>
            {
                while (true)
                {
                    if (_command_queue.TryDequeue(out PiCommand command))
                    {
                        string jsonString = BuildCommand(command);
                        Console.WriteLine("Sending command: " + jsonString);
                        _serialPort.SendMessage(Encoding.UTF8.GetBytes(jsonString));
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }).Start();
        }
        
        public void Operate(int shockerId, SerialOperation operation, int? duration = null, int? intensity = null)
        {
            OperationValues values = new OperationValues(shockerId, operation, duration, intensity);
            PiCommand cmd = new PiCommand("operate", values);
            SendCommand(cmd);
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
                Thread.Sleep(
                    100); //Wait 100 ms to ensure every command is not executed too fast (and thus is actually picked up by the PiShock)
            }

            //Close our port and thread
            _serialPort.Disconnect();
            _cancellationTokenSource.Cancel();
        }
    }
}