IMAGE_NAME=grpc

build:
	@dotnet build --nologo grpc.sln

run: build
	@dotnet run -p GrpcApp

docker:
	@docker build -t $(IMAGE_NAME) .
	@docker image prune --filter='label=stage=build' -f >/dev/null
