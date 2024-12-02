using minimalApi.DTOs;
using MinimalApi.Dominio.Entidades;

namespace minimal_api.Dominio.Interfaces;

public interface IAdministradorServico
{
    Administrador? Login(LoginDTO loginDTO);

    Administrador Incluir(Administrador administrador);
    Administrador? BuscarPorId (int id);
    List<Administrador> Todos (int? pagina);
}
