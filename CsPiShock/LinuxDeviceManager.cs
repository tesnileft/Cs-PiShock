using System.Security.Cryptography;
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
        var myRegex=new Regex(@"ttyUSB");

        string[] ttyDirs = Directory.GetDirectories("/sys/class/tty");
        var usbDirs = ttyDirs.Where( f => myRegex.IsMatch(f)).ToList();
        foreach (string f in usbDirs)
        {
            foreach (var id in usb_ids)
            {
                
            }
            
        }
        
        
        return "todo";
    }
    





}