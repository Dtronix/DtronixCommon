# DtronixCommon [![NuGet](https://img.shields.io/nuget/v/DtronixCommon.svg?maxAge=60)](https://www.nuget.org/packages/DtronixCommon) [![Action Workflow](https://github.com/Dtronix/DtronixCommon/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Dtronix/DtronixCommon/actions)

DtronixCommon is a support library which houses a collection of commonly used classes & utility methods to be resued.  The intent is that this library can be directly used via the NuGet package, or to be referenced as a Git Submodule since the entire library may not be needed in all projects.  This may change at a later point where the library is broken up into smaller libraries.

### Usage

- [Nuget Package](https://www.nuget.org/packages/DtronixCommon).
- Manual building. `dotnet build -c Release`

### Build Requirements

- .NET 6.0

##### Dtronix.Threading.Dispatcher.ThreadDispatcher

This class and it's supporting classes are for the management of separately executed queued actions.  It allows the specification of the nubmer of threads to create and execute the passed actions on.  It is TPL based so any blocking action or task can be awaited on until completion 

##### Dtronix.Threading.Tasks.ManualResetAwaiterSource & ManualResetAwaiterSource<T>

Performace class which is close to a `TaskCompletionSource`, but less the full task class generation and can be reset for reuse.

### License

[MIT](LICENSE) License
