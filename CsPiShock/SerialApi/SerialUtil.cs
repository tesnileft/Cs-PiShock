using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

public static class SerialUtil
{
    //Possible VID and PID port values for the PiShock
    static List<(int, int)> _usbIds = new List<(int, int)>()
    {
        (0x1A86, 0x7523), //CH340, PiShock Next
        (0x1A86, 0x55D4), //CH9102, PiShock Lite
    };

    public static string GetComPort()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetComPortLinux();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetComPortWin();
        }

        return "COM 3";
    }

    [SupportedOSPlatform("linux")]
    public static string GetComPortLinux()
    {
        List<string> potentialDevices = new List<string>();
        var myRegex=new Regex("usb");
        string ttyPath = "/sys/class/tty";

        string devicesPath = "/sys/bus/usb/devices";
        string[] deviceDirs = Directory.GetDirectories(devicesPath);
        var usbDirs = deviceDirs.Where( f => myRegex.IsMatch(f)).ToList();
        foreach (string f in usbDirs)
        {
            Debug.WriteLine($"Checking device on {f}");
            string idProd, idVend;
            try
            {
                idProd = File.ReadAllText(f + "/idProduct", Encoding.UTF8);
                idVend = File.ReadAllText(f + "/idVendor", Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.WriteLine("No product or vendor ID found; skipping");
                continue;
            }
            
            foreach (var id in _usbIds)
            {
                //For each USB device, check if device ID's match
                int idP = Convert.ToInt32("0x" + idProd.Trim(), 16);
                int idV = Convert.ToInt32("0x" + idVend.Trim(), 16);
                if ((idV, idP) == id)
                {
                    //This device is the one we need
                    
                    string currentDir = f.Split('/').Last();
                    string onePointODir = f + "/" + currentDir + ":1.0";
                    var tryTty = Directory.GetDirectories(onePointODir);
                    for (int i = 0; i < tryTty.Length; i++)
                    {
                        string[] splitted = tryTty[i].Split("/");
                        tryTty[i] = splitted[splitted.Length - 1];
                    }
                    
                    string ttyPattern = "ttyUSB";
                    var ttyRegex = new Regex(ttyPattern);
                    var ttyDirs = tryTty.Where( d => ttyRegex.IsMatch(d)).ToList();
                    potentialDevices.Add(ttyDirs[0]);
                }
                else
                {
                    Debug.WriteLine("No match :(");
                }
                
            }
            
        }

        switch (potentialDevices.Count)
        {
            case 0:
                Console.WriteLine("No devices found!!! :(");
                break;
            case 1:
                //TODO return port
                return "/dev/" + potentialDevices[0];
            default:
                //TODO manage multiple devices
                break;
        }
        
        
        return "/dev/ttyUSB1";
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
            $"Select * From Win32_PnPEntity where PNPDeviceID Like '%{Convert.ToString(_usbIds[0].Item1, 16)}%' AND (PNPDeviceID Like '%{Convert.ToString(_usbIds[0].Item2, 16)}%' OR PNPDeviceID Like '%{Convert.ToString(_usbIds[1].Item2, 16)}%')");

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
            string curCtrl = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\";

            String piShockPort =
                Registry.GetValue(curCtrl + "Enum\\" + piShock["PnpDeviceId"] + "\\Device Parameters", "PortName",
                    "")!.ToString()!;
            return piShockPort;
        }

        else
        {
            throw new NullReferenceException("The PiShock was found but it doesn't exist, this shouldn't happen");
        }
    }
}