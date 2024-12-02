using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using MinimalApi.Dominio.Entidades;
using MinimalApi.Infraestrutura.Db;
using minimal_api.Dominio.Servicos;
using System.Reflection;

namespace Test.Domain.Entidades;

[TestClass]
public class AdministradorServicoTest
{



    private DbContextos CriarContextoDeteste()
    {

        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        
        var builder = new ConfigurationBuilder()
            .SetBasePath(path ?? Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();
        
        var Configuration = builder.Build();

        return new DbContextos(Configuration);






    }




    [TestMethod]
    public void TestandoSalvarAdministrador()
    {       
        var context = CriarContextoDeteste();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE Administradores");


        var adm = new Administrador();
        adm.Email = "teste@teste.com";
        adm.Senha = "teste";
        adm.Perfil = "Adm";
        var administradorServico = new AdministradorServico(context);


        //Act
        administradorServico.Incluir(adm);

        //Assert
        Assert.AreEqual(1, administradorServico.Todos(1).Count());

    }
}
