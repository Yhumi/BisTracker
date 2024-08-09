using BisTracker.BiS.Models;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BisTracker.BiS
{
    internal static class BisSheetReader
    {
        public static async Task<XivGearAppResponse?> XivGearApp(Uri xivGearAppBisUri)
        {
            Svc.Log.Debug($"Fetching bis from: {xivGearAppBisUri.AbsoluteUri}");
            string? xivGearAppSetId = XivGearApp_GetSetId(xivGearAppBisUri);
            Svc.Log.Debug($"Set ID: {xivGearAppSetId}");
            if (xivGearAppSetId == null) { return null; }

            string? jsonResponse = await FetchXivGearAppBisAsync(xivGearAppSetId);
            if (jsonResponse == null) { return null; }

            return JsonConvert.DeserializeObject<XivGearAppResponse>(jsonResponse);
        }   

        private static string? XivGearApp_GetSetId(Uri xivGearAppBisUri)
        {
            //Valid URIs:
            //https://xivgear.app/?page=sl%7C52c4aeab-2dd5-43cc-b02f-809b5649ada5

            //Decoding should end up with sl|{id}
            var queryDictionary = System.Web.HttpUtility.ParseQueryString(xivGearAppBisUri.Query);
            string? setId = queryDictionary.Get("page")?.Split(new[] { '|' }, 2)[1];
            return setId;
        }

        private static async Task<string?> FetchXivGearAppBisAsync(string xivGearAppBisId)
        {
            //API URL: https://api.xivgear.app/shortlink/{id};
            using HttpResponseMessage response = await new HttpClient().GetAsync(new Uri($"https://api.xivgear.app/shortlink/{xivGearAppBisId}"));
            Svc.Log.Debug($"Response recieved: {response.StatusCode}");
            return await response.Content.ReadAsStringAsync();
        }
    }
}
