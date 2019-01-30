FROM microsoft/dotnet:2.2-sdk AS build-env
WORKDIR /AspNetCoreWebApi

# Copy everything else and build
COPY ./AspNetCoreWebApi/ ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:2.2-aspnetcore-runtime as runtime
WORKDIR /app

COPY --from=build-env /AspNetCoreWebApi/out .
ENTRYPOINT ["dotnet", "AspNetCoreWebApi.dll"]
