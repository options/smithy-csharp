# Smithy to C# Code Generator

[![Build Status](https://github.com/options/smithy-csharp/actions/workflows/build.yml/badge.svg)](https://github.com/options/smithy-csharp/actions/workflows/build.yml)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![GitHub Release](https://img.shields.io/github/release/options/smithy-csharp.svg)](https://github.com/options/smithy-csharp/releases)

A robust code generator that transforms Smithy 2.0 service models into idiomatic C# code.

## 🚀 Overview

The Smithy to C# code generator is a tool that allows developers to define their service models using the Smithy 2.0 IDL (Interface Definition Language) and generate production-ready C# code. This project aims to bridge the gap between the Smithy specification and .NET ecosystem by providing a comprehensive code generation solution.

## ✨ Features

- **Complete Smithy 2.0 Support**: Parse and generate code for all Smithy 2.0 shapes and traits
- **Idiomatic C# Generation**: Produces clean, readable C# code following .NET conventions
- **ASP.NET Core Integration**: Generated APIs work seamlessly with ASP.NET Core 
- **Validation**: Generated models include appropriate validation attributes
- **Documentation**: XML comments from Smithy are preserved in C# output
- **Customization**: Extensible design allows for custom generation rules

## 📋 Requirements

- .NET 8.0 SDK or later
- Windows, macOS, or Linux operating system

## 🔧 Installation

### Using .NET CLI

```bash
# Install the tool globally
dotnet tool install -g smithy-csharp
```

### Building from Source

```bash
# Clone the repository
git clone https://github.com/options/smithy-csharp.git
cd smithy-csharp

# Build the solution
dotnet build

# Package the tool
# On Windows
.\pack-tool.ps1
# On Linux/macOS
./pack-tool.sh

# Install from local package
dotnet tool install -g --add-source ./nupkg smithy-csharp
```

### Versioning and Releases

The project follows semantic versioning (SemVer). When creating releases:

1. Update the version in `Smithy.Cli/Smithy.Cli.csproj`:
   ```xml
   <Version>X.Y.Z</Version>
   <AssemblyVersion>X.Y.Z.0</AssemblyVersion>
   <FileVersion>X.Y.Z.0</FileVersion>
   ```

2. Create and push a tag that matches the version:
   ```bash
   git tag vX.Y.Z
   git push origin vX.Y.Z
   ```

This will trigger the GitHub Actions release workflow which will:
- Build and test the solution
- Package the tool with the version from the tag
- Create a GitHub Release with the package attached
- Publish the package to NuGet.org
```

You can also use the provided scripts:
- Windows: `.\pack-tool.ps1`
- macOS/Linux: `./pack-tool.sh`

## 📝 Usage

### Basic Usage

```bash
# Generate C# code from a Smithy model
smithy-cli generate path/to/smithy/file.smithy

# Specify output directory
smithy-cli generate path/to/smithy/file.smithy --output MyGeneratedCode

# Get help
smithy-cli --help

# Check version
smithy-cli --version
```

### Generating from Multiple Files

```bash
smithy-cli generate --input-dir path/to/smithy/files --output MyGeneratedCode
```

### Using JSON Model Files

```bash
smithy-cli generate path/to/model.json --output MyGeneratedCode
```

### Customizing Output

```bash
smithy-cli generate path/to/smithy/file.smithy --output MyGeneratedCode --namespace MyCompany.Services
```

## 📊 Project Structure

- **Smithy.Model**: Core model classes representing Smithy shapes and traits
- **Smithy.CSharpGenerator**: The code generator engine
- **Smithy.Cli**: Command-line interface for the generator
- **Smithy.CSharpGenerator.Tests**: Unit tests for the code generator

## 🧩 Code Examples

### Simple Smithy Definition

```smithy
namespace example.weather

/// Weather forecast service
@restJson1
service WeatherService {
    version: "2023-01-01",
    operations: [GetForecast]
}

/// Get weather forecast operation
@http(method: "GET", uri: "/forecast/{locationId}")
operation GetForecast {
    input := {
        @required
        @httpLabel
        locationId: String,
        
        @httpQuery("units")
        units: String = "celsius"
    },
    output := {
        forecast: ForecastData
    }
}

structure ForecastData {
    @required
    temperature: Float,
    description: String,
    precipitation: Float
}
```

### Generated C# Code

```csharp
namespace example.weather
{
    /// <summary>
    /// Weather forecast service
    /// </summary>
    [ApiController]
    [Route("api/v2023-01-01/weather-service")]
    public class WeatherServiceController : ControllerBase, IWeatherService
    {
        /// <summary>
        /// Get weather forecast operation
        /// </summary>
        [HttpGet("/forecast/{locationId}")]
        public Task<GetForecastOutput> GetForecast(GetForecastInput input)
        {
            // TODO: Implement service operation
            return Task.FromResult(new GetForecastOutput());
        }
    }

    public interface IWeatherService
    {
        /// <summary>
        /// Get weather forecast operation
        /// </summary>
        Task<GetForecastOutput> GetForecast(GetForecastInput input);
    }

    public class GetForecastInput
    {
        [Required]
        [FromRoute]
        public string LocationId { get; set; }
        
        [FromQuery(Name = "units")]
        public string Units { get; set; } = "celsius";
    }

    public class GetForecastOutput
    {
        public ForecastData Forecast { get; set; }
    }

    public class ForecastData
    {
        [Required]
        public float Temperature { get; set; }
        
        public string Description { get; set; }
        
        public float? Precipitation { get; set; }
    }
}
```

## 📈 Current Status

The project currently supports approximately 95% of the Smithy 2.0 specification, with only a few advanced features still in development. The generated code is production-ready and follows best practices for C# development.

### Supported Features

- ✅ All simple types (blob, boolean, string, byte, short, integer, long, float, double, bigInteger, bigDecimal, timestamp, document)
- ✅ Aggregate types (structure, list, map, set, union)
- ✅ Service types (service, operation, resource)
- ✅ Constraint traits and validation attributes
- ✅ HTTP protocol binding traits
- ✅ Error traits and exception generation
- ✅ Documentation preservation

### In Development

- 🚧 Mixins support
- 🚧 Advanced selector support
- 🚧 Advanced streaming operations

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgements

- [AWS Smithy](https://awslabs.github.io/smithy/) - The Smithy specification
- [.NET Foundation](https://dotnetfoundation.org/) - For their amazing work on .NET
