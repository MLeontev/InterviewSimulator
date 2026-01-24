using CodeExecution.Infrastructure.Implementation.CodeExecution;
using CodeExecution.Infrastructure.Implementation.DataAccess;
using CodeExecution.Infrastructure.Workers;
using CodeExecution.UseCases;
using Interview.Infrastructure.Implementation.DataAccess;
using InterviewSimulator.API.Extensions;
using MassTransit;
using QuestionBank.Infrastructure.Implementation.DataAccess;
using Users.Infrastructure.Implementation.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.AddAppConfiguration();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CodeExecution module
builder.Services.AddCodeExecutionDocker(builder.Configuration);
builder.Services.AddCodeExecutionModuleUseCases();
builder.Services.AddCodeExecutionDataAccess(builder.Configuration);
builder.Services.AddCodeExecutionWorkers();

// QuestionBank module
builder.Services.AddQuestionBankDataAccess(builder.Configuration);

// Interview module
builder.Services.AddInterviewDataAccess(builder.Configuration);

// Users module
builder.Services.AddUsersDataAccess(builder.Configuration);

builder.Services.AddMassTransit(configure =>
{
    configure.SetKebabCaseEndpointNameFormatter();

    configure.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapOpenApi();

app.ApplyMigrations();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();