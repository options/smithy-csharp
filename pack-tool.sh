#!/bin/bash

# Clean up previous packages
rm -rf ./nupkg
mkdir -p ./nupkg

# Build solution
dotnet build -c Release

# Package the CLI tool
dotnet pack Smithy.Cli/Smithy.Cli.csproj -c Release -o ./nupkg

echo ""
echo "To install the tool locally (for testing), run:"
echo "dotnet tool install --global --add-source ./nupkg smithy-csharp"
echo ""
echo "To uninstall:"
echo "dotnet tool uninstall --global smithy-csharp"
