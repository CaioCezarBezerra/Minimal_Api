using minimal_api.Dominio.Interfaces;
using minimalApi.DTOs;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Infraestrutura.Db;

namespace minimal_api.Dominio.Servicos;

public class AdministradorServico : IAdministradorServico
{
    private readonly DbContextos _contexto;
    public AdministradorServico(DbContextos contexto)
    {
        _contexto = contexto;

    }

    public Administrador? BuscarPorId(int id)
    {
       return _contexto.Administradores.Where(v => v.Id == id).FirstOrDefault();
    }

    public Administrador Incluir(Administrador administrador)
    {
        _contexto.Administradores.Add(administrador);
        _contexto.SaveChanges();

        return administrador;
    }

    public Administrador? Login(LoginDTO loginDTO)
    {
        var adm = _contexto.Administradores.Where(a => a.Email == loginDTO.Email && a.Senha == loginDTO.Senha).FirstOrDefault();
        return adm;
    }

    public List<Administrador> Todos(int? pagina)
    {
       var query = _contexto.Administradores.AsQueryable();

        int itensPorPagina = 10;

        if(pagina != null)
        {
            query = query.Skip(((int)pagina - 1) * itensPorPagina).Take(itensPorPagina);
        }
        return query.ToList();
    }
}
