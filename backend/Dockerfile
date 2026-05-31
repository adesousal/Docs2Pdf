FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ./Docs2Pdf.Api.csproj ./
RUN dotnet restore ./Docs2Pdf.Api.csproj

COPY . ./
RUN dotnet publish ./Docs2Pdf.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:${PORT:-80}

RUN apt-get update && apt-get install -y --no-install-recommends libreoffice libreoffice-core libreoffice-writer libreoffice-calc libreoffice-impress && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish ./

RUN useradd --no-log-init --create-home appuser && chown -R appuser /app
USER appuser

EXPOSE 80
ENTRYPOINT ["dotnet", "Docs2Pdf.Api.dll"]
