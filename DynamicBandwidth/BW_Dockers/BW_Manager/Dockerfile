FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
WORKDIR /app


COPY /app/published .
ENTRYPOINT ["dotnet", "DynamicBandwidthManager.dll"]