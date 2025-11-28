# Stage 1: Build
FROM mcr.microsoft.com / dotnet / sdk:9.0 AS build
WORKDIR /app

# Copy the project file and restore dependencies
COPY MobileProviderBillPaymentSystem.csproj .
RUN dotnet restore

# Copy the rest of the source code
COPY . .

# Build and publish the app in Release mode
RUN dotnet publish -c Release -o /app/publish


# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Copy published files from build stage
COPY --from=build /app/publish .

# Expose port 80
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

# Run the API
ENTRYPOINT["dotnet", "MobileProviderBillPaymentSystem.dll"]
