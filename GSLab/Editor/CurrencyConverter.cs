using UnityEngine.Networking;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GSLab.BuildValidator 
{
    public static class CurrencyConverter
    {
        private static decimal? usdToVndRate;

        public static async Task<decimal> ConvertUsdToVndAsync(decimal amount)
        {
            if (usdToVndRate == null)
                usdToVndRate = await GetUsdToVndRateAsync();
            return amount * usdToVndRate.Value;
        }
        
        public static async Task<decimal> GetUsdToVndRateAsync()
        {
            var req = UnityWebRequest.Get("https://api.exchangerate-api.com/v4/latest/USD");
            await req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(req.error);
            var json = req.downloadHandler.text;
            var data = JsonUtility.FromJson<ExchangeRateResponse>(json);
            return data.rates["VND"];
        }
        
        [Serializable]
        public class ExchangeRateResponse
        {
            public Dictionary<string, decimal> rates;
        }
    }
}



