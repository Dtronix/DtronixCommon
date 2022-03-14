# DtronixCommon [![NuGet](https://img.shields.io/nuget/v/DtronixCommon.svg?maxAge=60)](https://www.nuget.org/packages/DtronixCommon) [![Action Workflow](https://github.com/Dtronix/DtronixCommon/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Dtronix/DtronixCommon/actions)

DtronixCommon is a support library which houses collections of common or performance classes & utility methods. The intent is that this library can be directly used via the NuGet package, or to be referenced as a Git Submodule since the entire library may not be needed in all projects.  This may change at a later point where the library is broken up into smaller libraries.

### Namespaces & Classes

Classes described with (Isolated) do not have any other class depencices inside the project and the directory can be referenced as a git module.

[Dtronix.Threading](docs/classes/Dtronix.Threading.md)

### Usage

- [Nuget Package](https://www.nuget.org/packages/DtronixCommon).
- Manual building. `dotnet build -c Release`

### Build Requirements

- .NET 6.0

### License

[MIT](LICENSE) License
