using MinimalApi.Infraestrutura.Db;
using minimalApi.DTOs;
using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.Interfaces;
using MinimalApi.Dominio.Entidades;
using minimal_api.Dominio.Servicos;
using Microsoft.AspNetCore.Mvc;
using minimal_api.Dominio.ModelViews;
using minimalApi.Dominio.Enuns;


#region Build
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<DbContextos>(options =>{
    options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
    );
});

var app = builder.Build();

#endregion
#region Home 
app.MapGet("/", () => Results.Json(new Home())).WithTags("Home");
#endregion

#region Administradores
app.MapPost("/administradores/login", ([FromBody]LoginDTO loginDTO, IAdministradorServico administradorServico) =>{
    if (administradorServico.Login(loginDTO) != null)
        return Results.Ok("Login Com Sucesso");  
    else
    return Results.Unauthorized();   
    
}).WithTags("Administradores");



app.MapGet("/administradores", ([FromQuery]int? pagina, IAdministradorServico administradorServico) =>{

    var adms = new List<AdministradorModelView>();
    var administradores = administradorServico.Todos(pagina);
    foreach(var adm in administradores)
    {
        adms.Add(new AdministradorModelView{
            Id = adm.Id,
            Email = adm.Email,
            Perfil = adm.Perfil

        });
    }

    return Results.Ok(administradorServico.Todos(pagina));  
}).WithTags("Administradores");


app.MapGet("/Administradores/{id}", ([FromRoute]int id, IAdministradorServico administradorServico ) =>{
    var administrador = administradorServico.BuscarPorId(id);
    if(administrador == null) return Results.NotFound();
    return Results.Ok(administrador);

}).WithTags("Administradores");


app.MapPost("/administradores", ([FromBody]AdministradorDTO administradorDTO, IAdministradorServico administradorServico) =>{
   var validacao = new ErrosDeValidacao{
    Mensagens = new List<string>()
   };
   if(string.IsNullOrEmpty(administradorDTO.Email))
        validacao.Mensagens.Add("Email não poser ser Vazio!");
    if(string.IsNullOrEmpty(administradorDTO.Senha))
        validacao.Mensagens.Add("Senha não poser ser Vazia!");
    if(administradorDTO.Perfil == null)
        validacao.Mensagens.Add("Perfil não poser ser Vazio!");

    if(validacao.Mensagens.Count > 0)
      return Results.BadRequest(validacao);
   
   
   
    var veiculo = new Administrador{
        Email = administradorDTO.Email,
        Senha = administradorDTO.Senha,
        Perfil = administradorDTO.Perfil.ToString() ?? Perfil.Editor.ToString()
    };
    administradorServico.Incluir(veiculo);

    return Results.Created($"/administrador/{veiculo.Id}", veiculo);
      
    
}).WithTags("Administradores");
#endregion


#region Veiculos
ErrosDeValidacao validaDTO(VeiculoDTO veiculoDTO)
{
    var validacao = new ErrosDeValidacao();

   if(string.IsNullOrEmpty(veiculoDTO.Nome))
      validacao.Mensagens.Add("O nome nao pode ser Vazio");

    if(string.IsNullOrEmpty(veiculoDTO.Marca))
      validacao.Mensagens.Add("A Marca não pode ficar em Branco");

    if(veiculoDTO.Ano < 1950)
      validacao.Mensagens.Add("Veiculo muito antigo, aceito somente ano superior");

    return validacao;
}

app.MapPost("/veiculo", ([FromBody]VeiculoDTO veiculoDTO, IVeiculoServico veiculoServico) =>{
    
    var validacao = validaDTO(veiculoDTO);
    if(validacao.Mensagens.Count > 0)
      return Results.BadRequest(validacao);
   
   
   
    var veiculo = new Veiculo{
        Nome = veiculoDTO.Nome,
        Marca = veiculoDTO.Marca,
        Ano = veiculoDTO.Ano
    };
    
    veiculoServico.Incluir(veiculo);

    return Results.Created($"/veiculo/{veiculo.Id}", veiculo);
    
}).WithTags("Veiculo");

app.MapGet("/veiculo", ([FromQuery]int? pagina, IVeiculoServico veiculoServico ) =>{
    var veiculo = veiculoServico.Todos(pagina);

    return Results.Ok(veiculo);
    
}).WithTags("Veiculo");

app.MapGet("/veiculo/{id}", ([FromRoute]int id, IVeiculoServico veiculoServico ) =>{
    var veiculo = veiculoServico.BuscarPorId(id);

    if(veiculo == null) return Results.NotFound();
    return Results.Ok(veiculo);
    
}).WithTags("Veiculo");


app.MapPut("/veiculo/{id}", ([FromRoute]int id, VeiculoDTO veiculoDTO ,IVeiculoServico veiculoServico ) =>{
    var veiculo = veiculoServico.BuscarPorId(id);
    if(veiculo == null) return Results.NotFound();
    
    
    var validacao = validaDTO(veiculoDTO);
    if(validacao.Mensagens.Count > 0)
    return Results.BadRequest(validacao);

    veiculo.Nome = veiculoDTO.Nome;
    veiculo.Marca = veiculoDTO.Marca;
    veiculo.Ano = veiculoDTO.Ano;

    veiculoServico.Atualizar(veiculo);

    
    return Results.Ok(veiculo);
    
}).WithTags("Veiculo");


app.MapDelete("/veiculo/{id}", ([FromRoute]int id, IVeiculoServico veiculoServico ) =>{
    var veiculo = veiculoServico.BuscarPorId(id);
    if(veiculo == null) return Results.NotFound();

    

    veiculoServico.Apagar(veiculo);

    
    return Results.NoContent();
    
}).WithTags("Veiculo");






#endregion


#region  App
app.UseSwagger();
app.UseSwaggerUI();

app.Run();

#endregion
