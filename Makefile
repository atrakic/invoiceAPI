MAKEFLAGS += --silent

.PHONY: all build docker run add remove format test clean clean-all

all: build test docker
	func start

docker:
	docker compose up -d

build run add remove format test clean:
	if [ "$@" == "build" ]; then dotnet --version; fi
	dotnet $@

clean-all:
	docker-compose down --remove-orphans -v --rmi local
	dotnet clean
	rm -rf ./{bin,obj}
	rm -rf ./tests/*/{bin,obj}
