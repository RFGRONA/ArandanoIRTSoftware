# --- Etapa 1: Build ---
# Usamos la imagen completa del SDK de .NET 8 para compilar la aplicación.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia los archivos de proyecto (.csproj y .sln) y restaura las dependencias primero.
# Esto aprovecha el sistema de caché de Docker. Si no cambian los proyectos, no se vuelve a ejecutar.
COPY ["ArandanoIRT.Web.csproj", "."]
RUN dotnet restore "./ArandanoIRT.Web.csproj"

# Copia el resto del código fuente de la aplicación.
COPY . .

# Compila y publica la aplicación en modo 'Release' para producción.
# El resultado se guarda en la carpeta /app/publish.
RUN dotnet publish "./ArandanoIRT.Web.csproj" -c Release -o /app/publish --no-restore


# --- Etapa 2: Final ---
# Usamos la imagen ligera del runtime de ASP.NET, que es mucho más pequeña y segura.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Expone el puerto 8080. La aplicación escuchará en este puerto dentro del contenedor.
EXPOSE 8080

# Copia únicamente los artefactos compilados desde la etapa 'build'.
# Aquí está la magia: no se copia ni el SDK ni el código fuente, solo el resultado.
COPY --from=build /app/publish .

# Define el punto de entrada. Este es el comando que se ejecutará cuando el contenedor arranque.
ENTRYPOINT ["dotnet", "ArandanoIRT.Web.dll"]