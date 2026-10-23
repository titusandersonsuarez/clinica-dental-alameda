FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DentalAlameda.sln ./
COPY src/DentalAlameda.Domain/*.csproj src/DentalAlameda.Domain/
COPY src/DentalAlameda.Application/*.csproj src/DentalAlameda.Application/
COPY src/DentalAlameda.Infrastructure/*.csproj src/DentalAlameda.Infrastructure/
COPY src/DentalAlameda.Api/*.csproj src/DentalAlameda.Api/
COPY tests/DentalAlameda.Tests/*.csproj tests/DentalAlameda.Tests/
RUN dotnet restore
COPY . .
RUN dotnet test --no-restore -c Release && \
    dotnet publish src/DentalAlameda.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 TZ=America/Bogota
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "DentalAlameda.Api.dll"]
