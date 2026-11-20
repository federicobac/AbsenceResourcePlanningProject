using API.Extensions;
using API.Utils;
using Infra;
using LinqToDB;
using LinqToDB.Mapping;
using Service;
using Service.Security;

var builder = WebApplication.CreateBuilder(args);

var mappingSchema = new MappingSchema();

mappingSchema.SetConverter<DateTime, TimeOnly>(dt => TimeOnly.FromDateTime(dt));

var options = new DataOptions<MyDatabaseConnection>(
    new DataOptions().UseSQLite("Data Source=../Infra/db.db").UseMappingSchema(mappingSchema));

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

jwtSettings.Validate();

builder.Services.AddJwtAuthentication(jwtSettings);
builder.Services.AddAuthorization();

builder.Services.AddScoped<MyDatabaseConnection>(_ => new MyDatabaseConnection(options));
builder.Services.AddScoped<Seeder>();

builder.Services.AddServiceLayer(jwtSettings);

builder.Services.AddControllers();
builder.Services.AddOpenApiDocumentation();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<MyExceptionHandler>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetService<Seeder>();
    seeder.Seed();
}

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseOpenApi();
app.UseSwaggerUi();
app.Run();