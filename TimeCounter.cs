using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeKeeper
{
    internal class TimeCounter
    {
        public DateTime Day { get; set; }
        public int Minutes { get; set; }
        public int DefaultMinutes { get; set; }
        public DateTime LastLogOn { get; set; }

        public override string ToString()
        {
            return $"TimeCounter.Day = {this.Day}, TimeCounter.Minutes = {this.Minutes}, TimeCounter.DefaultMinutes = {this.DefaultMinutes}, TimeCounter.LastLogOn = {this.LastLogOn}";
        }
    }
}
