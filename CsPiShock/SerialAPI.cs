namespace CsPiShock;
using System.Diagnostics;
using System.Management;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Collections.Concurrent;
using static Shocker;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.Text;
using System.Data.SqlTypes;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

/// <summary>
/// Low level access to PiShock serial functionality
/// </summary>
public class PiShockSerialApi
{
    //Possible VID and PID port values for the PiShock
    static List<(int, int)> USB_IDS = new List<(int, int)>(){
    (0x1A86, 0x7523),  //CH340, PiShock Next
    (0x1A86, 0x55D4),  //CH9102, PiShock Lite
    };

    /// <summary>
    /// Enum for the serial operations
    /// 
    /// <para>SHOCK:    Send a shock to the shocker.</para>
    /// <para>VIBRATE:  Send a vibration to the shocker</para>
    /// <para>BEEP:     Send a beep to the shocker.</para>
    /// <para>END:      End the current operation.</para>
    /// </summary>
    
    enum SerialOperation 
    {
        SHOCK = 1,
        VIBRATE = 2,
        BEEP = 3,
        END = 0
    }
    enum DeviceType
    {
        NEXT = 4,
        LITE = 3
    }
    const string TERMINAL_INFO = "TERMINALINFO: ";
    //Class variables
    private string? ComPort;
    SerialPort _serialPort = null!;
    const int InfoTimeout = 20;
    ConcurrentQueue<ShockerCommand> _command_queue = new ConcurrentQueue<ShockerCommand>();
    CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    List<SerialShocker> serialShockers = new List<SerialShocker>();

    /// <summary>
    /// Main function for debugging
    /// </summary>
    private static void Main()
    {
        PiShockSerialApi pishock = new PiShockSerialApi();
        //SerialShocker s = new SerialShocker(8619, pishock);
        //Console.WriteLine(pishock.Info()); //Print the info of the shocker
        SerialShocker shockerA = new SerialShocker(8619, pishock);
        SerialShocker shockerB = new SerialShocker(9509, pishock);

        bool running = true;
        while (running)
        {
            switch(Console.ReadLine())
            {
                case "Info":
                Console.WriteLine(pishock.Info());
                break;
                case "Stop":
                case "stop":
                pishock.Dispose();
                running = false;
                break;
                case "sa":
                shockerA.Vibrate(100,100);
                break;
                case "sb":
                shockerB.Vibrate(100,100);
                break;
                case "aandb":
                shockerA.Vibrate(100,20);
                Thread.Sleep(100);
                shockerB.Vibrate(100,20);
                break;
            }
        }
    }
    /// <summary>
    /// Mainly for debugginf the development of this, you should probably run this on a seperate thread
    /// </summary>
    void EnableDebug()
    {
        _serialPort.DataReceived += (s, e) => Console.WriteLine("Data received: " + _serialPort.ReadLine());
    }
    PiShockSerialApi(string? providedPort = null)
    {
        ComPort = GetPort(providedPort);    //Get the com port
        Connect();                          //Initialize the serial port
        StartThread();
    }

    /// <summary>
    /// Initializes the port that will be used for serial
    /// </summary>
    /// <param name="providedPort"> Only needs to be provided if the OS is not Windows, but if you have a custom port you can provide it </param>
    string GetPort(string? providedPort = null)
    {
        if (providedPort == null)
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine(ComPort);
            return GetComPortWin();
            }
            else
            {
                Debug.WriteLine("No port provided, defaulting to COM3");
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
    void Connect()
    {
        _serialPort = new SerialPort
        {
            PortName = ComPort,
            BaudRate = 115200
        };
        Console.WriteLine("Connecting to " + ComPort);
        _serialPort.Open();
        Console.WriteLine("Connected to " + ComPort);
    }

    
    /// <summary>
    /// Sends an operation to the PiShock
    /// </summary>
    /// <param name="shocker_id"> ID of the shocker </param>
    /// <param name="operation"> <c>SerialOperation</c> representing the operation</param>
    /// <param name="duration"> Duration in milliseconds </param>
    /// <param name="intensity"> Range from 0 to 100 </param>
    
        
    void SendCommand(PiCommand piCommand)
    {
        _serialPort.WriteLine(BuildCommand(piCommand));
    }   
    void SendCommand(string command)
    {
        PiCommand piCommand = new PiCommand
        {
            cmd = command
        };
        _serialPort.WriteLine(BuildCommand(piCommand));
    }

    string BuildCommand(PiCommand piCommand)
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
    string WaitInfo(int timeOut = InfoTimeout, bool debug = false)
    {
        int count = 0;
        while(timeOut > count)
        {
            string line = _serialPort.ReadLine();

            if(debug)
            Console.WriteLine(line);

            if (line.StartsWith(TERMINAL_INFO))
            {
                return line[TERMINAL_INFO.Length..];
            }
            count++;
        }
        throw new TimeoutException("Timed out waiting for info, make sure the given device is indeed a PiShock");
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
            const String CUR_CTRL = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\";
            
            String PiShockPort = Registry.GetValue(CUR_CTRL + "Enum\\" + PiShock["PnpDeviceId"] + "\\Device Parameters", "PortName", "")!.ToString()!;
            return PiShockPort;
        }

        else
        {
            throw new NullReferenceException("This should never happen");
        }  
    }

