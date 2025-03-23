#!/bin/bash

source ./config.sh

RESOURCE_GROUP_NAME=rg-tf-state
STORAGE_ACCOUNT_NAME=aichatbot$RANDOM
CONTAINER_NAME=tfstate

# Login
az login

# Switch to Visual Studio Subscription
az account set --subscription $ARM_SUBSCRIPTION_ID

# Create resource group
az group create --name $RESOURCE_GROUP_NAME --location centralus

# Register the Resource Provider
az provider register --namespace 'Microsoft.Storage'

# Create storage account
az storage account create --resource-group $RESOURCE_GROUP_NAME --name $STORAGE_ACCOUNT_NAME --sku Standard_LRS --encryption-services blob

# Create blob container
az storage container create --name $CONTAINER_NAME --account-name $STORAGE_ACCOUNT_NAME