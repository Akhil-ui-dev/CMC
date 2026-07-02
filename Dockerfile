# Stage 1: Build stage using .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["Akhil.Proj.Web/Akhil.Proj.Web.csproj", "Akhil.Proj.Web/"]
RUN dotnet restore "Akhil.Proj.Web/Akhil.Proj.Web.csproj"

# Copy the rest of the source code and build
COPY . .
WORKDIR "/src/Akhil.Proj.Web"
RUN dotnet build "Akhil.Proj.Web.csproj" -c Release -o /app/build

# Stage 2: Publish stage
FROM build AS publish
RUN dotnet publish "Akhil.Proj.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final runtime stage using ASP.NET 10 Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Expose default ASP.NET Core port (8080)
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Akhil.Proj.Web.dll"]
