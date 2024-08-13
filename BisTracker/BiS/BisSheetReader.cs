using BisTracker.BiS.Models;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            string? pageQuery = queryDictionary.Get("page");
            if (pageQuery == null) { return null; }
            if (pageQuery.Contains("dont-copy-this-link")) { return null; }
            string? setId = pageQuery.Split(new[] { '|' }, 2)[1];
            return setId;
        }

        private static async Task<string?> FetchXivGearAppBisAsync(string xivGearAppBisId)
        {
            //API URL: https://api.xivgear.app/shortlink/{id};
            using HttpResponseMessage response = await new HttpClient().GetAsync(new Uri($"https://api.xivgear.app/shortlink/{xivGearAppBisId}"));
            Svc.Log.Debug($"Response recieved: {response.StatusCode}");
            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<EtroResponse?> Etro(Uri etroUri)
        {
            Svc.Log.Debug($"Fetching bis from: {etroUri.AbsoluteUri}");
            string? etroSetId = Etro_GetSetId(etroUri);
            Svc.Log.Debug($"Set ID: {etroSetId}");
            if (etroSetId == null || etroSetId == string.Empty) { return null; }

            string? jsonResponse = await FetchEtroBisAsync(etroSetId);
            if (jsonResponse == null) { return null; }

            EtroResponse etroResponse = new EtroResponse(jsonResponse);
            etroResponse.BuildItemsFromEtroResponse(jsonResponse);
            return etroResponse;
        }

        private static string? Etro_GetSetId(Uri etroUri)
        {
            //Valid URIs:
            //https://etro.gg/gearset/99b9d484-2a10-4d76-b9bd-c8574596ecf2

            //Simply check for etro.gg/gearset/ then get everything after the last /
            if (!etroUri.AbsoluteUri.ToLower().Contains("etro.gg/gearset/")) { return null; }

            //Get everything after last /
            int pos = etroUri.AbsoluteUri.LastIndexOf("/") + 1;
            if (pos < 0) { return null; }
            
            return etroUri.AbsoluteUri.Substring(pos, etroUri.AbsoluteUri.Length - pos);
        }

        private static async Task<string?> FetchEtroBisAsync(string etroAppId)
        {
            using HttpResponseMessage response = await new HttpClient().GetAsync(new Uri($"https://etro.gg/api/gearsets/{etroAppId}"));
            Svc.Log.Debug($"Response recieved: {response.StatusCode}");
            return await response.Content.ReadAsStringAsync();
        }
    }
}