    public void StartThread()
    {
        CancellationToken cancellationToken = _cancellationTokenSource.Token;
        new Thread(() =>
        {
            while (true)
            {
                if (_command_queue.TryDequeue(out var eQ))
                {
                        if (eQ.intensity != null && (0 > eQ.intensity | eQ.intensity > 100))
                        {
                            throw new Exception("Intensity must be between 0 and 100");
                        }
                        if (eQ.duration < 0 | eQ.duration > (Math.Pow(2, 32) - 1))
                        {
                            throw new Exception($"Duration must be between 0 and 2^32, not {eQ.duration}");
                        }
                        PiCommand command = new PiCommand()
                        {
                            cmd = eQ.cmd,
                            value = eQ.id == null ? null : 
                            new OperationValues((int)eQ.id, eQ.op, eQ.duration, eQ.intensity)
                        };
                        SendCommand(command);
                    
                    //Do command stuff
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

    /// <summary>
    /// Help struct to generate JSON strings to send to the pishock
    /// </summary>
    struct PiCommand
    {
        public string cmd { get ; set;}
        public OperationValues? value {get ; set;}
        public PiCommand(string command)
        {
            cmd = command;
        }
    }
    /// <summary>
    /// Help struct to generate JSON strings to send to the PiShock
    /// </summary>
    struct OperationValues
    {
        public int? id { get; set;}
        public string? op { get; set;}
        public int? duration {get; set;}
        public int? intensity { get; set;}
        /// <summary>
        /// Simple initializer for <c>OperationValues</c>
        /// </summary>
        /// <param name="shockerId"> ID of the shocker </param>
        /// <param name="operation"> Type of operation (Takes a value from <c>SerialOperation</c>)</param>
        /// <param name="opIntensity"></param>
        public OperationValues(int? shockerId, SerialOperation? operation, int? opDuration, int? opIntensity=null)
        {
            string[] operations = {"end", "shock", "vibrate", "beep"};
            id = shockerId;
            op = operations[(int) operation];
            duration = opDuration;
            intensity = opIntensity;
        }
    }
    /// <summary>
    /// Help struct that is sent to the processing thread to then be sent to the pishock async from the main thread.
    /// </summary>
    struct ShockerCommand
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

    class SerialShocker : Shocker
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
        public void Shock(int duration, int intensity)
        {

            api._command_queue.Enqueue(new
                ShockerCommand(
                    "operate",
                    info.ShockerId,
                    SerialOperation.SHOCK,
                    duration,
                    intensity
                    )
                );
            //api.Operate(info.ShockerId, SerialOperation.SHOCK, duration, intensity);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Vibrate(int duration, int intensity)
        {
            api._command_queue.Enqueue(new
                ShockerCommand(
                    "operate",
                    info.ShockerId,
                    SerialOperation.VIBRATE,
                    duration,
                    intensity
                    )
                );
            //api.Operate(info.ShockerId, SerialOperation.VIBRATE, duration, intensity);
        }
        public void Beep(int duration)
        {
            api._command_queue.Enqueue(new
                ShockerCommand(
                    "operate",
                    info.ShockerId,
                    SerialOperation.VIBRATE,
                    duration
                    )
                );
            //api.Operate(info.ShockerId, SerialOperation.BEEP, duration);
        }
        /// <summary>
        /// End the currently running operation
        /// </summary>
        public void End()
        {
            api._command_queue.Enqueue(new
                ShockerCommand(
                    "operate",
                    info.ShockerId,
                    SerialOperation.END
                    )
                );
            //api.Operate(info.ShockerId, SerialOperation.END, 0);
        }
        private BasicShockerInfo _Info(int shockerId)
        {
            JObject terminalInfo = api.Info();
            JToken shocker = terminalInfo.SelectToken("shockers")!.First(x => (int)x.SelectToken("id")! == shockerId);
            if(shocker.SelectToken("id") == null | shocker.SelectToken("id")!.Value<int>() != shockerId)
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