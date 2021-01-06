# SDK snippets (C#)

This folder contains SDK call examples that are included in the Azure Digital Twins documentation. 

## Contents

Below is a list of the request bodies that are contained in this folder, including mappings between the samples and the documents in which they appear, and descriptions of each.

| JSON request body file | Used in | Description |
| --- | --- | --- |
| deadLetterEndpoint.json | [how-to-manage-routes-apis-cli](https://docs.microsoft.com/azure/digital-twins/how-to-manage-routes-apis-cli) | Body of a request to create an endpoint with dead-lettering enabled |
| filter.json | [how-to-manage-routes-apis-cli](https://docs.microsoft.com/azure/digital-twins/how-to-manage-routes-apis-cli) | Body of a request that adds a filter to an event route |
| filter-multiple.json | [how-to-manage-routes-apis-cli](https://docs.microsoft.com/azure/digital-twins/how-to-manage-routes-apis-cli) (via include file) | Body of a request that adds multiple filters to an event route |

## Strategy

Since these are separate requests, each one is kept separately as its own file. Documents can then reference these files by name to pull in the entire snippet.