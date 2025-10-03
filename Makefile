MAKEFLAGS += --silent

.PHONY: all build docker run add remove format test infra clean clean-all

all: build test docker
	func start

docker:
	docker compose up -d

build run add remove format test clean:
	if [ "$@" == "build" ]; then dotnet --version; fi
	dotnet $@


rgName?=$(shell basename $(CURDIR))
location?=northeurope

infra:
	VERSION="refs/heads/master" \
	URL="https://raw.githubusercontent.com/Azure/azure-quickstart-templates/$$VERSION/quickstarts/microsoft.web/function-app-create-dynamic/main.bicep"; \
	FILE="main.bicep"; \
	echo "// # Downloaded from: [$$URL] at: [$$(date)]" > $$FILE; \
	curl -sL "$$URL" >> $$FILE; \
	echo "OK"
	az account show
	az group create --name $(rgName) --location $(location)
	az deployment group create \
		--resource-group $(rgName) \
		--template-file main.bicep \
		--parameters location=$(location)

clean-all:
	docker-compose down --remove-orphans -v --rmi local
	dotnet clean
	rm -rf ./{bin,obj}
	rm -rf ./tests/*/{bin,obj}
