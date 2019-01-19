FROM microsoft/dotnet:sdk AS build-env
WORKDIR /AspNetCoreWebApi

# Copy csproj and restore as distinct layers


# Copy everything else and build
COPY ./AspNetCoreWebApi/ ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /AspNetCoreWebApi/out .
ENTRYPOINT ["dotnet", "AspNetCoreWebApi.dll"]
