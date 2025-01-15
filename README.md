# Cosmos CMS
[Project Website](https://cosmos.moonrise.net) | [Documentation](https://cosmos.moonrise.net) | [Get Help](https://cosmos.moonrise.net/Support)

[![CodeQL](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/actions/workflows/codeql.yml/badge.svg)](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/actions/workflows/codeql.yml)
[![Publish Docker Images CI](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/actions/workflows/docker-image.yml/badge.svg)](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/actions/workflows/docker-image.yml)

Cosmos CMS is light weight, high performance, cloud-native web content management system that uses the best tools on the planet to create and manage web content.

## CKEditor 5

![CKEditor](ckeditor.webp)

[CKEditor](https://ckeditor.com/) is a widely-used "no-code" or [WYSIWYG](https://en.wikipedia.org/wiki/WYSIWYG) (What You See Is What You Get) HTML text editor that allows users to create and edit web content with ease, without needing to write HTML code. It is highly popular due to its robust features, including rich text formatting options, a customizable interface, and extensive documentation [1](https://trends.builtwith.com/widgets/CKEditor). CKEditor supports a wide range of plugins, enabling users to extend its functionality to meet specific needs. Its frequent updates and large community also contribute to its reliability and versatility, making it a preferred choice for developers and content creators alike [2](https://dev.to/keganblumenthal/froala-vs-ckeditor-a-duel-between-the-two-most-popular-html-editors-3igg).

## GrapesJS

![GrapesJS](grapesjs.png)

[GrapesJS](https://grapesjs.com/) is a free, open-source web builder framework designed to help developers and designers create and customize web pages and HTML templates with ease. It features a visual editor with a drag-and-drop interface, allowing users to build complex web pages without needing extensive coding knowledge [3](https://esketchers.com/grapesjs-things-to-consider-before-using-it/). GrapesJS is popular due to its flexibility, extensive customization options, and a wide range of pre-designed templates and components [4](https://www.talentica.com/blogs/grapesjs-things-to-consider-before-using-it/). It was initially developed to be integrated into Content Management Systems (CMS) to speed up the creation of dynamic templates, making it a versatile tool for both beginners and experienced developers [4](https://www.talentica.com/blogs/grapesjs-things-to-consider-before-using-it/). The ability to export designs in various formats and its active community support further contribute to its widespread adoption [3](https://esketchers.com/grapesjs-things-to-consider-before-using-it/).

## Monaco/Visual Studio Code

<image src="./CodeEditor.png" style="max-width: 380px;">

The [Monaco Editor](https://microsoft.github.io/monaco-editor/) is a powerful, open-source code editor that powers Visual Studio Code, Microsoft's popular code editor. It is designed to provide a rich editing experience with features like syntax highlighting, IntelliSense, and code navigation [5](https://snyk.io/advisor/python/monaco-editor). Monaco Editor is highly popular due to its versatility and performance, supporting a wide range of programming languages and being easily embeddable in web applications [6](https://npm-compare.com/codemirror,monaco-editor). Its robust API allows developers to customize and extend its functionality to suit specific needs, making it a preferred choice for many web-based development tools [6](https://npm-compare.com/codemirror,monaco-editor). The active community and continuous updates further enhance its reliability and appeal [5](https://snyk.io/advisor/python/monaco-editor).

Our implementation of Monaco includes a DIFF tool and Emmet Notation.

## Filerobot Image Editor

<image src="./Filerobot.png" style="max-width: 380px;">

[Filerobot](https://scaleflex.github.io/filerobot-image-editor/) Image Editor is a versatile, easy-to-use image editing tool designed to be seamlessly integrated into web applications. It allows users to perform a variety of image transformations such as resizing, cropping, flipping, fine-tuning, annotating, and applying filters with just a few lines of code [7](https://github.com/scaleflex/filerobot-image-editor). Its popularity stems from its simplicity, extensive functionality, and the ability to enhance user experience by providing powerful editing capabilities directly within web platforms [7](https://github.com/scaleflex/filerobot-image-editor). Additionally, its open-source nature and active maintenance ensure it remains up-to-date and reliable for developers [8](https://socket.dev/npm/package/filerobot-image-editor).

## Filepond File Uploader

[FilePond](https://pqina.nl/filepond/) is a versatile file upload library designed for web applications, offering a sleek and customizable interface for handling file uploads. It supports features like image previews, drag-and-drop functionality, and file validation, making it user-friendly and efficient [9](https://npm-compare.com/filepond). FilePond's popularity stems from its ease of integration, extensive customization options, and the ability to handle various file types seamlessly [9](https://npm-compare.com/filepond). Its active community and continuous updates ensure it remains a reliable and up-to-date solution for developers looking to enhance their web applications with robust file upload capabilities [9](https://npm-compare.com/filepond).

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

