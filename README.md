# PowerCord
 PowerCord is a Discord bot with a PowerShell frontend. Not affiliated with the Discord client of the same name, it's just a much better pun.

## Features

Like most bots, PowerCord has:
- A simple, yet powerful command syntax
- Consistent command naming (`Get-Ping`, `Get-ChildItem`, `Set-Content`)
- Named, typed parameters
- Command overloading and aliasing
- Custom type conversion
- Custom output formatting

But thanks to PowerShell, also adds...
 - [Operators (`+`, `-`, `-eq`, `-neq`)](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_operators)
 - [Conditionals (`if`, `else`)](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_if)
 - [Loops (`for`, `foreach`, `while`)](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_while)
 - [Functions](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_functions)
 - [Variables](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_variables)
 - [Arrays](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_arrays)
 - [Scripts](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_scripts)
 - [Exceptions](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_try_catch_finally)
 - A sandboxed filesystem
 - Dynamic modules as either PowerShell scripts, or .NET assemblies *with* support for codesigning

And most importantly, the Pipeline.

### Pipeline

[The Pipeline](https://docs.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_pipelines) is PowerShell's major party piece. The ability to pass data in the form of objects from one command to another using a simple, consistent syntax. 

It can be simple things, like getting the content of a file:

```ps
$ Get-ChildItem test.txt | Get-Content
```

Outputs:
```
Hello, world!
```

All the way to getting the top 5 members of the current server that aren't bots, ordered by their account age, formatted in a table showing their Id, Username, Discriminator and the exact date/time they joined Discord:

```ps
$ Get-AllDiscordMembers | 
    Where-Object { -not ($_.IsBot) } | 
        Sort-Object -Property Id | 
            Select-Object -First 5 | 
                Format-Table Id,Username,Discriminator,CreationTimestamp
```

Outputs: (personal details blanked)
```
                Id Username       Discriminator CreationTimestamp
                -- --------       ------------- -----------------
 973778867xxxxxxxx Redblueflame   xxxx          26/09/2015 17:04:57 +00:00
 998010980xxxxxxxx WamWooWam      xxxx          03/10/2015 09:33:55 +00:00
1024616296xxxxxxxx Daaniel        xxxx          10/10/2015 17:45:56 +00:00
1203989019xxxxxxxx BlockBuilder57 xxxx          29/11/2015 05:42:15 +00:00
1377390096xxxxxxxx X33N1OwO       xxxx          16/01/2016 02:05:38 +00:00
```

This, in my opinion, is where PowerShell (and by extension PowerCord) shines. The ability to run these kinds of complex command chains allowing you to analyse and search for data. This is what makes PowerCord unique, and why I decided to work on this in the first place.

# Building
To build, PowerCord depends on
 - .NET 5.0.103 SDK
 - PowerShell Core 6+ or PowerShell 7+
 - (Optional) Visual Studio 2019+

With these installed, building should be as simple as recursively cloning the repo, following [the build instructions for PowerShell](https://github.com/WamWooWam/PowerShell/blob/powercord/docs/building/windows-core.md), then opening `PowerCord.sln` in Visual Studio, and hitting go OR using `dotnet run` at the command line.

PowerCord will run on Windows, macOS and Linux hosts, however file system sandboxing is only available on Windows due to upstream issues.

PowerCord depends on a fork of PowerShell to allow access to internal classes and methods that really should be public, because creating a PowerShell host of this type without them is practically impossible.

# Publishing
PowerCord includes a `publish.ps1` script, designed to make publishing as simple as possible. It by default compiles and publishes for `win7-x64`, and signs the output binaries using `Signing.pfx` in the current directory. It's important that your target machine trusts this certificate, or modules will refuse to load.

The target machine also needs to have permissions configured. PowerCord must not be able to write to the current directory, content root, or `<ContentRoot>\Global` directories. If it can write to any of these, it will fail at runtime.

A sample configuration file is provided as `sample.appsettings.json`. Set your bot token and rename this file to `appsettings.json` before publishing.

To generate a self-signed certificate, I recommend using [the `New-SelfSignedCertificate` cmdlet](https://docs.microsoft.com/en-us/powershell/module/pkiclient/new-selfsignedcertificate).