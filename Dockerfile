# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only csproj first to leverage layer caching
COPY MobileProviderBillPaymentSystem.csproj ./

# Restore dependencies
RUN dotnet restore MobileProviderBillPaymentSystem.csproj

# Copy the rest of the source code
COPY . .

# Publish the project (no-restore because we already restored)
RUN dotnet publish MobileProviderBillPaymentSystem.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish ./

# Bind to port 80 and allow container to listen on all network interfaces
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "MobileProviderBillPaymentSystem.dll"]
