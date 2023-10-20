using BlazorVTPSolutions.DTOs;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;


namespace BlazorVTPSolutions.Services
{
    public class EmpresaService : IEmpresaService
    {
        private readonly HttpClient _httpClient;
        private readonly ISegmentoService _segmentoService;
        private Empresa? _empresaSelecionada; // variável para armazenar a empresa selecionada

        public EmpresaService(HttpClient httpClient, ISegmentoService segmentoService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _segmentoService = segmentoService ?? throw new ArgumentNullException(nameof(segmentoService));
        }

        public async Task<(List<Empresa>, int)> GetEmpresasAsync(int page, int pageSize)
        {
            try
            {
                var jsonResult = await FetchDataFromApiAsync(page, pageSize); 

                if (string.IsNullOrWhiteSpace(jsonResult))
                    return (new List<Empresa>(), 0);

                var empresas = GetEmpresasFromJson(jsonResult);
                await AssignSegmentosToEmpresas(empresas);

                var totalEmpresas = GetTotalEmpresasFromJson(jsonResult);

                return (empresas, totalEmpresas);

            }
            catch (Exception ex) 
            {
                throw new Exception($"Ocorreu algum erro ao consultar a API \nErro: { ex.Message }");
            }
            
        }

        public async Task<string> FetchDataFromApiAsync(int page, int pageSize)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/v1/dataset/64420e4bb510ceea93e0b0ed/611ed902fd5915f2ae005dbb?pageSize={pageSize}&apiKey=64420e39b510ceea93e0b0ea&page={page}");

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Ocorreu algum erro ao consultar a API \nErro: {ex.Message}"); 
            }
        }

        public List<Empresa> GetEmpresasFromJson(string jsonResult)
        {
            JObject respostaJson = JObject.Parse(jsonResult);
            var values = respostaJson.SelectToken("data");

            if (values == null)
            {
                throw new ArgumentException("O JSON fornecido não contém o campo 'data'.");
            }

            var empresas = JsonConvert.DeserializeObject<List<Empresa>>(values.ToString()) ?? new List<Empresa>();

            foreach (var empresa in empresas)
            {
                empresa.SetOidFromIdJObject();
            }

            return empresas;
        }

        public async Task AssignSegmentosToEmpresas(List<Empresa> empresas)
        {
            var segmentos = await _segmentoService.GetSegmentosAsync();

            foreach (var empresa in empresas)
            {
                empresa.SegmentoEmpresa = segmentos.FirstOrDefault(e => e.oid == empresa.SegmentoId) ?? new Segmento();
            }
        }

        public int GetTotalEmpresasFromJson(string jsonResult)
        {
            JObject respostaJson = JObject.Parse(jsonResult);
            var totalEmpresas = respostaJson.SelectToken("total");

            if (totalEmpresas == null)
            {
                throw new ArgumentException("O JSON fornecido não contém o campo 'total'.");
            }

            return totalEmpresas.Value<int>();
        }

        public async Task<Empresa> GetEmpresaAsync(string oid)
        {
            try
            {
                var jsonResult = await FetchEmpresaDataFromApiAsync(oid);

                var data = GetEmpresaFromJson(jsonResult);
                var empresa = data.ToObject<Empresa>();

                AssignOidSegmentoIdToEmpresa(empresa, jsonResult);

                await AssignSegmentoToEmpresa(empresa);

                _empresaSelecionada = empresa;

                return empresa;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ocorreu algum erro ao consultar a API \nErro:  { ex.Message }");
            }
        }

        private async Task<string> FetchEmpresaDataFromApiAsync(string oid)
        {
            var response = await _httpClient.GetAsync($"/api/v1/dataset/64420e4bb510ceea93e0b0ed/611ed902fd5915f2ae005dbb?apiKey=64420e39b510ceea93e0b0ea&_id={oid}");
            return await response.Content.ReadAsStringAsync();
        }

        private JToken GetDataFromJsonResult(string jsonResult)
        {
            var jObject = JObject.Parse(jsonResult);
            var data = jObject["data"];

            if (data == null)
            {
                throw new ArgumentException("Erro ao realizar o Parse desse json.");
            }

            return data;
        }

        private JToken GetEmpresaFromJson(string jsonResult)
        {

            var data = GetDataFromJsonResult(jsonResult);

            if (data?.First == null)
            {
                throw new ArgumentException("O JSON fornecido não contém o campo 'data'.");
            }

            return data.First;
        }

        private async Task AssignSegmentoToEmpresa(Empresa empresa)
        {
            var segmentos = await _segmentoService.GetSegmentosAsync();
            empresa.SegmentoEmpresa = segmentos.FirstOrDefault(e => e.oid == empresa.SegmentoId) ?? new Segmento();
        }

        private void AssignOidSegmentoIdToEmpresa(Empresa empresa, string jsonResult)
        {
            var data = GetDataFromJsonResult(jsonResult);
            var dataArray = data.First ?? throw new ArgumentException("Nenhum item encontrado no Array."); 

            empresa.SegmentoId = dataArray["Segmento"]?["$oid"]?.ToString() ?? "SegmentoId não encontrado";
            empresa.oid = dataArray["_id"]?["$oid"]?.ToString() ?? "OId da Empresa não encontrado";
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
