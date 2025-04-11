# 📦 .NET Aspire Project: Cloud Integration Sample (Web-Queue-Worker)

## 🎯 Objective

Create a sample application demonstrating how to integrate multiple cloud services using **.NET Aspire**.  
The application follows the **Web-Queue-Worker** architectural pattern, designed to support asynchronous and scalable workloads.

🔗 Reference Architecture:  
https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/web-queue-worker

## 🧱 Architecture Overview

The core components of the application include:

- **Web Frontend**: Serves client requests.
- **API Service**: Handles incoming requests, saves request metadata, and enqueues messages.
- **Message Queue**: Acts as a communication bridge between the API and the worker (Azure Storage Queue).
- **Worker (Azure Function)**: Processes long-running tasks or heavy operations asynchronously.
- **Distributed Cache (Redis)**: Stores session data and frequently accessed records for performance.
- **Database (Cosmos DB / SQL)**: Persists request history and related data.

Key characteristics:
- Both **web** and **worker** are **stateless**.
- All long-running operations are offloaded to the worker.
- Cache and queue enable **low-latency** and **resilient** handling of requests.
- The worker is **optional**—can be excluded for simpler apps without long-running logic.

---

## ✅ Current Implementation (As-Is)

- `Aspire-Worker-Queue-Cache.ApiService`:  
  A **Minimal API** project that exposes endpoints to the web frontend.  
  Depends on `ServiceDefaults` for common configuration.

- `Aspire-Worker-Queue-Cache.Web`:  
  A **Blazor Web App** configured with default .NET Aspire service settings.  
  Uses `ServiceDefaults` for telemetry and resilience.

- `Aspire-Worker-Queue-Cache.ServiceDefaults`:  
  Shared configuration project for **resilience**, **service discovery**, and **telemetry** across the solution.

- `Aspire-Worker-Queue-Cache.AppHost`:  
  The **orchestrator** project that wires all services together.  
  This should be set as the **Startup Project**.

- `Aspire-Worker-Queue-Cache.Functions`:  
  **Azure Function App** scaffolding that will serve as the worker to process queue messages and update storage.

---

## 🚀 Planned Enhancements (To-Be)

- Implement the **Azure Function worker** to:
  - Receive messages from the Azure Storage Queue.
  - Execute business logic (e.g., complete long-running tasks).
  - Update the request status in the database.
  - Invalidate or update the cache accordingly.

- Use **Azure Storage Queues** for message-based communication between API and worker.

- Leverage **Azure Cache for Redis** for storing user sessions and recently accessed data.

- Store request history in a persistent store:
  - ✅ **Cosmos DB** *(preferred for distributed and scalable workloads)*  
  - OR  
  - ✅ **Azure SQL Database** *(suitable for relational data)*

---

This prompt is structured to guide solution design, implementation, or to generate documentation/examples for integrating .NET Aspire with Azure-native services using the Web-Queue-Worker pattern.
