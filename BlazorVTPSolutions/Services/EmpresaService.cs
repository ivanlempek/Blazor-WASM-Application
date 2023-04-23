using BlazorVTPSolutions.DTOs;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;


namespace BlazorVTPSolutions.Services
{
    public class EmpresaService : IEmpresaService
    {
        private readonly HttpClient _httpClient;
        private readonly ISegmentoService _segmentoService;

        private Empresa _empresaSelecionada; // variável para armazenar a empresa selecionada


        public EmpresaService(HttpClient httpClient, ISegmentoService segmentoService)
        {
            _httpClient = httpClient;
            _segmentoService = segmentoService;
        }

        public async Task<(List<Empresa>, int)> GetEmpresasAsync(int page, int pageSize)
        {
            var response = await _httpClient.GetAsync($"/api/v1/dataset/64420e4bb510ceea93e0b0ed/611ed902fd5915f2ae005dbb?pageSize={pageSize}&apiKey=64420e39b510ceea93e0b0ea&page={page}");
            response.EnsureSuccessStatusCode();
            var jsonResult = await response.Content.ReadAsStringAsync();
            JObject jObject = JObject.Parse(jsonResult);
            var values = jObject.SelectToken("data").ToString();
            
            if (string.IsNullOrWhiteSpace(values))
                return (new List<Empresa>(), 0);

            var segmentos = await _segmentoService.GetSegmentosAsync();    
            var empresas = JsonConvert.DeserializeObject<List<Empresa>>(values);
    
            foreach (var empresa in empresas)
            {
                empresa.SetOidFromIdJObject();

                empresa.SegmentoEmpresa = segmentos.FirstOrDefault(e => e.oid == empresa.SegmentoId);
            }

            var totalEmpresas = jObject.SelectToken("total").Value<int>();
            

            //return empresas;
            return (empresas, totalEmpresas);
        }

        public async Task<Empresa> GetEmpresaAsync(string oid)
        {
            var response = await _httpClient.GetAsync($"/api/v1/dataset/64420e4bb510ceea93e0b0ed/611ed902fd5915f2ae005dbb?apiKey=64420e39b510ceea93e0b0ea&_id={oid}");
            response.EnsureSuccessStatusCode();
            var jsonResult = await response.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(jsonResult);
            var data = jObject["data"].First;
            var empresa = data.ToObject<Empresa>();

            // Extrai o valor do SegmentoId do JToken correspondente e atribui na propriedade SegmentoId da empresa
            empresa.SegmentoId = data["Segmento"]?["$oid"]?.ToString();
            empresa.oid = data["_id"]?["$oid"]?.ToString();
            var segmentos = await _segmentoService.GetSegmentosAsync();
            empresa.SegmentoEmpresa = segmentos.FirstOrDefault(e => e.oid == empresa.SegmentoId);

            // armazenar a empresa selecionada
            _empresaSelecionada = empresa;

            return empresa;
        }

        public async Task<Empresa> CreateEmpresaAsync(Empresa empresa)
        {
            // Verifica se já existe uma empresa com o mesmo nome
            var (empresas, _) = await GetEmpresasAsync(page: 1, pageSize: int.MaxValue);
            if (empresas.Any(e => e.Nome == empresa.Nome))
            {
                throw new Exception("Já existe uma empresa com o mesmo nome.");
            }

            var jsonEmpresa = JsonConvert.SerializeObject(new
            {
                Nome = empresa.Nome,
                Site = empresa.Site,
                Segmento = new JObject
                {
                    ["$oid"] = empresa.SegmentoEmpresa.oid
                }
            });

            var httpContent = new StringContent(jsonEmpresa, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/v1/dataset/64420e4bb510ceea93e0b0ed/611ed902fd5915f2ae005dbb?apiKey=64420e39b510ceea93e0b0ea", httpContent);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var empresaCriada = JsonConvert.DeserializeObject<Empresa>(content);

            return empresaCriada;
        }

        public async Task UpdateEmpresaAsync(Empresa empresa)
        {
            if (_empresaSelecionada != null) 
            {
                var jsonEmpresa = JsonConvert.SerializeObject(new
                {
                    _id = new JObject
                    {
                        ["$oid"] = empresa.oid
                    },
                    Nome = empresa.Nome,
                    Site = empresa.Site,
                    Segmento = new JObject
                    {
                        ["$oid"] = empresa.SegmentoEmpresa.oid
                    }
                });

                var httpContent = new StringContent(jsonEmpresa, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"/api/v1/dataset/64420e4bb510ceea93e0b0ed/611ed902fd5915f2ae005dbb/{empresa.oid}?apiKey=64420e39b510ceea93e0b0ea", httpContent);
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task DeleteEmpresaAsync(string oid)
        {
            await _httpClient.DeleteAsync($"/api/v1/dataset/64420e4bb510ceea93e0b0ed/611ed902fd5915f2ae005dbb/{oid}?apiKey=64420e39b510ceea93e0b0ea");
        }
    }
}
