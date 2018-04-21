using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Helpers
{
    public class DateTimeHelper
    {
        private static readonly DateTime EPOCH = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime FromEpoch(long epochTime)
        {
            return EPOCH.AddMilliseconds(epochTime);
        }

        public static DateTime? FromEpoch(long? epochTime)
        {
            return epochTime == null ? (DateTime?)null : EPOCH.AddMilliseconds(epochTime.Value);
        }

        public static long ToEpoch(DateTime time)
        {
            return (long)Math.Round(time.Subtract(EPOCH).TotalMilliseconds);
        }

        public static long? ToEpoch(DateTime? time)
        {
            return time == null ? (long?)null : ToEpoch(time.Value);
        }
    }
}
