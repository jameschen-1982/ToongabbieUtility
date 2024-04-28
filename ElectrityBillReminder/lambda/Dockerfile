FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /lambda

RUN apt-get update \
   && apt-get install -y zip clang zlib1g-dev

COPY ./ToongabbieUtility.ElectricityBillReminder/ToongabbieUtility.ElectricityBillReminder.csproj ./ToongabbieUtility.ElectricityBillReminder/ToongabbieUtility.ElectricityBillReminder.csproj
COPY ./ToongabbieUtility.ElectricityBillReminder.Tests/ToongabbieUtility.ElectricityBillReminder.Tests.csproj ./ToongabbieUtility.ElectricityBillReminder.Tests/ToongabbieUtility.ElectricityBillReminder.Tests.csproj

RUN dotnet restore ./ToongabbieUtility.ElectricityBillReminder/ToongabbieUtility.ElectricityBillReminder.csproj
RUN dotnet restore ./ToongabbieUtility.ElectricityBillReminder.Tests/ToongabbieUtility.ElectricityBillReminder.Tests.csproj

COPY . .

RUN dotnet test ./ToongabbieUtility.ElectricityBillReminder.Tests/ToongabbieUtility.ElectricityBillReminder.Tests.csproj

# Amazon.Lambda.Tools doesn't yet support .NET 8, so reverting to dotnet publish and zip rather than dotnet lambda package
RUN dotnet publish --configuration Release --self-contained --runtime linux-x64 ./ToongabbieUtility.ElectricityBillReminder/ToongabbieUtility.ElectricityBillReminder.csproj

RUN cd /lambda/ToongabbieUtility.ElectricityBillReminder/bin/Release/net8.0/linux-x64/publish && zip -r /lambda/ToongabbieUtility.ElectricityBillReminder.zip .
