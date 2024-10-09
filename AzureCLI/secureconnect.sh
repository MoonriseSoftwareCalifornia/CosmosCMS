#!/bin/bash
echo "What is the resource group name?"
read resourceGroup
echo "What is the Cosmos DB account name?"
read cosmosAccountName
echo "What is the Object ID of your Editor?"
read editorPrincialId
echo "What is the Object ID of your Publisher?"
read publisherPrincipalId
cid=$(az cosmosdb show --resource-group $resourceGroup --name $cosmosAccountName --query {id:id} -o tsv)
# Data Contributor role for the editor (RW)
az cosmosdb sql role assignment create \
    --resource-group $resourceGroup \
    --account-name $cosmosAccountName  \
    --role-definition-name "Cosmos DB Built-in Data Contributor" \
    --principal-id $editorPrincialId \
    --scope "/"
# Data Reader role for the publisher (RW)
az cosmosdb sql role assignment create \
    --resource-group $resourceGroup \
    --account-name $cosmosAccountName  \
    --role-definition-name "Cosmos DB Built-in Data Reader" \
    --principal-id $publisherPrincipalId \
    --scope "/"