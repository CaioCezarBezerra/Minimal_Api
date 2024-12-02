using MinimalApi.Infraestrutura.Db;
using minimalApi.DTOs;
using Microsoft.EntityFrameworkCore;
using minimal_api.Dominio.Interfaces;
using MinimalApi.Dominio.Entidades;
using minimal_api.Dominio.Servicos;
using Microsoft.AspNetCore.Mvc;
using minimal_api.Dominio.ModelViews;
using minimalApi.Dominio.Enuns;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using minimal_api;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authorization;


#region Build
var builder = WebApplication.CreateBuilder(args);

var key = builder.Configuration.GetSection("Jwt").ToString();
if(string.IsNullOrEmpty(key)) key = "123456";


builder.Services.AddAuthentication(option =>{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;


}).AddJwtBearer(option => {
    option.TokenValidationParameters = new TokenValidationParameters{
        ValidateLifetime = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer = false,
        ValidateAudience = false,
    };
});

builder.Services.AddAuthorization();

builder.Services.AddScoped<IAdministradorServico, AdministradorServico>();
builder.Services.AddScoped<IVeiculoServico, VeiculoServico>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme{
        Name = "Autohorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta maneira: Bearer {Seu Token}"

    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        
        {
            new OpenApiSecurityScheme{

                Reference = new OpenApiReference
                {
                 Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
        
    });
});



builder.Services.AddDbContext<DbContextos>(options =>{
    options.UseMySql(
        builder.Configuration.GetConnectionString("mysql"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("mysql"))
    );
});

var app = builder.Build();

#endregion
#region Home 
app.MapGet("/", () => Results.Json(new Home())).AllowAnonymous().WithTags("Home");
#endregion

#region Administradores
string GerarTokenJwt(Administrador administrador){
    if(string.IsNullOrEmpty(key)) return string.Empty;
    var SecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);


    var claims = new List<Claim>()
    {
        new Claim("Email", administrador.Email),
        new Claim("Perfil", administrador.Perfil),
        new Claim(ClaimTypes.Role, administrador.Email),

    };
    var token = new JwtSecurityToken(
        claims: claims,
        expires:  DateTime.Now.AddDays(1),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}



app.MapPost("/administradores/login", ([FromBody]LoginDTO loginDTO, IAdministradorServico administradorServico) =>{
    var adm = administradorServico.Login(loginDTO);
    
    if (adm != null)
    {
        string token = GerarTokenJwt(adm);
        return Results.Ok(new AdministradorLogado {
            Email = adm.Email,
            Perfil = adm.Perfil,
            Token = token
        });
    }  
    else
    return Results.Unauthorized();   
    
}).AllowAnonymous().RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"}).WithTags("Administradores");



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
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"}).WithTags("Administradores");


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
      
    
}).RequireAuthorization().WithTags("Administradores");
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
    
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor"}).WithTags("Veiculo");

app.MapGet("/veiculo", ([FromQuery]int? pagina, IVeiculoServico veiculoServico ) =>{
    var veiculo = veiculoServico.Todos(pagina);

    return Results.Ok(veiculo);
    
}).RequireAuthorization().WithTags("Veiculo");

app.MapGet("/veiculo/{id}", ([FromRoute]int id, IVeiculoServico veiculoServico ) =>{
    var veiculo = veiculoServico.BuscarPorId(id);

    if(veiculo == null) return Results.NotFound();
    return Results.Ok(veiculo);
    
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute { Roles = "Adm, Editor"}).WithTags("Veiculo");


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
    
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"}).WithTags("Veiculo");


app.MapDelete("/veiculo/{id}", ([FromRoute]int id, IVeiculoServico veiculoServico ) =>{
    var veiculo = veiculoServico.BuscarPorId(id);
    if(veiculo == null) return Results.NotFound();

    

    veiculoServico.Apagar(veiculo);

    
    return Results.NoContent();
    
}).RequireAuthorization().RequireAuthorization(new AuthorizeAttribute { Roles = "Adm"}).WithTags("Veiculo");






#endregion


#region  App
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

#endregion
