# Use SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy csproj and restore
COPY *.csproj .
RUN dotnet restore

# Copy all files and build
COPY . .
RUN dotnet publish -c Release -o out

# Use runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "MobileProviderBillPaymentSystem.dll"]
