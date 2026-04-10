using System;

namespace Unity.GameDataCollection.Editor
{
    static class DateTimeUtils
    {
        public static string FormatCommitDateTimeNow()
        {
            return DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        }
    }
}

