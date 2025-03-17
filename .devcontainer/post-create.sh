#!/usr/bin/env bash

USERNAME=${USERNAME:-"vscode"}

set -eux

# Setup STDERR.
err() {
    echo "(!) $*" >&2
}

# Ensure apt is in non-interactive to avoid prompts
export DEBIAN_FRONTEND=noninteractive

# Prepare the development environment
dotnet restore src/Pdf2Image.sln
dotnet build src/Pdf2Image.sln --no-restore
