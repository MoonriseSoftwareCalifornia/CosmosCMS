![Cosmos CMS Banner Image](couple-8168096_1920.png)
# Cosmos

Cosmos CMS is a modern content management system that is "out of the box" fast, open, and easy to use.  Its cloud-first design comes with built-in integration with Content Distribution Networks (CDN) and supports regional replication redundancy.

[Get Started/Free Install](https://cosmos.moonrise.net) | [Cosmos Documentation Website](https://cosmos.moonrise.net) | [Free Help Options](https://cosmos.moonrise.net/Support)

[![CodeQL](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/actions/workflows/codeql.yml/badge.svg)](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/actions/workflows/codeql.yml)
[![Publish Docker Images CI](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/actions/workflows/docker-image.yml/badge.svg)](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/actions/workflows/docker-image.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=MoonriseSoftwareCalifornia_CosmosCMS&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=MoonriseSoftwareCalifornia_CosmosCMS)

[![SonarCloud](https://sonarcloud.io/images/project_badges/sonarcloud-white.svg)](https://sonarcloud.io/summary/new_code?id=MoonriseSoftwareCalifornia_CosmosCMS)

## Free Install Options
Install Cosmos without email provider integration (can be added later):

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fcosmosassets.z22.web.core.windows.net%2FArmTemplates%2FInstallation%2Fazuredeploy-no-email.json)

### These install options require an Email service to already be installed:

| Email Service | Install Button |
| ------------- | -------------- |
| [Azure Communication Services (with Email)](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/email/send-email?tabs=windows%2Cconnection-string%2Csend-email-and-get-status-async%2Csync-client&pivots=platform-azportal)| [![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fcosmosassets.z22.web.core.windows.net%2FArmTemplates%2FInstallation%2Fazuredeploy-azurecomm.json) |
| [Twillio SendGrid](https://sendgrid.com/en-us/partners/azure) | [![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fcosmosassets.z22.web.core.windows.net%2FArmTemplates%2FInstallation%2Fazuredeploy-sendgrid.json) |
| Any SMTP Email service | [![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fcosmosassets.z22.web.core.windows.net%2FArmTemplates%2FInstallation%2Fazuredeploy-smtp.json) |

## About

Cosmos is a "decoupled"](https://en.wikipedia.org/wiki/Headless_content_management_system#Decoupled_CMS) web content management system, meaning content distribution is separated from content management. This provides significant performance and security benefits over monolithic systems of the past.

Content distribution is handled by:

* A static website backed by blob storage, where static content like CSS, JavaScript and images are served from. This is where the majority of content comes from.
* [A lightweight dynamic, ready-only API/HTML website](https://github.com/MoonriseSoftwareCalifornia/Cosmos/tree/main/WebApps/Publisher) hosts the dynamic content.

Content is managed by a [separate web application](https://github.com/MoonriseSoftwareCalifornia/Cosmos/tree/main/WebApps/Editor) that uses:

* [CKEditor](https://www.cosmoswps.com/cosmos/documentation/creating_content/live_editor) for easy WYSIWYG or ["live" editing](https://www.cosmoswps.com/cosmos/documentation/creating_content/live_editor)
* Online version of VS Code called ["Monaco"](https://microsoft.github.io/monaco-editor/)
* [File management tool](https://www.cosmoswps.com/cosmos/documentation/managing_files) for the static website and server-side code
* An [online image editor](https://www.cosmoswps.com/cosmos/documentation/creating_content/image_editor)

Data is stored in Azure Cosmos--a NoSQL database with regional redundancy with active-active replication.

Because Cosmos stores content in a cloud-based BLOB storage and NoSQL database, it comes with near-limitless storage capacity.

## Features

For non-technical users:
* Cosmos offers a set of user-friendly editing tools that enables you to write content and upload media files directly inside of web pages. It also has an "Auto Save" feature so you save your changes as you create.
* It also comes with a built-in image editor making quick edits possible without having to first download the image.

For web designers and engineers:
* Cosmos is an open canvas, allowing them to use their choice of UI framework.
* Back-end developers can work with .Net, NodeJS, React, Angular, Java, PHP, or Perl, users can build custom functionality in Cosmos.
* Cosmos comes with the Monaco code editor which uses the same engine as Visual Studio Code.
* It also comes with DIFF tool that enables users to compare any two versions of a web page. This tool, combined with the HTML editor's "AutoSave" feature, makes it easy for users to create and manage content efficiently.

For administrators:
* Its tight integration with Azure allows it to scale up for high performance with multi-region “hot” replicas or scale down for less demanding scenarios and operate very inexpensively.
* Cosmos is built with integration for Azure CDNs, Front Door and Web Application Firewall, and needs little to no configuration for each. 

## Contents of this repository

* The Content Editor
* The publisher
* Code libraries

