build:
	@dotnet build --nologo grpc.sln

run: build
	@dotnet run -p GrpcApp
