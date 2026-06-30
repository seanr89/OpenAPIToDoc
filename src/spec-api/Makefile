# Makefile for spec-api

# Variables
CONFIGURATION ?= Release
OUTPUT_DIR ?= ./publish
OPENAPI_DIR ?= .
OPENAPI_FILE ?= openapi.json
DOTNET ?= dotnet

.PHONY: all build release openapi clean help

all: build openapi

build:
	$(DOTNET) build

release:
	$(DOTNET) publish -c $(CONFIGURATION) -o $(OUTPUT_DIR)

openapi:
	$(DOTNET) build -c $(CONFIGURATION) --no-incremental /p:OpenApiDocumentsDirectory="$(OPENAPI_DIR)" /p:OpenApiGenerateDocumentsOptions="--file-name $(basename $(OPENAPI_FILE))"
	@echo "OpenAPI specification generated successfully at: $(OPENAPI_DIR)/$(OPENAPI_FILE)"

clean:
	$(DOTNET) clean
	rm -rf $(OUTPUT_DIR)
	rm -f $(OPENAPI_DIR)/$(OPENAPI_FILE)

help:
	@echo "Available commands:"
	@echo "  make build         - Build the project in Debug mode (default)"
	@echo "  make release       - Publish a release build of the project to $(OUTPUT_DIR)"
	@echo "  make openapi       - Generate OpenAPI specification file ($(OPENAPI_DIR)/$(OPENAPI_FILE))"
	@echo "  make clean         - Clean build artifacts and generated files"
	@echo "  make help          - Display this help message"
