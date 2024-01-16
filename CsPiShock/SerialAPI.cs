﻿namespace CsPiShock;
using System.Diagnostics;
using System.Management;
using System.IO.Ports;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static Shocker;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Microsoft.Win32;
using System.Text;
using System.Data.SqlTypes;
using System.Text.Json;
using Newtonsoft.Json.Linq;

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

    /// <summary>
    /// Main function for debugging
    /// </summary>
    static void Main()
    {
        PiShockSerialApi pishock = new PiShockSerialApi();
        var info = pishock.Info();
        Console.WriteLine(info.SelectToken("shockers"));
    }

    PiShockSerialApi(string? providedPort = null)
    {
        ComPort = GetPort(providedPort);    //Get the com port
        Connect();                          //Initialize the serial port
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
        _serialPort.Open();
        Console.WriteLine("Connected to " + ComPort);
    }
    
    /// <summary>
    /// Sends an operation to the PiShock
    /// </summary>
    /// <param name="shocker_id"></param>
    /// <param name="operation"></param>
    /// <param name="duration"> Duration in ms </param>
    /// <param name="intensity"></param>
    void Operate(int shocker_id, SerialOperation operation, float duration, int? intensity = null)
    {
        if(intensity != null && (0 > intensity | intensity > 100))
        {
            throw new Exception("Intensity must be between 0 and 100");
        }
        if (duration < 0 | duration > (2^32))
        {
            throw new Exception("Duration must be between 0 and 2^32");
        }
        PiCommand command = new PiCommand()
            {
            cmd="operate",
            value = new OperationValues(shocker_id, operation, (int)duration, intensity)
            };
        SendCommand(command);
        
    }
        
    
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
        return JsonConvert.SerializeObject(piCommand, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
    }
    /// <summary>
    /// Gets the basic information of the PiShock
    /// </summary>
    /// <returns> A <c>BasicShockerInfo</c> object with the information of the PiShock</returns>
    /// <exception cref="NotImplementedException"></exception>
    BasicShockerInfo BasicInfo()
    {
        throw new NotImplementedException();
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
                string jsonString = line[TERMINAL_INFO.Length..];
                return line[TERMINAL_INFO.Length..];
            }
            count++;
        }
        throw new TimeoutException("Timed out waiting for info, make sure the given device is indeed a PiShock");

       
    }
    void DecodeInfo(string jsonString)
    {

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

    public void Dispose()
    {
        
    }

    /// <summary>
    /// Help struct to generate JSON strings to send to the pishock
    /// </summary>
    struct PiCommand
    {
        public string cmd { get ; set;}
        public OperationValues? value {get ; set;} = null;
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
        int? id { get; set;}
        string? op { get; set;}
        int? duration {get; set;}
        int? intensity { get; set;}
        /// <summary>
        /// Simple initializer for <c>OperationValues</c>
        /// </summary>
        /// <param name="shockerId"> ID of the shocker </param>
        /// <param name="operation"> Type of operation (Takes a value from <c>SerialOperation</c>)</param>
        /// <param name="opIntensity"></param>
        public OperationValues(int shockerId, SerialOperation operation, int opDuration, int? opIntensity=null)
        {
            string[] operations = {"end", "shock", "vibrate", "beep"};
            id = shockerId;
            op = operations[(int) operation];
            duration = opDuration;
        }
    }
    class SerialShocker : Shocker
    {
        private BasicShockerInfo info;
        private PiShockSerialApi api;

        public SerialShocker(int  shockerId, PiShockSerialApi api)
        {   
            this.info = _Info(shockerId);
            this.api = api;
        }
        public override string ToString()
        {
            return $"Serial shocker {info.ShockerId} ({api.ComPort})";
        }
        public void Shock()
        {

        }
        /// <summary>
        /// 
        /// </summary>
        public void Vibrate(float duration, int intensity)
        {
            api.Operate(info.ShockerId, SerialOperation.VIBRATE, duration, intensity);
        }
        public void Beep()
        {

        }
        /// <summary>
        /// End the currently running operation
        /// </summary>
        public void End()
        {
            api.Operate(info.ShockerId, SerialOperation.END, 0);
        }
        private BasicShockerInfo _Info(int shockerId)
        {
            BasicShockerInfo info = new BasicShockerInfo(api.Info());
            return info;
        }
        public BasicShockerInfo Info()
        {
            return info;
        }
    }
}