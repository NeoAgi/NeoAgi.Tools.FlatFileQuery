# Pull our base image
FROM 161361684013.dkr.ecr.us-west-2.amazonaws.com/dotnet-sdk6.0:latest AS build

COPY . /src/app
WORKDIR /src/app

# Setup our NuGet
ARG KEY_ID=Undefined
ARG ACCESS_KEY=Undefined
ENV AWS_ACCESS_KEY_ID=${KEY_ID}
ENV AWS_SECRET_ACCESS_KEY=${ACCESS_KEY}

# Build the Application
RUN dotnet restore NeoAgi.Tools.FlatFileQuery.csproj 
RUN dotnet build NeoAgi.Tools.FlatFileQuery.csproj -c Release -o /app/build

# Publish the Application
FROM build AS publish
RUN dotnet publish NeoAgi.Tools.FlatFileQuery.csproj -c Release -r linux-x64 --self-contained -o /app/release

# Build our Distroless Layer
FROM gcr.io/distroless/dotnet AS final
COPY --from=publish /app/release /opt/release

ENV BUILD_HASH=##BUILD_HASH##

# Finally Execute our Container
WORKDIR /opt/release
ENTRYPOINT [ "dotnet", "NeoAgi.Tools.FlatFileQuery.dll" ]