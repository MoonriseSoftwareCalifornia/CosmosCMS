# Cosmos CMS ARM Templates - AI Agent Guide

## Project Overview
This repository contains Azure Resource Manager (ARM) templates for deploying Cosmos CMS, a multi-tenant content management system with flexible architecture options. The infrastructure supports three deployment architectures: Static, Decoupled, and Headless.

## Architecture Patterns

### Multi-Architecture Deployment Model
Cosmos CMS supports three distinct architectures controlled by the `architecture` parameter:

- **Static**: Single-tier CMS with static web hosting (`CosmosStaticWebPages: true`)
- **Decoupled**: Editor + Publisher services with separate containers (`cosmos-editor` + `cosmos-publisher`)  
- **Headless**: API-only backend with development environment (`cosmos-editor` + `cosmos-api`)

### Conditional Resource Deployment
The templates use ARM conditional deployment extensively:
```arm
"condition": "[or(equals(parameters('architecture'),'Decoupled'), equals(parameters('architecture'),'Headless'))]"
```
This pattern determines whether publisher/API services are deployed based on architecture choice.

## Infrastructure Components

### Core Azure Services
- **Cosmos DB**: Serverless SQL database (`EnableServerless` capability)
- **Azure Storage**: Blob storage with `$web` container for static hosting
- **App Service Plan**: Linux containers with Docker images from `toiyabe/cosmos-*`
- **Azure Front Door**: CDN with WAF protection and custom domain support

### Container Strategy
Uses specific container patterns:
- Editor: `toiyabe/cosmos-editor:latest`
- Publisher: `toiyabe/cosmos-publisher:latest` (Decoupled)
- API: `toiyabe/cosmos-api:latest` (Headless)

### Security & Identity
- Managed Identity enabled on all App Services
- WAF policies reference external resource: `/subscriptions/eea20e7a-4dd0-4385-80d6-6b448e17a7da/resourceGroups/RG-ADMIN/providers/Microsoft.Network/frontdoorWebApplicationFirewallPolicies/CosmosMultiTenantEditorFrontDoorStandard`

## Template Structure

### Key Files
- `azuredeploy.json`: Main deployment template (single-tenant)
- `azuredeploy-multitenant.json`: Enhanced version with multi-tenancy features  
- `azuredeploy-staticweb-cdn.json`: Front Door CDN configuration for static sites
- `temp.json`: Simplified Front Door template for testing

### Variable Naming Convention
Uses `uniqueString(resourceGroup().id)` for resource naming:
- Storage: `files{uniqueString}`
- Cosmos DB: `cosmos{uniqueString}`
- App Services: `editor-{uniqueString}`, `publisher-{uniqueString}`

### Email Provider Integration
Supports multiple email services through conditional configuration:
- Azure Communications Services (`azureCommunicationsConnectionString`)
- SendGrid (`sendGridApiKey`)
- SMTP relay (`SmtpEmailProviderOptions__*` settings)

## Configuration Patterns

### App Settings Strategy
Critical configuration uses environment-specific patterns:
- `CosmosArchitecture`: Drives conditional feature behavior
- `ASPNETCORE_ENVIRONMENT`: Set to "Development" for Headless, "Production" for others
- `CosmosPublisherUrl`: Cross-service communication URL

### Storage Container Structure
Standard containers created:
- `$web`: Static website hosting
- `pkeys`: Encryption keys storage  
- `ekeys`: Additional encryption keys

## Front Door & CDN Patterns

### Custom Domain Handling
Uses normalized domain names: `replace(parameters('customDomainName'), '.', '-')`

### Managed Certificate Integration
Automatic SSL certificates with naming pattern:
`{profileName}/0--{uniqueString}-{normalizedDomainName}`

### Compression Configuration
Extensive MIME type compression for web performance optimization.

## Deployment Considerations

### Resource Dependencies
Templates use proper ARM dependency chains:
1. Storage Account → Blob Containers
2. Cosmos DB Account → SQL Database
3. App Service Plan → Web Apps → Configuration

### Health Check Configuration
Publisher/API services include health check endpoint: `/Identity/Account/Login`

### Version Management
Template version: `10.13.0.0` indicates active development with semantic versioning.

## Development Workflow

When modifying templates:
1. Test with `temp.json` for Front Door changes
2. Use resource group-specific deployment for isolation
3. Validate conditional logic with different architecture parameters
4. Check WAF policy references for production deployments

## External Dependencies
- Docker Hub images: `toiyabe/cosmos-*` container registry
- External WAF policy in `RG-ADMIN` resource group
- Documentation site: `https://cosmos.moonrise.net/install`