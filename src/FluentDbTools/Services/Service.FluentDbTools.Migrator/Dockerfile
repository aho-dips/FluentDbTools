FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src

COPY . .
WORKDIR /src/DbTools/Services/Service.FluentDbTools.Migrator
RUN dotnet publish Service.FluentDbTools.Migrator.csproj -c Release -o /app

FROM microsoft/dotnet:2.1-aspnetcore-runtime AS final

ENV TZ=Europe/Oslo
ENV RUNNING_IN_CONTAINER=true
ENV DATABASE=postgres

WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Service.FluentDbTools.Migrator.dll"]
