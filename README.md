# MigrateWcfToWebApi

.NET Core console app that auto generates ASP.NET Web API service and associated client code files to replace exising WCF Service endpoints. 

[Roslyn (.NET Compiler Platform)](https://github.com/dotnet/roslyn) libraries for C# are used to parse/generate the files.

## Install

1) Clone repo
2) Build solution
3) run _MigrateWcfToWebApi.dll_ from command line with specified args

## Requirements

- .NET Core 2.0

## Usage

For details run `dotnet MigrateWcfToWebApi.dll -h` 

## Example

```powershell
dotnet MigrateWcfToWebApi.dll -wcfServiceDir:"../myServiceFiles" -serviceOutputDir:"../serviceOutput" -wcfClientDir:"../myClientFiles" -clientOutputDir:"../clientOutput" 
```
