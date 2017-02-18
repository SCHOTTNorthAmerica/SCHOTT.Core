using System;

namespace SCHOTT.Core.Extensions
{
    /// <summary>
    /// A class to provide date extensions.
    /// </summary>
    public static class Date
    {
        /// <summary>
        /// Returns the string representation of the current date in W##/#### format. 
        /// Utilizes the German method for conversion to match the SCHOTT calendar.
        /// </summary>
        /// <param name="dateToConvert">The date to convert in DateTime format.</param>
        /// <returns>The string representing the Date in W##/#### format.</returns>
        public static string WeekYear(this System.DateTime dateToConvert)
        {
            var cul = System.Globalization.CultureInfo.CurrentCulture;

            var weekNum = cul.Calendar.GetWeekOfYear(
                dateToConvert,
                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                System.DayOfWeek.Monday);

            var year = weekNum >= 52 && dateToConvert.Month == 1 ? dateToConvert.Year - 1 : dateToConvert.Year;

            return "W" + weekNum.ToString("00") + "/" + year.ToString("0000");
        }

        /// <summary>
        /// Rounds the current date to the nearest supplied TimeSpan Interval.
        /// </summary>
        /// <param name="dateToRound">The Date to round to the nearest Interval.</param>
        /// <param name="interval">The Interval used for the rounding algorithm.</param>
        /// <returns></returns>
        public static System.DateTime Round(this System.DateTime dateToRound, System.TimeSpan interval)
        {
            var ticks = (dateToRound.Ticks + interval.Ticks / 2 + 1) / interval.Ticks;
            return new System.DateTime(ticks * interval.Ticks);
        }

        /// <summary>
        /// Rounds the current date down to the nearest supplied TimeSpan Interval.
        /// </summary>
        /// <param name="dateToRoundDown">The Date to round down to the nearest Interval.</param>
        /// <param name="interval">The Interval used for the rounding algorithm.</param>
        /// <returns></returns>
        public static System.DateTime Floor(this System.DateTime dateToRoundDown, System.TimeSpan interval)
        {
            var ticks = dateToRoundDown.Ticks / interval.Ticks;
            return new System.DateTime(ticks * interval.Ticks);
        }

        /// <summary>
        /// Rounds the current date up to the nearest supplied TimeSpan Interval.
        /// </summary>
        /// <param name="dateToRoundUp">The Date to round up to the nearest Interval.</param>
        /// <param name="interval">The Interval used for the rounding algorithm.</param>
        /// <returns></returns>
        public static System.DateTime Ceil(this System.DateTime dateToRoundUp, System.TimeSpan interval)
        {
            var ticks = (dateToRoundUp.Ticks + interval.Ticks - 1) / interval.Ticks;
            return new System.DateTime(ticks * interval.Ticks);
        }

        /// <summary>
        /// converts a timespan into a string representation
        /// </summary>
        /// <param name="t">timespan variable</param>
        /// <param name="displayMilliseconds">true = display milliseconds</param>
        /// <returns>string representation of timespan</returns>
        internal static string ConvertTime(TimeSpan t, bool displayMilliseconds)
        {
            var first = true;
            var timeOutput = "";
            if (t.Days > 0)
            {
                first = false;
                timeOutput += $"{t.Days:D2}d";
            }
            if (t.Hours > 0)
            {
                if (!first) { timeOutput += ":"; }
                else { first = false; }

                timeOutput += $"{t.Hours:D2}h";
            }
            if (t.Minutes > 0)
            {
                if (!first) { timeOutput += ":"; }
                else { first = false; }

                timeOutput += $"{t.Minutes:D2}m";
            }
            if (t.Seconds > 0)
            {
                if (!first) { timeOutput += ":"; }
                else { first = false; }

                timeOutput += $"{t.Seconds:D2}s";
            }
            if (t.Milliseconds > 0 && displayMilliseconds)
            {
                if (!first) { timeOutput += ":"; }
                else { first = false; }

                timeOutput += $"{t.Milliseconds:D3}ms";
            }

            return timeOutput;
        }

    }

}
