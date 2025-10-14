# RLBot CSharpInterface

A library to interface with the RLBot v5 framework through C#.

## Usage

Add the package to your project using:
```
dotnet add package RLBot.Framework
```
You can use `--version x.y.z` to install a specific version. See https://www.nuget.org/packages/RLBot.Framework for available versions.

Refer to `Tests/` for a few examples.

## Contribute

Setup

1. Make sure [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) is installed
1. Clone repository
1. Update submodules: `git submodules update --init`
1. Generate flatbuffers: `./generate-flatbuffers.sh`
1. Open `CSharpInterface.sln` in your favorite C# IDE

### Formatting

This project uses the CSharpier formatter. You can run it with `dotnet csharpier .`

### Testing

There is currently a few system tests available in `Tests/`.

TODO

### Deployment

The RLBot interface is available on NuGet at https://www.nuget.org/packages/RLBot.Framework.

To publish new versions there:
1. Modify `RLBot/RLBot.csproj` to have an incremented version number.
1. Modify `RLBot/RLBot.csproj` to update the release notes, etc.
1. Make sure `RLBot` has been built with the Release configuration: `dotnet build -c Release`
    - This should generate `.nupkg` and `.snupkg` files in `RLBot/bin/Release/`.
1. Go to https://www.nuget.org/packages/manage/upload and log in with an account owning the nuget package.
1. Upload the `.nupkg` file, then upload the `.snupkg` file.
