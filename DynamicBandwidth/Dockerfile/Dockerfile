FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app

EXPOSE 6379:6379

COPY /app/published .
ENTRYPOINT ["dotnet", "DynamicBandwidthManager.dll"]