FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
LABEL stage=build
WORKDIR /app

# copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out -r linux-x64

# build runtime image
FROM debian:10-slim
ENV TZ Asia/Tokyo
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT 1

WORKDIR /app
COPY --from=build-env /app/out ./
CMD ["/app/GrpcApp.App"]
