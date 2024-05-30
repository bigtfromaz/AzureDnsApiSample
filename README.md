# Azure DNS API Sample Program

This repository contains a comprehensive sample program demonstrating the usage of Azure DNS API. 
The program is written in C# and uses the Azure SDK for .NET.

## Overview

The sample program demonstrates how to interact with Azure DNS using the Azure SDK for .NET. It assumes you already have a DNS zone and covers the following operations:

- Reading configuration from `appsettings.json`, environment variables, and command line arguments.
-- Shows how to cascade configuration sources.
- Authenticating with Azure using a client secret credential.
- Fetching a subscription resource from Azure.
- Listing all DNS zones in a subscription.
- Creating and updating DNS `A` and `AAAA` records in a DNS zone.
- Fetching a specific DNS record.
- Deleting a DNS record.
- Printing out all the recordsets in a DNS zone.

## Prerequisites

- .NET 7.0 SDK
- An Azure account
- A configured Azure DNS zone

## Setup

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Update the `appsettings.json` file with your Azure credentials and DNS zone details.	 
   a) For Visual Studio you should set "User Secrets" in your project to keep your secrets private.
   b) You can also set environment variables to keep your secrets

## Running the Program

You can run the program directly from Visual Studio by pressing `F5` or from the command line using the `dotnet run` command.

## Code Structure

The main logic of the program is contained in the `Main` method in `Program.cs`. This method performs all the operations mentioned in the overview.

The program also contains several helper methods:

- `NewARecord`: Creates a new DNS `A` record given an IPv4 address.
- `NewAaaaRecord`: Creates a new DNS `AAAA` record given an IPv6 address.
- `GetHostList`: Returns a list of all the hosts in a DNS zone.
- `DeleteHost`: Deletes all the records having a given host name.
- `PrintOutZone`: Prints out various details about a DNS zone and its records.

## Packages Used

The program uses the following NuGet packages:

- Azure.Core
- Azure.Identity
- Azure.ResourceManager.Dns
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Configuration.Binder
- Microsoft.Extensions.Configuration.CommandLine
- Microsoft.Extensions.Configuration.EnvironmentVariables
- Microsoft.Extensions.Configuration.UserSecrets

## Further Reading

For more information on Azure DNS, see the [official Azure DNS documentation](https://docs.microsoft.com/en-us/azure/dns/). For more information on the Azure SDK for .NET, see the [official Azure SDK for .NET documentation](https://docs.microsoft.com/en-us/dotnet/azure/).
