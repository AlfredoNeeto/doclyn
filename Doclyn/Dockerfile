FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["Doclyn.slnx", "."]
COPY ["Doclyn.Api/Doclyn.Api.csproj", "Doclyn.Api/"]
COPY ["Doclyn.Application/Doclyn.Application.csproj", "Doclyn.Application/"]
COPY ["Doclyn.Domain/Doclyn.Domain.csproj", "Doclyn.Domain/"]
COPY ["Doclyn.Infrastructure/Doclyn.Infrastructure.csproj", "Doclyn.Infrastructure/"]
RUN dotnet restore "Doclyn.Api/Doclyn.Api.csproj"

COPY . .
RUN dotnet publish "Doclyn.Api/Doclyn.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends tesseract-ocr tesseract-ocr-por libtesseract-dev libleptonica-dev \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
COPY tessdata ./tessdata

ENTRYPOINT ["dotnet", "Doclyn.Api.dll"]
