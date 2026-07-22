FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["mqtt2matrix.csproj", "./"]
RUN dotnet restore "mqtt2matrix.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "mqtt2matrix.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "mqtt2matrix.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
ENV MQTTSERVER="localhost"
ENV MQTTPORT="1883"
ENV MQTTUSER="mqtt"
ENV MQTTPASSWORD="mqtt"

ENV MQTTBASETOPIC="bot/matrix/actions/"

ENV MATRIXSERVER=""
ENV MATRIXUSERNAME=""
ENV MATRIXPASSWORD=""
ENV MATRIXDEVICEID="mqtt2matrixbot"
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mqtt2matrix.dll"]
