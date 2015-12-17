using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Web.Http;

namespace Hugh.Services
{
    public class SettingsService
    {
        public static ApplicationData APP_DATA = ApplicationData.Current;
        public static ApplicationDataContainer LOCAL_SETTINGS = APP_DATA.LocalSettings;

        public static void FirstTimeSetup()
        {
            string tmpUsername = LOCAL_SETTINGS.Values["user"] as string;

            if (string.IsNullOrEmpty(tmpUsername))
            {
                var responseBridgeUsername = RetrieveUsername();
                LOCAL_SETTINGS.Values["user"] = responseBridgeUsername;
            }
        }

        public static async Task<string> RetrieveUsername()
        {
            System.Diagnostics.Debug.WriteLine("Retrieving username");
            Boolean hasGotUsername = false;
            string jsonResponse = "";
            string usernameRetrieved = "";
            while (!hasGotUsername)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    HttpStringContent content = new HttpStringContent("{\"devicetype\":\"HueApp#ComfyCrew\"}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                    string ip, username;
                    int port;
                    SettingsService.RetrieveSettings(out ip, out port, out username);
                    var response = await client.PostAsync(new Uri(string.Format("http://{0}:{1}/api/", ip, port)), content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return string.Empty;
                    }

                    jsonResponse = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(jsonResponse);

                    JsonArray jsonArray = JsonArray.Parse(jsonResponse);
                    ICollection<string> keys = jsonArray.First().GetObject().Keys;
                    if (keys.Contains("error"))
                    {
                        Hugh.Views_Viewmodels.MainPage.ShowErrorDialogue();
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        hasGotUsername = true;
                        JsonObject succesObject = jsonArray.First().GetObject();

                        System.Diagnostics.Debug.WriteLine(succesObject.Values.First().GetObject().Values.First().GetString());
                        usernameRetrieved = succesObject.Values.First().GetObject().Values.First().GetString();
                    }

                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
            return usernameRetrieved;
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
