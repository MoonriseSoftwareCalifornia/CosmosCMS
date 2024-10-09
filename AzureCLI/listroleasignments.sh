#!/bin/bash
echo "What is the resource group name?"
read resourceGroup
echo "What is the Cosmos DB account name?"
read cosmosAccountName

az cosmosdb sql role assignment list \
    --resource-group $resourceGroup \
    --account-name $cosmosAccountName  \