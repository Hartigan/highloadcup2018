FROM base/archlinux AS build-env
WORKDIR /AspNetCoreWebApi

RUN pacman -Sy
RUN pacman -S dotnet-sdk --noconfirm

# Copy everything else and build
COPY ./AspNetCoreWebApi/ ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Build runtime image
FROM base/archlinux as runtime
WORKDIR /app

RUN pacman -Sy
RUN pacman -S dotnet-runtime --noconfirm

COPY --from=build-env /AspNetCoreWebApi/out .
ENTRYPOINT ["dotnet", "AspNetCoreWebApi.dll"]
