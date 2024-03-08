# ADP Portal API - Developer Guide

## Overview
The ADP Portal API provides backend services for the ADP Portal web application. This document provides information on setting up your development environment, running the API, and testing the API endpoints.

## Getting Started

### Prerequisites
- .NET Core 8.0
- Visual Studio 2022 or VS Code

### Installation
1. Clone the repository
   ```
   git clone https://github.com/DEFRA/adp-portal-api
    ```
2. Open the solution in Visual Studio or VS Code
3. Restore the NuGet packages
4. Build the solution

## Configuration Settings

### AdpAdoProject
This section contains settings related to the ADO project.

- `Name`: The name of the ADP ADO project.

### Ado
This section contains settings related to Azure DevOps.

- `OrganizationUrl`: The URL of the Azure DevOps organization.
- `UsePatToken`: A boolean value indicating whether to use a Personal Access Token (PAT) for authentication.
- `PatToken`: The Personal Access Token for authentication.

### AdpTeamGitRepo
This section contains settings related to the Git repository for the team.

- `RepoName`: The name of the repository.
- `BranchName`: The name of the branch.
- `Organisation`: The name of the organization.
- `Auth`: This section contains authentication details for the GitHub App.
  - `AppName`: The name of the GitHub App.
  - `AppId`: The ID of the GitHub App.
  - `PrivateKeyBase64`: The private key for the GitHub App, encoded in Base64.

### AzureAd
This section contains settings related to Azure Active Directory.

- `TenantId`: The tenant ID for Azure AD.
- `SpClientId`: The client ID for the service principal.
- `SpClientSecret`: The client secret for the service principal.
- `SpObjectId`: The object ID for the service principal.
