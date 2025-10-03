MAKEFLAGS += --silent

.PHONY: all build docker run add remove format test infra clean clean-infra clean-local

all: build test docker
	func start

docker:
	docker compose up -d

build run add remove format test clean:
	if [ "$@" == "build" ]; then dotnet --version; fi
	dotnet $@

clean-local:
	docker-compose down --remove-orphans -v --rmi local
	dotnet clean
	rm -rf ./{bin,obj}
	rm -rf ./tests/*/{bin,obj}

rgName?=$(shell basename $(shell pwd -P))
location?=northeurope
FILE?=main.bicep

infra:
	@if [ ! -f $(FILE) ]; then \
		VERSION="refs/heads/master"; \
		URL="https://raw.githubusercontent.com/Azure/azure-quickstart-templates/$$VERSION/quickstarts/microsoft.web/function-app-create-dynamic/main.bicep"; \
		echo "// # Downloaded from: [$$URL] at: [$$(date)]" > $(FILE); \
		curl -sL "$$URL" >> $(FILE); \
		echo "OK"; \
	fi
	az account show || az login --scope https://management.core.windows.net//.default
	az group create --name $(rgName) --location $(location)
	az deployment group create \
		--resource-group $(rgName) \
		--template-file main.bicep \
		--parameters runtime=dotnet \
		--parameters location=$(location)

clean-infra:
	az group delete --name $(rgName)
