FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["InterviewSimulator.API/InterviewSimulator.API.csproj", "InterviewSimulator.API/"]
COPY ["CodeExecution.Infrastructure.Implementation.DataAccess/CodeExecution.Infrastructure.Implementation.DataAccess.csproj", "CodeExecution.Infrastructure.Implementation.DataAccess/"]
COPY ["CodeExecution.Domain/CodeExecution.Domain.csproj", "CodeExecution.Domain/"]
COPY ["CodeExecution.Infrastructure.Interfaces.DataAccess/CodeExecution.Infrastructure.Interfaces.DataAccess.csproj", "CodeExecution.Infrastructure.Interfaces.DataAccess/"]
COPY ["CodeExecution.Infrastructure.Workers/CodeExecution.Infrastructure.Workers.csproj", "CodeExecution.Infrastructure.Workers/"]
COPY ["CodeExecution.IntegrationEvents/CodeExecution.IntegrationEvents.csproj", "CodeExecution.IntegrationEvents/"]
COPY ["CodeExecution.UseCases/CodeExecution.UseCases.csproj", "CodeExecution.UseCases/"]
COPY ["CodeExecution.Infrastructure.Interfaces.CodeExecution/CodeExecution.Infrastructure.Interfaces.CodeExecution.csproj", "CodeExecution.Infrastructure.Interfaces.CodeExecution/"]
COPY ["Framework.Domain/Framework.Domain.csproj", "Framework.Domain/"]
COPY ["CodeExecution.Presentation/CodeExecution.Presentation.csproj", "CodeExecution.Presentation/"]
COPY ["CodeExecution.Infrastructure.Implementation.CodeExecution.Docker/CodeExecution.Infrastructure.Implementation.CodeExecution.Docker.csproj", "CodeExecution.Infrastructure.Implementation.CodeExecution.Docker/"]
COPY ["QuestionBank.Infrastructure.Implementation.DataAccess/QuestionBank.Infrastructure.Implementation.DataAccess.csproj", "QuestionBank.Infrastructure.Implementation.DataAccess/"]
COPY ["QuestionBank.Domain/QuestionBank.Domain.csproj", "QuestionBank.Domain/"]
COPY ["QuestionBank.Infrastructure.Interfaces.DataAccess/QuestionBank.Infrastructure.Interfaces.DataAccess.csproj", "QuestionBank.Infrastructure.Interfaces.DataAccess/"]
RUN dotnet restore "InterviewSimulator.API/InterviewSimulator.API.csproj"
COPY . .
WORKDIR "/src/InterviewSimulator.API"
RUN dotnet build "./InterviewSimulator.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./InterviewSimulator.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InterviewSimulator.API.dll"]
