using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsPiShock
{
    public abstract class ApiBase
    {
        ConcurrentQueue<PiCommand> _command_queue = new ConcurrentQueue<PiCommand>();
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        /// <summary>
        /// Enum for the serial operations
        /// 
        /// <para>SHOCK:    Send a shock to the shocker.</para>
        /// <para>VIBRATE:  Send a vibration to the shocker</para>
        /// <para>BEEP:     Send a beep to the shocker.</para>
        /// <para>END:      End the current operation.</para>
        /// </summary>
        public enum SerialOperation
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

        /// <summary>
        /// Help struct to generate JSON strings to send to the pishock
        /// </summary>
        internal struct PiCommand
        {
            public string cmd { get; set; }
            public dynamic? value { get; set; }
            public PiCommand(string command, dynamic? values = null)
            {
                cmd = command;
                value = values;
            }
        }
        /// <summary>
        /// Help struct to generate JSON strings to send to the PiShock
        /// </summary>
        internal struct OperationValues
        {
            public int? id { get; set; }
            public string? op { get; set; }
            public int? duration { get; set; }
            public int? intensity { get; set; }
            /// <summary>
            /// Simple initializer for <c>OperationValues</c>
            /// </summary>
            /// <param name="shockerId"> ID of the shocker </param>
            /// <param name="operation"> Type of operation (Takes a value from <c>SerialOperation</c>)</param>
            /// <param name="opDuration"></param>
            /// <param name="opIntensity"></param>
            public OperationValues(int shockerId, SerialOperation operation, int? opDuration = null, int? opIntensity = null)
            {
                string[] operations = { "end", "shock", "vibrate", "beep" };
                id = shockerId;
                op = operations[(int)operation];
                duration = opDuration;
                intensity = opIntensity;
            }
        }
        internal struct NetworkValues
        {
            public string ssid { get; set; }
            public string? password { get; set; }
            public NetworkValues(string ssid, string? pass = null)
            {
                this.ssid = ssid;
                password = pass;
            }
        }

    }
}
