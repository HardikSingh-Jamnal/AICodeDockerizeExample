# Centralized Search Platform: Master Implementation Guide

This document is the **definitive manual** for building the Centralized Search Platform. It provides in-depth instructions, configuration details, and architectural rationale for every component.

---

## Phase 1: Infrastructure & Environment Strategy

### Step 1: Containerized Environment (Docker)
We use Docker to ensure the development environment matches production.

**Detailed Configuration (`docker-compose.yml`)**:
1.  **Elasticsearch (v8.x)**:
    *   *Config*: Set `xpack.security.enabled=false` for local dev to avoid SSL certificate complexity initially.
    *   *Resource Limits*: `mem_limit: 1g` (ES is memory hungry).
    *   *Volume*: Bind mount `./es-data:/usr/share/elasticsearch/data` to persist indices across restarts.
2.  **Kibana (v8.x)**:
    *   *Port*: 5601.
    *   *Dependency*: `depends_on: elasticsearch`.
    *   *Purpose*: Used for verifying data ingestion and testing raw search queries before writing API code.
3.  **RabbitMQ (3-management)**:
    *   *Port*: 5672 (App), 15672 (Management UI).
    *   *Purpose*: Decouples the "Source" (mock services) from the "Sink" (Search DB). This ensures that if Elasticsearch goes down, we don't lose data; messages just pile up in the queue.

**Why this matters**: Isolating dependencies ensures "it works on my machine" applies to everyone.

---

## Phase 2: The "Brain" - Search Engine Configuration

### Step 2: Optimal Index Creation
We don't just "dump" data. We configure the index for *speed* and *accuracy*.

**The "Edge N-gram" Strategy**:
*   *Problem*: User types "Tes" and wants to see "Tesla". Standard search needs full words.
*   *Solution*: `edge_ngram` tokenizer breaks "Tesla" into `[T, Te, Tes, Tesl, Tesla]`.
*   *Implementation*:
    *   Create a custom analyzer named `autocomplete` using this tokenizer.
    *   Apply it to `title`, `model`, `make`.
    *   **Search Analyzer**: Use standard `lowercase` analyzer at query time (don't n-gram the query itself, or you get too many matches).

**Strict Mapping**:
*   *Config*: `"dynamic": "strict"`.
*   *Reason*: If a developer accidentally sends a huge blob of text into a field meant for dates, default ES tries to guess. Strict mapping throws an error, preventing index pollution.

---

## Phase 3: The "Feeder" - Ingestion Pipeline

### Step 3: Event Bus Topology
We use a **Topic Exchange** model in RabbitMQ.

*   **Exchange Name**: `domain.events` (Topic Type).
*   **Routing Keys**: `domain.entity.action` (e.g., `offer.sales.created`, `transport.logistics.updated`).
*   **Search Queue**: `q_search_ingestion`.
*   **Binding**: Bind `q_search_ingestion` to `domain.events` with pattern `#` (listen to everything initially) or specific keys `*.created`, `*.updated`.

### Step 4: The Ingester Worker (Node.js/TypeScript)
This is a critical background service.

**Detailed Logic Flow**:
1.  **Connect & Prefetch**: Connect to RabbitMQ. Set `channel.prefetch(10)` to process 10 messages in parallel (adjust based on load).
2.  **Message Parsing**:
    *   Receive JSON payload.
    *   Validate schema (using Zod or Joi) to ensure required fields (`id`, `type`) exist.
3.  **Transformation (The "Anti-Corruption Layer")**:
    *   Convert specific domain events into the generic `SearchDocument` format.
    *   *Example*: Calculate `visible_to_roles`. If `status == 'pending'`, `visible_to = ['admin']`. If `status == 'active'`, `visible_to = ['admin', 'public']`.
4.  **Idempotent Upsert**:
    *   **Action**: Use Elasticsearch `Index` API with explicit ID (not auto-generated ID).
    *   *Why?* If we receive the same "Update" event twice, or process "Created" then "Updated", writing to the same ID simply overwrites the latest state. It guarantees eventual consistency without complex lock mechanisms.
5.  **Ack/Nack Strategy**:
    *   **Success**: `channel.ack(msg)`.
    *   **Transient Failure** (e.g. ES timeout): `channel.nack(msg, requeue=true)`.
    *   **Permanent Failure** (e.g. JSON parse error): `channel.nack(msg, requeue=false)` (Dead Letter).

---

## Phase 4: The "Face" - Search API Development

### Step 5: API Layer Construction
Technically, this is a proxy that adds security.

**Endpoint Design**: `POST /search` (POST is better than GET for complex filter payloads) or `GET` with complex query params.

**Security Middleware Deep Dive**:
1.  Intercept Request.
2.  **JWT Decode**: Get `userId` and `roles` (e.g., `["SELLER"]`).
3.  **Context Injection**: Attach these to the request object `req.user`.

**The Query Builder (The "Secret Sauce")**:
We construct a generic Elasticsearch Bool Query.

```json
{
  "query": {
    "bool": {
      "must": [
        { "multi_match": { "query": "user input", "fields": ["title", "description"] } }
      ],
      "filter": [
        // SECURITY FILTERS (Injected by server, User cannot touch these)
        { 
          "bool": {
            "should": [
               // Simulating logic: "Show if Public OR I own it"
               { "term": { "visible_to_roles": "PUBLIC" } },
               { "term": { "owner_id": "current_user_123" } }
            ]
          }
        },
        // USER FILTERS (from frontend)
        { "term": { "entity_type": "OFFER" } }
      ]
    }
  }
}
```

**Why this is Secure**: The user can send whatever filters they want in the body, but the `must` and `security filter` sections are hardcoded by the backend based on their trusted token.

---

## Phase 5: Reliability & Day 2 Operations

### Step 6: Handling "The Gap" (Race Conditions)
*   *Scenario*: User updates Offer price -> Redirects to Search Page. Search index hasn't updated yet (takes 1 sec). User sees old price.
*   *Fix*:
    1.  **Frontend Optimistic UI**: Show the new price immediately based on local state.
    2.  **Write-Wait**: The API that updates the SQL DB can optionally wait for the RabbitMQ Ack before returning "Success" to UI (though full consistency takes time).

### Step 7: Bulk Re-indexing
*   *Requirement*: You change the index mapping (e.g., add a new analyzer).
*   *Procedure*:
    1.  Create `index_v2`.
    2.  Run a "Re-index Script" that pulls all data from SQL DBs and pushes to `index_v2` via the Ingestion Pipeline.
    3.  Switch Alias `platform_search` to point to `index_v2`.
    4.  Delete `index_v1`.
    *   *Benefit*: Zero downtime updates.

---

## Phase 6: Frontend Integration Strategy

### Step 8: The "Uni-Search" Component
*   **Debounce**: Implement a 300ms debounce on the input.
*   **Stale Requests**: Use `AbortController` in `fetch/axios`.
    *   *Why?* User types "A" (Req 1), then "Ap" (Req 2). Req 1 might finish *after* Req 2. We must cancel Req 1 so the UI doesn't flicker back to "A" results.
*   **Highlighting**: Use Elasticsearch `highlight` feature to return snippets: `...Tesla <em>Model</em> 3...`. Display this HTML safely in the UI.

---

## Summary of Success Considerations
1.  **Latency**: Aim for < 100ms search response. (ES is usually < 20ms).
2.  **Security**: Never trust client IDs. Always derive identity from JWT.
3.  **Observability**: Log every "Zero Result" query. It helps identify what users are looking for but not finding (business opportunity).
