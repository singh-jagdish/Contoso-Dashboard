# Azure Document Scan Adapter

The training application uses `LocalDocumentScanQueue`, `LocalDocumentScanWorker`, and `LocalMalwareScanner` so it remains offline. A production deployment should replace the queue publisher with Azure Storage Queue and deploy an Azure Function using a Queue Storage trigger.

The message contains `documentId`, `scanJobId`, and `requestedAtUtc` only. The function must load the authoritative private storage path from the database, use managed identity or an approved secret store, invoke the production malware scanner, update the document status, and move exhausted messages to a poison queue. The handler must remain idempotent because queue delivery can repeat.
