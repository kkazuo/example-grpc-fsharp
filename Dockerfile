FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
LABEL stage=build
WORKDIR /app

# copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out -r linux-x64 --self-contained=false /p:PublishSingleFile=false || true

# build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
ENV TZ Asia/Tokyo
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT 1

WORKDIR /app
COPY --from=build-env /app/out/ /app/
CMD ["/app/GrpcApp.App"]
