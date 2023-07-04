using System;
using System.Diagnostics;
using System.Reflection;

namespace DeveLanCacheUI_Backend.Status
{
    public static class StatusObtainer
    {
        public static StatusModel GetStatus()
        {
            var statusModel = new StatusModel
            {
                ApplicationName = Assembly.GetEntryAssembly().GetName().Name,
                Version = Assembly.GetEntryAssembly().GetName().Version.ToString(),
                UpTime = (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString()
            };

            return statusModel;
        }
    }
}
