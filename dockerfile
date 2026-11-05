FROM mcr.microsoft.com/dotnet/sdk:10.0 as build
workdir /src

COPY ["AttendanceLesson.csproj", "./"]
RUN dotnet restore "AttendanceLesson.csproj"

COPY . . 
RUN dotnet publish "AttendanceLesson.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

expose 8000
ENTRYPOINT ["dotnet","AttendanceLesson.dll"]
