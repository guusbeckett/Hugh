using System;
using Windows.Storage;

namespace Hugh.Services
{
    public class SettingsService
    {
        public static ApplicationData APP_DATA = ApplicationData.Current;
        public static ApplicationDataContainer LOCAL_SETTINGS = APP_DATA.LocalSettings;

        public static void FirstTimeSetup()
        {
            string tmpUsername = Hugh.Services.SettingsService.LOCAL_SETTINGS.Values["user"] as string;

            if (string.IsNullOrEmpty(tmpUsername))
            {
                var responseBridgeUsername = Hugh.Services.HueLightService.RetrieveUsername();
                Hugh.Services.SettingsService.LOCAL_SETTINGS.Values["user"] = responseBridgeUsername;
            }
        }

        public static void RetrieveSettings(out string ip, out int port, out string username)
        {
            string tmpIp = LOCAL_SETTINGS.Values["ip"] as string;
            int tmpPort = Convert.ToInt32(LOCAL_SETTINGS.Values["port"]);
            string tmpUsername = LOCAL_SETTINGS.Values["user"] as string;

            if (string.IsNullOrEmpty(tmpIp))
            {
                tmpIp = "";
            }
            if (tmpPort == 0)
            {
                tmpPort = 8000;
            }
            if (string.IsNullOrEmpty(tmpUsername))
            {
                tmpUsername = "";
            }
            ip = tmpIp;
            port = tmpPort;
            username = tmpUsername;
        }

        public static void SetSettings(string ip, int port, string username)
        {
            LOCAL_SETTINGS.Values["ip"] = ip;
            LOCAL_SETTINGS.Values["port"] = port;
            LOCAL_SETTINGS.Values["user"] = username;
        }
    }
}
