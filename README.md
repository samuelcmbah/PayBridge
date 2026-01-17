# 🌉 PayBridge API

**A Standalone Payment Orchestration Service built with .NET 8 and Clean Architecture.**

PayBridge is a centralized "Payment Gateway as a Service" designed to decouple payment processing from business logic. It allows multiple applications (like my e-commerce and subscription projects) to share a single payment infrastructure.

---

## 🚀 The Motivation

While developing **SportStore** (an e-commerce app), I realized that integrating payment providers like Paystack directly into the application created tight coupling. If I wanted to build a second project, I would have to:

1. Duplicate the Paystack/Flutterwave integration logic.
2. Manage multiple webhook endpoints.
3. Rewrite security and verification logic for every new app.

**PayBridge** solves this by acting as a "Bridge." It handles the technical details of the payment provider, verifies security signatures, and notifies the originating application via a secure server-to-server callback.

---

## 🏗️ Architecture

The project is built using **Clean Architecture** to ensure that business logic remains independent of external frameworks and providers.

### 1. PayBridge.Domain

The core of the system. Contains:

- **Entities:** The `Payment` entity which tracks the lifecycle of a transaction.
- **Enums:** `PaymentStatus`, `PaymentProvider`, and `PaymentPurpose` to ensure type safety across the solution.

### 2. PayBridge.Application

The orchestration layer. Contains:

- **Interfaces:** Definitions for Repositories, Gateways, and Notification services.
- **DTOs:** Request and Response models for the API.
- **Services:** The `PaymentService` which coordinates between the database and the selected payment provider using the **Strategy Pattern**.

### 3. PayBridge.Infrastructure

The implementation layer. Contains:

- **Paystack Integration:** Handles the actual HTTP calls to Paystack and handles decimal-to-kobo conversions.
- **Security:** HMAC SHA512 verification for incoming webhooks.
- **Persistence:** Entity Framework Core implementation with `ApplicationDbContext`.

### 4. PayBridge.API

The entry point. Contains:

- **Controllers:** High-level endpoints for initializing payments.
- **Webhooks:** Dedicated endpoints to listen for provider callbacks.

---

## 🛡️ Key Technical Features

- **Metadata Handover:** We pass internal IDs to Paystack as metadata. When the webhook returns, PayBridge uses this metadata to accurately map the provider's transaction back to our internal database record.
- **HMAC Webhook Security:** PayBridge does not "trust" incoming data. Every webhook from Paystack is cryptographically verified using a secret hash to ensure it originated from the provider.
- **Provider Agnostic:** Using the **Strategy Pattern**, the system can dynamically resolve between different providers (Paystack, Flutterwave, etc.) without changing the core business logic.
- **Multi-Project Routing:** A single PayBridge instance can serve multiple frontend applications by tracking the `AppName` and `RedirectUrl` for each transaction.

---

## 🛠️ Tech Stack

- **Language:** C# / .NET 8
- **ORM:** Entity Framework Core
- **Database:** SQL Server
- **Security:** HMAC-SHA512
- **External API:** Paystack API

---

## ⚙️ Setup & Installation

### 1. Clone the repository

```bash
git clone https://github.com/your-username/PayBridge.git
cd PayBridge
```

### 2. Configure Settings

Add your keys to `PayBridge.API/appsettings.json`:

```json
{
  "Paystack": {
    "SecretKey": "sk_test_your_key",
    "PublicKey": "pk_test_your_key",
    "WebhookSecret": "your_webhook_secret"
  }
}
```

### 3. Run Migrations

Using Package Manager Console:

```powershell
Update-Database -Project PayBridge.Infrastructure -StartupProject PayBridge.API
```

### 4. Webhook Testing

Use ngrok to expose your localhost and point your Paystack Dashboard to:

```
https://your-ngrok-url/api/webhooks/paystack
```

---

## 📈 Roadmap

- [x] Initial Clean Architecture Setup
- [x] Paystack Initialization & Metadata Handover
- [x] HMAC Signature Verification
- [x] App Notification Service (Webhook Forwarding)
- [ ] Flutterwave Gateway Implementation
- [ ] Support for Recurring Subscriptions

---

## 👤 Author

**Samuel Mbah**

- LinkedIn: [www.linkedin.com/in/samuelcmbah](https://www.linkedin.com/in/samuelcmbah)
- GitHub: [@samuelcmbah](https://github.com/samuelcmbah)

---
