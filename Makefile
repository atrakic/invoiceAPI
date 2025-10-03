MAKEFLAGS += --silent

.PHONY: all build docker run add remove format test clean

all: build docker
	func start

docker:
	docker compose up -d

build run add remove format test clean:
	dotnet $@

clean:
	docker-compose down --remove-orphans -v --rmi local
	dotnet clean
	rm -rf ./{bin,obj}
	rm -rf ./tests/*/{bin,obj}
