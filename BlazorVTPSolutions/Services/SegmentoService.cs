﻿using BlazorVTPSolutions.DTOs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;

namespace BlazorVTPSolutions.Services
{
    public interface ISegmentoService
    {
        Task<List<Segmento>> GetSegmentosAsync();
    }
    public class SegmentoService : ISegmentoService
    {
        private readonly HttpClient _httpClient;
        private List<Segmento>? Segmentos { get; set; }

        public SegmentoService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Segmento>> GetSegmentosAsync()
        {
            if (Segmentos != null && Segmentos.Count > 0)
            {
                return Segmentos;
            }

            var jsonResult = await FetchDataFromApiAsync();
            var values = ParseResultFromApi(jsonResult); 

            if (string.IsNullOrWhiteSpace(values))
                return new List<Segmento>();

            // armazena a lista de segmentos na propriedade Segmentos
            var segmentos = GetSegmentosFromJson(values);
            Segmentos = segmentos;

            return segmentos; 
        }

        private async Task<string> FetchDataFromApiAsync()
        {
            var response = await _httpClient.GetAsync("/api/v1/dataset/64420e4bb510ceea93e0b0ed/611edbd7fd5915f2ae005dc2?apiKey=64420e39b510ceea93e0b0ea&pageSize=30");

            return await response.Content.ReadAsStringAsync();
        }

        private string ParseResultFromApi(string jsonResult)
        {
            JObject jObject = JObject.Parse(jsonResult);
            var data = jObject["data"];

            if (data == null)
            {
                throw new ArgumentException("O JSON fornecido não contém o campo 'data'.");
            }

            return data.ToString();
        }

        private List<Segmento> GetSegmentosFromJson(string values)
        {
            var segmentos = JsonConvert.DeserializeObject<List<Segmento>>(values);

            foreach (var segmento in segmentos)
            {
                segmento.SetOidFromIdJObject();
            }

            return segmentos;
        }
    }
}
