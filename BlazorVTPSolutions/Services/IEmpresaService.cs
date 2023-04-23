using BlazorVTPSolutions.DTOs;
using MongoDB.Bson;

namespace BlazorVTPSolutions.Services
{
    public interface IEmpresaService
    {
        Task<(List<Empresa>, int)> GetEmpresasAsync(int page, int pageSize);
        Task<Empresa> GetEmpresaAsync(string oid);
        Task<Empresa> CreateEmpresaAsync(Empresa empresa);
        Task UpdateEmpresaAsync(Empresa empresa);
        Task DeleteEmpresaAsync(string oid);
    }
}
