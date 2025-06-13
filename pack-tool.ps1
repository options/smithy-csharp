# Clean up previous packages
Remove-Item -Path ./nupkg -Recurse -Force -ErrorAction SilentlyContinue
mkdir -p ./nupkg

# Build solution
dotnet build -c Release

# Package the CLI tool
dotnet pack Smithy.Cli/Smithy.Cli.csproj -c Release -o ./nupkg

Write-Host ""
Write-Host "To install the tool locally (for testing), run:"
Write-Host "dotnet tool install --global --add-source ./nupkg smithy-csharp"
Write-Host ""
Write-Host "To uninstall:"
Write-Host "dotnet tool uninstall --global smithy-csharp"
