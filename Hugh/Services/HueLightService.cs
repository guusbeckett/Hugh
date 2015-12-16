using HughLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web.Http;

namespace Hugh.Services
{
    class HueLightService
    {
        public static async Task<List<Light>> RetrieveLights()
        {
            var cts = new CancellationTokenSource();
            List<Light> retVal = new List<Light>();
            cts.CancelAfter(5000);

            try
            {
                HttpClient client = new HttpClient();
                string ip, username;
                int port;
                SettingsService.RetrieveSettings(out ip, out port, out username);
                var response = await client.GetAsync(new Uri(string.Format("http://{0}:{1}/api/{2}/lights/", ip, port, username))).AsTask(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    cts.Cancel();    
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                //System.Diagnostics.Debug.WriteLine(jsonResponse);

                retVal = ParseJson(jsonResponse);
            }
            catch (Exception)
            {
                cts.Cancel();
            }
            return retVal;
        }
        
        public static List<Light> ParseJson(string json)
        {
            List<Light> lights = new List<Light>();

            JsonObject jsonObject = JsonObject.Parse(json);
            foreach (string key in jsonObject.Keys)
            {
                string lightId = key;

                if (lightId.Equals("error"))
                {
                    System.Diagnostics.Debug.WriteLine(jsonObject[key]);
                    continue;
                }

                JsonObject lightToAdd;
                Light l = null;

                try
                {
                    lightToAdd = jsonObject.GetNamedObject(key, null);

                    JsonObject lightState = lightToAdd.GetNamedObject("state", null);
                    if (lightState != null)
                    {
                        Light.Effect effect = Light.Effect.EFFECT_NONE;
                        if (lightState.GetNamedString("effect", string.Empty).Equals("colorloop"))
                            effect = Light.Effect.EFFECT_COLORLOOP;

                        l = new Light(Convert.ToInt32(lightId), lightToAdd.GetNamedString("name", string.Empty), lightState.GetNamedBoolean("on", false),
                                        Convert.ToInt32(lightState.GetNamedNumber("hue", 0)), Convert.ToInt32(lightState.GetNamedNumber("sat", 255)),
                                        Convert.ToInt32(lightState.GetNamedNumber("bri", 255)), effect, lightState.GetNamedBoolean("reachable", false), false);
                    }
                    else
                        l = new Light(Convert.ToInt32(lightId), string.Format("Hue lamp {0}", lightId), true, 20000, 255, 255, Light.Effect.EFFECT_NONE, true, false);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0} - {1}", e.Message, e.StackTrace));
                    continue;
                }

                if (l != null)
                    lights.Add(l);
            }

            return lights;
        }


        public static async Task<string> LightNameTask(Light light)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);

            try
            {
                HttpClient client = new HttpClient();
                HttpStringContent content = new HttpStringContent(string.Format("{{ \"name\": \"{0}\" }}", light.name), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                string ip, username;
                int port;
                SettingsService.RetrieveSettings(out ip, out port, out username);
                var response = await client.PutAsync(new Uri(string.Format("http://{0}:{1}/api/{2}/lights/{3}", ip, port, username, light.id)), content).AsTask(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(jsonResponse);

                return jsonResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public static async Task<string> LightOnTask(Light light)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);

            try
            {
                HttpClient client = new HttpClient();
                HttpStringContent content = new HttpStringContent(string.Format("{{ \"on\": {0} }}", light.on.ToString().ToLower()), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                string ip, username;
                int port;
                SettingsService.RetrieveSettings(out ip, out port, out username);
                var response = await client.PutAsync(new Uri(string.Format("http://{0}:{1}/api/{2}/lights/{3}/state", ip, port, username, light.id)), content).AsTask(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(jsonResponse);

                return jsonResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public static async Task<string> LightLoopTask(Light light)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);

            try
            {
                HttpClient client = new HttpClient();
                HttpStringContent content = new HttpStringContent(string.Format("{{ \"effect\": \"{0}\" }}", light.effect == Light.Effect.EFFECT_COLORLOOP ? "colorloop" : "none"), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                string ip, username;
                int port;
                SettingsService.RetrieveSettings(out ip, out port, out username);
                var response = await client.PutAsync(new Uri(string.Format("http://{0}:{1}/api/{2}/lights/{3}/state", ip, port, username, light.id)), content).AsTask(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(jsonResponse);

                return jsonResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        public static async Task<string> LightColorTask(Light light)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);

            try
            {
                HttpClient client = new HttpClient();
                HttpStringContent content = new HttpStringContent(string.Format("{{ \"hue\": {0}, \"sat\": {1}, \"bri\": {2} }}", light.hue, light.saturation, light.value), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                string ip, username;
                int port;
                SettingsService.RetrieveSettings(out ip, out port, out username);
                var response = await client.PutAsync(new Uri(string.Format("http://{0}:{1}/api/{2}/lights/{3}/state", ip, port, username, light.id)), content).AsTask(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(jsonResponse);

                return jsonResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }
    }
}
