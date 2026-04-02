using System.Text.Json.Serialization;
using CodeExecution.Controllers;
using CodeExecution.Infrastructure.Implementation.CodeExecution;
using CodeExecution.Infrastructure.Implementation.DataAccess;
using CodeExecution.Infrastructure.Workers;
using CodeExecution.UseCases;
using Interview.Infrastructure.Implementation.DataAccess;
using Interview.UseCases;
using InterviewSimulator.API.Extensions;
using Interview.Presentation;
using MassTransit;
using QuestionBank.Infrastructure.Implementation.DataAccess;
using QuestionBank.Infrastructure.Implementation.DataAccess.Seeding;
using QuestionBank.ModuleContract.Implementation;
using QuestionBank.UseCases;
using Users.Infrastructure.Implementation.DataAccess;
using Users.Infrastructure.Implementation.Identity.Keycloak;
using Users.UseCases;

var builder = WebApplication.CreateBuilder(args);

builder.AddAppConfiguration();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuth(builder.Configuration);

// CodeExecution module
builder.Services.AddCodeExecutionDocker(builder.Configuration);
builder.Services.AddCodeExecutionModuleUseCases();
builder.Services.AddCodeExecutionDataAccess(builder.Configuration);
builder.Services.AddCodeExecutionWorkers();

// QuestionBank module
builder.Services.AddQuestionBankDataAccess(builder.Configuration);
builder.Services.AddQuestionBankModuleUseCases();
builder.Services.AddQuestionBankModuleApi();
builder.Services.AddQuestionBankSeeding();

// Interview module
builder.Services.AddInterviewDataAccess(builder.Configuration);
builder.Services.AddInterviewModuleUseCases();

// Users module
builder.Services.AddUsersDataAccess(builder.Configuration);
builder.Services.AddKeycloakIdentity(builder.Configuration);
builder.Services.AddUsersModuleUseCases();

builder.Services.AddMassTransit(configure =>
{
    configure.SetKebabCaseEndpointNameFormatter();
    configure.AddConsumer<CodeSubmissionCreatedConsumer>();
    configure.AddConsumer<CodeSubmissionCompletedConsumer>();

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

using (var scope = app.Services.CreateScope())
{
    var seedRunner = scope.ServiceProvider.GetRequiredService<QuestionBankSeedRunner>();
    await seedRunner.RunAsync();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
