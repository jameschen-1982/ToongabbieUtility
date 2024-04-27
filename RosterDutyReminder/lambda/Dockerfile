FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /lambda

RUN apt-get update \
   && apt-get install -y zip clang zlib1g-dev

COPY ./RosterDutyReminder/RosterDutyReminder.csproj ./RosterDutyReminder/RosterDutyReminder.csproj
COPY ./RosterDutyReminder.Tests/RosterDutyReminder.Tests.csproj ./RosterDutyReminder.Tests/RosterDutyReminder.Tests.csproj

RUN dotnet restore ./RosterDutyReminder/RosterDutyReminder.csproj
RUN dotnet restore ./RosterDutyReminder.Tests/RosterDutyReminder.Tests.csproj

COPY . .

RUN dotnet test ./RosterDutyReminder.Tests/RosterDutyReminder.Tests.csproj

# Amazon.Lambda.Tools doesn't yet support .NET 8, so reverting to dotnet publish and zip rather than dotnet lambda package
RUN dotnet publish --configuration Release --self-contained --runtime linux-x64 ./RosterDutyReminder/RosterDutyReminder.csproj

RUN cd /lambda/RosterDutyReminder/bin/Release/net8.0/linux-x64/publish && zip -r /lambda/RosterDutyReminder.zip .
