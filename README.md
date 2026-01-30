# Blackhole

![License](https://img.shields.io/github/license/satybaldin/blackhole)
![Stars](https://img.shields.io/github/stars/satybaldin/blackhole)
![Issues](https://img.shields.io/github/issues/satybaldin/blackhole)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Docker](https://img.shields.io/badge/Docker-ready-blue)
![Open Source](https://img.shields.io/badge/Open%20Source-Apache-2.0-green)

Blackhole is an open-source integration sink and simulator for distributed systems.

It provides a controlled environment for capturing, inspecting, replaying, and simulating external integrations such as SMTP, HTTP APIs, and webhooks. Blackhole is designed for local development, automated testing, and integration debugging, where interacting with real third-party services is impractical, unreliable, or unsafe.

---

## Overview

Modern backend systems rely heavily on external services:
- email providers
- third-party HTTP APIs
- webhook consumers
- authentication providers
- event buses and queues

Testing these integrations directly against real services introduces cost, flakiness, nondeterminism, and operational risk.

Blackhole addresses this problem by acting as a **boundary component** between your system and the external world. Instead of mocking integrations inside your application, Blackhole captures real outbound traffic and provides tooling to observe and simulate external behavior in a deterministic and reproducible way.

---

## Key Capabilities

- Capture outbound integration traffic
- Persist and inspect payloads and metadata
- Replay requests deterministically
- Simulate latency, failures, and retries
- Support local development, CI, and E2E testing workflows

Blackhole is protocol-agnostic by design and is structured to support multiple integration types through a consistent internal model.

---

## Supported Integrations

### SMTP
- Full SMTP sink
- Message persistence
- Header and body inspection
- Attachment capture
- HTML rendering

> Additional protocols are implemented incrementally and follow the same capture → persist → simulate lifecycle.

---

## Roadmap (Non-Exhaustive)

- HTTP / REST integration sink
- Webhook receiver and replayer
- OAuth 2.0 / OpenID Connect simulator
- Object storage (S3-compatible) mock
- Queue and event bus simulation
- Fault and latency injection
- Scenario-based integration testing
- Web-based UI for inspection and replay
- CLI and client SDKs

---

## Architecture

At a high level, Blackhole is composed of the following layers:

1. **Ingress adapters**  
   Protocol-specific listeners (SMTP, HTTP, etc.)

2. **Normalization layer**  
   Converts inbound data into a unified internal representation

3. **Persistence layer**  
   Stores requests, metadata, and artifacts for inspection and replay

4. **Simulation layer**  
   Applies deterministic or probabilistic behaviors such as retries, delays, or failures

5. **Control plane (UI / API)**  
   Provides visibility and operational control

This layered design allows new protocols to be added without impacting existing functionality.

---

## Use Cases

### Local Development
- Capture emails without sending them
- Inspect third-party API requests
- Validate integration payloads

### Automated Testing
- Deterministic integration tests
- Replay recorded interactions
- Simulate unreliable third-party behavior

### Integration Debugging
- Inspect exact outbound requests
- Reproduce production-like failures locally
- Analyze retry and timeout behavior

---

## Design Principles

- **Observability first**  
  All captured interactions are persisted and inspectable.

- **Determinism**  
  Recorded interactions can be replayed predictably.

- **Protocol isolation**  
  Each integration type is isolated behind a common abstraction.

- **Minimal coupling**  
  Blackhole integrates without requiring application-level changes.

---

## Getting Started

```bash
git clone https://github.com/<org>/blackhole
cd blackhole
docker compose up
