using System;

namespace CRMSystem
{
    public static class DateTimeExtensions
    {
        // Bangladesh Standard Time: UTC+6
        public static readonly TimeZoneInfo BangladeshTimeZone = GetBangladeshTimeZone();

        private static TimeZoneInfo GetBangladeshTimeZone()
        {
            try
            {
                // windows
                return TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
            }
            catch
            {
                try
                {
                    //linax
                    return TimeZoneInfo.FindSystemTimeZoneById("Asia/Dhaka");
                }
                catch
                {
                    return TimeZoneInfo.CreateCustomTimeZone(
                        "Bangladesh Standard Time",
                        TimeSpan.FromHours(6),
                        "Bangladesh Standard Time",
                        "Bangladesh Standard Time");
                }
            }
        }

        /// <summary>
        /// Converts a UTC DateTime to Bangladesh Standard Time (UTC+6).
        /// </summary>
        public static DateTime ToBangladeshTime(this DateTime dateTime)
        {
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue)
            {
                return dateTime;
            }

            DateTime utc = dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            };

            return TimeZoneInfo.ConvertTimeFromUtc(utc, BangladeshTimeZone);
        }

        /// <summary>
        /// Converts a nullable UTC DateTime to Bangladesh Standard Time (UTC+6).
        /// </summary>
        public static DateTime? ToBangladeshTime(this DateTime? dateTime)
        {
            if (!dateTime.HasValue)
            {
                return null;
            }

            return dateTime.Value.ToBangladeshTime();
        }
    }
}
