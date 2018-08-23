FROM microsoft/azure-functions-dotnet-core2.0
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT="Development"
ENV AzureWebJobsScriptRoot=/app/bin/Debug/netstandard2.0/
ENV AzureWebJobsStorage="DefaultEndpointsProtocol=https;AccountName=;AccountKey=;BlobEndpoint=https://.blob.core.windows.net/;TableEndpoint=https://.table.core.windows.net/;QueueEndpoint=https://.queue.core.windows.net/;FileEndpoint=https://.file.core.windows.net/"
ENV AzureWebJobsDashboard="DefaultEndpointsProtocol=https;AccountName=;AccountKey=;BlobEndpoint=https://.blob.core.windows.net/;TableEndpoint=https://.table.core.windows.net/;QueueEndpoint=https://.queue.core.windows.net/;FileEndpoint=https://.file.core.windows.net/"
ENV queueConnectionString="DefaultEndpointsProtocol=https;AccountName=;AccountKey=;BlobEndpoint=https://.blob.core.windows.net/;TableEndpoint=https://.table.core.windows.net/;QueueEndpoint=https://.queue.core.windows.net/;FileEndpoint=https://.file.core.windows.net/"
ENV smssamplesEventHub="Endpoint=sb://-ns.servicebus.windows.net/;SharedAccessKeyName=SendPolicy;SharedAccessKey=;EntityPath=smsflatsamples"
ENV smsEventsEventHub="Endpoint=sb://-ns.servicebus.windows.net/;SharedAccessKeyName=eventssend;SharedAccessKey=;EntityPath=smsflatevents"
ENV smsConditionsEventHub="Endpoint=sb://-ns.servicebus.windows.net/;SharedAccessKeyName=send;SharedAccessKey=;EntityPath=smsflatconditions"
ENV smsCosmosDbConnection="AccountEndpoint=https://.documents.azure.com:443/;AccountKey=;"
ENV consoleLoggingMode=always
COPY . .
