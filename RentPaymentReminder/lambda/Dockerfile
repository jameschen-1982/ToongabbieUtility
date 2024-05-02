FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /lambda

RUN apt-get update \
   && apt-get install -y zip clang zlib1g-dev

COPY ./ToongabbieUtility.RentPaymentReminder/ToongabbieUtility.RentPaymentReminder.csproj ./ToongabbieUtility.RentPaymentReminder/ToongabbieUtility.RentPaymentReminder.csproj
COPY ./ToongabbieUtility.RentPaymentReminder.Tests/ToongabbieUtility.RentPaymentReminder.Tests.csproj ./ToongabbieUtility.RentPaymentReminder.Tests/ToongabbieUtility.RentPaymentReminder.Tests.csproj

RUN dotnet restore ./ToongabbieUtility.RentPaymentReminder/ToongabbieUtility.RentPaymentReminder.csproj
RUN dotnet restore ./ToongabbieUtility.RentPaymentReminder.Tests/ToongabbieUtility.RentPaymentReminder.Tests.csproj

COPY . .

RUN dotnet test ./ToongabbieUtility.RentPaymentReminder.Tests/ToongabbieUtility.RentPaymentReminder.Tests.csproj

# Amazon.Lambda.Tools doesn't yet support .NET 8, so reverting to dotnet publish and zip rather than dotnet lambda package
RUN dotnet publish --configuration Release --self-contained --runtime linux-x64 ./ToongabbieUtility.RentPaymentReminder/ToongabbieUtility.RentPaymentReminder.csproj

RUN cd /lambda/ToongabbieUtility.RentPaymentReminder/bin/Release/net8.0/linux-x64/publish && zip -r /lambda/ToongabbieUtility.RentPaymentReminder.zip .
