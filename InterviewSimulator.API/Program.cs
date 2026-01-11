using CodeExecution.Infrastructure.Implementation.CodeExecution;
using CodeExecution.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("runtimes.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCodeExecutionDocker(builder.Configuration);
builder.Services.AddCodeExecutionModuleUseCases();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();