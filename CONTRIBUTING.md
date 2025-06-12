# Contributing to Smithy C# Code Generator

Thank you for considering contributing to the Smithy C# Code Generator! Your contributions help make this project better for everyone.

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue on GitHub with the following information:

1. A clear, descriptive title
2. A detailed description of the issue
3. Steps to reproduce the problem
4. Expected behavior
5. Actual behavior
6. Screenshots if applicable
7. System information (OS, .NET version, etc.)

### Suggesting Enhancements

We welcome suggestions for enhancements! Please create an issue with:

1. A clear, descriptive title
2. A detailed description of the proposed enhancement
3. Any relevant examples or use cases
4. If applicable, references to similar features in other projects

### Code Contributions

If you'd like to contribute code:

1. Fork the repository
2. Create a new branch for your feature or fix
3. Write and test your code
4. Ensure your code follows the project's code style
5. Submit a pull request

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Git
- Your favorite IDE (Visual Studio, VS Code, Rider, etc.)

### Getting Started

1. Clone your fork of the repository
2. Navigate to the project directory
3. Run `dotnet restore` to restore dependencies
4. Run `dotnet build` to build the solution
5. Run `dotnet test` to run the tests

## Code Style Guidelines

### C# Guidelines

- Follow the [.NET coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Keep methods small and focused
- Write XML documentation for public APIs
- Include unit tests for new features

### Commit Guidelines

- Use clear, descriptive commit messages
- Begin the message with a short summary (50 characters or less)
- Reference issues and pull requests when appropriate

## Pull Request Process

1. Update the README.md with details of changes if applicable
2. Add or update tests for any new features
3. Ensure all tests pass
4. Update any relevant documentation
5. Your PR will be reviewed by the maintainers
6. Once approved, your PR will be merged

## Project Structure

- **Smithy.Model**: Core model classes
- **Smithy.CSharpGenerator**: Code generator logic
- **Smithy.Cli**: Command-line interface
- **Smithy.CSharpGenerator.Tests**: Unit tests

## Testing

Please ensure that any code you contribute includes appropriate tests. We aim for high test coverage and thorough testing of edge cases.

## Code of Conduct

Please note that this project adheres to a [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## Questions?

If you have any questions or need help with contributing, please feel free to create an issue or reach out to the maintainers.

Thank you for your contributions!
