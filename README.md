# 🚗 AI-Code-Challenge: Automotive Marketplace Platform

Welcome to the **AI-Code-Challenge**, a production-grade microservices ecosystem designed for a seamless vehicle buying, selling, and transport experience. This platform demonstrates a modern event-driven architecture built with **.NET 8** and **Next.js**, showcasing an innovative **Agent-First Development Methodology**.

---

## 🏗️ Architecture Overview

The system is built on a **High-Performance Microservices Architecture**, ensuring scalability, fault tolerance, and clear domain boundaries.

### Core Services
- **Offers Service**: Manages vehicle listings, seller interactions, and offer lifecycles.
- **Purchase Service**: Handles the transaction flow between buyers and sellers.
- **Transport Service**: Coordinates vehicle logistics and shipping assignments.
- **Search Service**: A high-speed, centralized intelligence layer providing unified search capabilities across all entities using Elasticsearch.

### Integration Patterns
- **Event-Driven Communications**: Services communicate asynchronously via **RabbitMQ**, ensuring loose coupling.
- **Transactional Outbox Pattern**: Guaranteed message delivery using a dedicated Outbox processor in each service to prevent data loss.
- **Vertical Slice Architecture**: Services are organized into vertical slices (Features) utilizing **MediatR** for maximized maintainability.

---

## 🤖 Agent-Driven Development

This repository was constructed utilizing a **Multi-Agent Collaborative AI Workflow**. We leveraged specialized AI agents to handle different phases of the software development lifecycle:

| Agent Role | Responsibility | Workflow |
| :--- | :--- | :--- |
| **Product Owner** | Requirements refinement & user-centric design. | `.agent/workflows/po.md` |
| **Tech Architect** | Structural design, infrastructure & tech stack selection. | `.agent/workflows/arch.md` |
| **Developer** | Clean code implementation, unit tests & features. | `.agent/workflows/dev.md` |
| **QA Agent** | Automated verification, edge-case testing & reliability. | `.agent/workflows/qa.md` |

This "Agent-First" approach ensures architectural integrity and high-quality implementation.

---

## 🛠️ Technology Stack

### Backend (.NET 8)
- **Framework**: Minimal APIs + .NET 8.
- **Messaging**: RabbitMQ (Message Bus).
- **Search**: Elasticsearch (NEST client).
- **Database**: PostgreSQL (Entity Framework Core).
- **Validation**: FluentValidation.
- **Orchestration**: MediatR.
- **Documentation**: Swagger/OpenAPI.

### Frontend (Next.js)
- **Framework**: Next.js 14 (App Router).
- **Language**: TypeScript.
- **Styling**: Tailwind CSS.

### Infrastructure
- **Containerization**: Docker & Docker Compose.
- **Messaging**: RabbitMQ Cluster.
- **Persistence**: Per-service isolated PostgreSQL databases.

---

## 🚀 How to Run the Project

Follow these steps to get the entire ecosystem up and running:

### 1. Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running.
- [Git](https://git-scm.com/) installed.

### 2. Clone the Repository
```bash
git clone https://github.com/HardikSingh-Jamnal/AI-Code-Challenge.git
cd AI-Code-Challenge
```

### 3. Launch Services
Use Docker Compose to spin up all containers (services, databases, and message brokers):

```bash
docker-compose up -d --build
```

### 4. Verify the Deployment
Once all containers show a `healthy` status, you can access the following endpoints:

- **Web Dashboard**: [http://localhost:3000](http://localhost:3000)
- **Search API (Swagger)**: [http://localhost:5007](http://localhost:5007)
- **Offers API (Swagger)**: [http://localhost:5005](http://localhost:5005)
- **Purchase API (Swagger)**: [http://localhost:5006](http://localhost:5006)
- **Transport API (Swagger)**: [http://localhost:5004](http://localhost:5004)
- **RabbitMQ Management**: [http://localhost:15672](http://localhost:15672) (admin/password)

---

## 📦 Project Structure

```text
├── .agent/              # AI Agent Workflows
├── backend/
│   ├── Offers/          # Listing Management
│   ├── Purchase/        # Transaction Flow
│   ├── Search/          # Unified Elasticsearch Service
│   └── Transport/       # Logistics Domain
├── frontend/            # Next.js Application
├── docker-compose.yml   # Multi-Container Config
└── CodeChallenge.sln    # Solution File
```

---

## 🔍 Key Features

- **Unified Intelligence**: Real-time search across all domains with role-based filtering.
- **Guaranteed Consistency**: Outbox pattern prevents data-loss during service communication.
- **Micro-Frontend Architecture**: Modern UI designed for scalability.
- **Plug-and-Play Infrastructure**: Fully containerized environment for rapid development.
