using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public static class LinuxDeviceManager
{
    public struct DeviceInfo
    {
        public string Bus;
        public string Vendor;
        public string Product;
    }
    
    public static string GetDeviceUsbPort(List<(int, int)> usb_ids)
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
            
            foreach (var id in usb_ids)
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
    





}