FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# csproj dosyasını kopyala ve restore et
COPY *.csproj .
RUN dotnet restore

# tüm kaynak kodları kopyala ve publish et
COPY . .
RUN dotnet publish -c Release -o /app

# runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

# SQLite için wwwroot klasörünü oluştur
RUN mkdir -p /app/wwwroot

# portları aç
EXPOSE 80
EXPOSE 443

# ortam değişkenleri
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

# uygulamayı başlat
ENTRYPOINT ["dotnet", "SporKulubu.dll"]