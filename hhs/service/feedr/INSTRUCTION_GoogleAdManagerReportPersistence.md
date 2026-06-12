# Instruction: Google Ad Manager Report Response Persistence Architecture

## Project Context
- **Project**: FeedR Service
- **Current State**: Google Ad Manager report responses are sent to console only
- **Source Data**: `ExamplreGoogleAdmResponse.json` contains the response from Google Ad Manager based on `CreateReportRequest` with defined Report Definitions
- **Objective**: Design and implement a two-tier persistence layer to store Google Ad Manager responses in MongoDB (bulk storage) and then derive normalized, per-client, per-day data to PostgreSQL using Entity Framework

---

## Overall Goal
Design and implement a complete two-tier data persistence architecture:
1. **MongoDB**: Bulk storage of raw Google Ad Manager API responses (as list of entities in a single record)
2. **PostgreSQL**: Normalized, derived storage with Entity Framework for per-client, per-day aggregation (daily totals and averages)

This includes creating mappers and repositories for both databases, and providing an API for date-range and client-based report retrieval.

---

## Data Structure & Requirements

### 1. Customer Configuration Structure

#### 1.1 Network Definitions
- **Multiple Networks**: Each customer configuration can have multiple network definitions
- **Current State**: 1 network per configuration
- **Maximum Capacity**: Up to 10 networks per configuration
- **Consideration**: Determine if a separate DB entity is required for network definitions or if they should be embedded

#### 1.2 AdUnit Hierarchy
The AdUnit structure follows a hierarchical relationship:
- **Level 1: AdUnitIdTopLevel**
  - Multiple `AdUnitIdTopLevel` values can exist per network
  - Each `AdUnitIdTopLevel` serves as a grouping mechanism
  
- **Level 2: AdUnitId**
  - Multiple `AdUnitId` values can be associated with each `AdUnitIdTopLevel`
  - Each `AdUnitId` belongs to a different client
  - The relationship: `Network → AdUnitIdTopLevel → AdUnitId (Client-specific)`

### 2. Response Data Structure

#### 2.1 Data Content
The Google Ad Manager response includes:
- **Daily Totals**: Aggregatable fields that can be summed
- **Average Values**: Fields that represent averages and require average calculation

#### 2.2 Storage Strategy
- **Bulk Storage**: Store the response as a list of entities in a single row/record
- **Per-Client, Per-Day**: Maintain separate entries for each client per day
- **Database**: MongoDB fro BulkStorage and PostgreSQL with Entity Framework ORM for Per-Clien and Per-Day. This should be derived from Bulk Storage

---

## Persistence Layer Design Requirements

### 3. Two-Tier Data Storage Architecture

#### 3.1 Stage 1: MongoDB - Bulk Response Storage
**Purpose**: Initial ingestion and bulk storage of raw Google Ad Manager responses

**MongoDB Collections/Documents**:
1. **RawGoogleAdManagerResponse Collection**
   - Stores complete raw Google Ad Manager API response as a single document/record
   - Contains list of entities from a single report request
   - Fields:
     - `_id`: MongoDB ObjectId
     - `responseData`: Raw JSON response (entire response as list of entities)
     - `requestId`: Unique request identifier linking to CreateReportRequest
     - `timestamp`: When response was received
     - `processedStatus`: Flag indicating whether this response has been derived to PostgreSQL
     - `metadata`: Additional tracking information

**MongoDB Storage Strategy**:
- Each Google Ad Manager report response is stored as a complete bulk record
- No normalization at this stage - responses stored as-is
- Serves as audit trail and data warehouse for raw data
- Acts as intermediate staging layer before processing

#### 3.2 Stage 2: PostgreSQL - Derived Per-Client, Per-Day Storage
**Purpose**: Processed, normalized storage for querying and API retrieval

**Entity Framework Entities Required**:
1. **Network Entity** (PostgreSQL)
   - Relationship to customer configuration
   - Store up to 10 networks per configuration
   - Mapped via Entity Framework

2. **AdUnitTopLevel Entity** (PostgreSQL)
   - Relationship to Network
   - Multiple instances per network
   - Mapped via Entity Framework

3. **AdUnitId/Client Entity** (PostgreSQL)
   - Relationship to AdUnitTopLevel
   - Client identifier/reference
   - Multiple instances per AdUnitTopLevel
   - Mapped via Entity Framework

4. **DailyReportResponse Entity** (PostgreSQL)
   - **Derived from**: MongoDB RawGoogleAdManagerResponse
   - **Granularity**: One record per client (AdUnitId) per day
   - **Composite Key**: (ClientId/AdUnitId, Date)
   - **Fields**:
     - AdUnitId (foreign key to Client Entity)
     - Date (daily aggregation date)
     - AggregatedTotals: Summed values for aggregatable fields
     - AverageValues: Calculated averages for average fields
     - RawMetrics: JSON column storing detailed breakdown if needed
     - SourceMongoDbId: Reference to source document in MongoDB
     - CreatedDate: When record was derived
     - UpdatedDate: When record was last updated

5. **MongoDbToPostgresMapping Entity** (PostgreSQL)
   - Tracks the derivation process
   - Fields:
     - MongoDbDocumentId
     - PostgresRecordIds (list of derived records)
     - DerivationDate
     - DerivationStatus

#### 3.3 Data Flow Between Layers
```
Google Ad Manager API 
    ↓
RawGoogleAdManagerResponse (MongoDB - Bulk Storage)
    ↓
Mapper/Transformer (Parse & Aggregate)
    ↓
DailyReportResponse (PostgreSQL - Per-Client, Per-Day)
```

#### 3.4 Data Aggregation Fields (PostgreSQL)
- **Aggregatable Fields**: Store cumulative daily totals for each client
- **Average Fields**: Store calculated daily average values for each client
- **Metadata**: Optional JSON column for storing raw metrics or breakdowns

---

## API Requirements

### 4. API Endpoint Specification

#### 4.1 Report Data Retrieval Endpoint
- **Endpoint Purpose**: Retrieve report data by client (AdUnitId) and date range
- **Input Parameters**:
  - `AdUnitId`: Client identifier
  - `StartDate`: Date range start
  - `EndDate`: Date range end
  
- **Output Data**:
  - **For Aggregatable Fields**: Return sum totals for the date range
  - **For Average Fields**: Return average values for the date range
  - **Grouping**: Group results appropriately (by day or period)

---

## Mapper Requirements

### 5. Data Mapping & Transformation Layer

#### 5.1 MongoDB → PostgreSQL Mapper
- **Input**: Raw Google Ad Manager response documents from MongoDB
- **Processing Steps**:
  1. Parse JSON response from MongoDB document
  2. Identify client/AdUnitId associations
  3. Group data by client and date
  4. Calculate daily totals for aggregatable fields
  5. Calculate daily averages for average fields
  6. Normalize data into PostgreSQL entity format

#### 5.2 Field Transformation Rules
- **Aggregatable Fields** (Sum Operation):
  - Identify fields that should be summed daily
  - Accumulate values across all rows for each client per day
  - Store in `AggregatedTotals` column
  
- **Average Fields** (Average Operation):
  - Identify fields that represent averages
  - Calculate average values across all rows for each client per day
  - Store in `AverageValues` column

#### 5.3 Data Validation
- Validate MongoDB source data integrity
- Check for missing or malformed fields
- Ensure AdUnitId to Client mapping exists
- Handle data type conversions

#### 5.4 Mapper Output
- Generate Entity Framework entity objects ready for PostgreSQL insertion
- Create mapping records in `MongoDbToPostgresMapping` for tracking
- Maintain referential integrity with Network, AdUnitTopLevel, and AdUnitId entities

---

## Database Repository Requirements

### 6. Repository Layer (Dual-Database Architecture)

#### 6.1 MongoDB Repository Operations

##### 6.1.1 Write Operations (Bulk Storage)
- **InsertRawResponse**: Insert complete Google Ad Manager response as a single bulk document
- **BulkInsertResponses**: Insert multiple raw responses in a single operation
- **UpdateProcessedStatus**: Mark response as processed/derived to PostgreSQL
- **Transactional Integrity**: Ensure complete response is stored atomically

##### 6.1.2 Read Operations (MongoDB)
- **GetUnprocessedResponses**: Retrieve responses not yet derived to PostgreSQL
- **GetResponseById**: Retrieve specific raw response by MongoDB ObjectId
- **GetResponseByRequestId**: Retrieve responses by CreateReportRequest ID
- **GetResponsesByDateRange**: Retrieve raw responses within date range (for reprocessing)

#### 6.2 PostgreSQL Repository Operations (Entity Framework)

##### 6.2.1 Write Operations (Derived Storage)
- **InsertDailyReportBulk**: Insert processed daily report data for multiple clients
- **UpsertDailyReport**: Insert or update existing daily report (by AdUnitId + Date composite key)
- **CreateMappingRecord**: Link MongoDB source to PostgreSQL derived records
- **Transactional Integrity**: Ensure referential integrity across Network, AdUnitTopLevel, AdUnitId, and DailyReportResponse tables

##### 6.2.2 Read Operations (Retrieval API)
- **GetByClientAndDateRange**: 
  - Input: AdUnitId, StartDate, EndDate
  - Output: List of DailyReportResponse records
  - Apply aggregation functions (sum/average)
  
- **GetByNetworkAndDateRange**:
  - Retrieve aggregated data across all clients in a network
  
- **GetDailyByClient**:
  - Retrieve specific daily report for a client
  
- **GetAggregatedTotals**:
  - Query: Sum aggregatable fields for date range and client
  
- **GetAverageMetrics**:
  - Query: Calculate/retrieve average fields for date range and client

#### 6.3 Data Derivation Workflow
1. **Monitor MongoDB**: Listen for new unprocessed responses
2. **Trigger Mapper**: Call mapper for each unprocessed response
3. **Transform Data**: Apply aggregation and normalization rules
4. **Insert to PostgreSQL**: Bulk insert derived DailyReportResponse records
5. **Update Tracking**: Mark response as processed in MongoDB, create mapping record
6. **Error Handling**: Log failures, retry logic for failed derivations

---

## Migration Path

### 7. Current vs. Target State
- **Current State**: Google Ad Manager responses sent to console only
- **Target State**: Two-tier persistence with full API-based retrieval
  
**Implementation Stages**:
1. **Stage 1**: Implement MongoDB bulk storage for raw responses (replaces console output)
2. **Stage 2**: Implement mapper/transformer for data aggregation and normalization
3. **Stage 3**: Implement PostgreSQL storage with Entity Framework entities
4. **Stage 4**: Implement derivation workflow (MongoDB → PostgreSQL)
5. **Stage 5**: Implement API endpoint for date-range and client-based retrieval

---

## Key Considerations & Design Decisions

### 8. Questions to Address During Implementation

#### 8.1 MongoDB Design Questions
1. **MongoDB Collection Schema**
   - Should response be stored as single BSON document or multiple sub-documents?
   - How to handle very large responses?
   - Indexing strategy for querying unprocessed responses?

2. **MongoDB Connection & Scaling**
   - Connection pooling and timeout configuration?
   - Sharding strategy if data volume grows?

3. **Data Retention Policy**
   - How long to retain raw responses in MongoDB?
   - Archive strategy for old data?

#### 8.2 PostgreSQL Design Questions
1. **Network Entity Design**
   - Should networks be separate entities or embedded in configuration?
   - What fields/properties should a network entity contain?

2. **AdUnit Relationship Modeling**
   - How to best represent the hierarchy: Network → AdUnitTopLevel → AdUnitId?
   - Should there be a separate mapping table?
   - Foreign key constraints strategy?

3. **DailyReportResponse Storage Strategy**
   - Composite key on (AdUnitId, Date) to ensure one record per client per day?
   - How to handle late-arriving data for a previous day? (Update/merge vs replace)
   - Store aggregated totals and averages in separate columns or JSON?

4. **Data Derivation Strategy**
   - Near real-time derivation vs batch processing?
   - How to handle failed derivations and retries?
   - Idempotency: What if same MongoDB response is processed twice?

#### 8.3 Integration & Performance Questions
1. **Performance Optimization**
   - Indexing strategy for AdUnitId and date queries on DailyReportResponse?
   - Partitioning strategy if data volume grows (date-based partitioning)?
   - Query optimization for date-range aggregations?

2. **Consistency Between Layers**
   - How to ensure data consistency if MongoDB and PostgreSQL operations fail partially?
   - Compensation/rollback logic?

3. **Monitoring & Observability**
   - How to track successful/failed derivations?
   - Logging strategy for troubleshooting?

---

## Implementation Scope

### 9. Deliverables Expected
1. **MongoDB Layer**:
   - MongoDB connection and configuration
   - RawGoogleAdManagerResponse collection schema
   - MongoDB repository with bulk insert and query operations
   
2. **PostgreSQL Layer**:
   - Entity Framework entity definitions (Network, AdUnitTopLevel, AdUnitId, DailyReportResponse, MongoDbToPostgresMapping)
   - Entity Framework DbContext configuration
   - Database migrations for PostgreSQL

3. **Mapper & Transformer**:
   - Mapper class to transform MongoDB documents to PostgreSQL entities
   - Aggregation logic (sum and average calculations)
   - Data validation rules

4. **Repository Layer**:
   - MongoDB repository for raw response storage
   - PostgreSQL repository for derived data queries and updates
   - Data derivation workflow orchestrator

5. **API Endpoint**:
   - Endpoint for client-based, date-range-filtered report retrieval
   - Response aggregation (totals and averages)
   - Error handling and validation

6. **Integration & Tracking**:
   - Derivation process monitoring
   - MongoDbToPostgresMapping implementation for audit trail
   - Error handling and retry logic for failed derivations

### 10. Testing Considerations
- **MongoDB Tests**:
  - Unit tests for MongoDB repository (insert, query, update operations)
  - Integration tests with MongoDB container (TestContainers)
  - Tests for bulk insert performance
  - Tests for unprocessed response queries

- **PostgreSQL Tests**:
  - Unit tests for Entity Framework repository operations
  - Integration tests with PostgreSQL container (TestContainers)
  - Tests for daily aggregation logic
  - Tests for date-range queries with sum/average calculations

- **Mapper Tests**:
  - Unit tests for data transformation logic
  - Tests for aggregation calculations (sum, average)
  - Tests for field mapping accuracy
  - Tests for error handling and validation

- **API Endpoint Tests**:
  - Integration tests for report retrieval endpoint
  - Tests with various date ranges
  - Tests with different client/AdUnitId combinations
  - Tests for aggregation accuracy

- **End-to-End Tests**:
  - Full flow from Google Ad Manager response → MongoDB → Mapper → PostgreSQL
  - Tests for data consistency between layers
  - Tests for failed derivation recovery

---

## Reference Files
- **Response Example**: `/Users/serdar.cakir/ws/org/tech-summus/hhs/service/feedr/.vscode/ExamplreGoogleAdmResponse.json`
- **Request Definition**: Refer to `CreateReportRequest` in the feedr project for expected field mappings

---

## Notes for Implementing Agent
- This instruction assumes a two-tier database architecture:
  - **MongoDB**: Primary storage for bulk raw Google Ad Manager responses (audit trail/data warehouse)
  - **PostgreSQL**: Secondary storage for normalized, queryable per-client, per-day aggregated data (operational database)
  
- **Critical Flow**: Raw Response (Google Ad Manager) → MongoDB (Bulk) → Mapper/Transformer → PostgreSQL (Derived, normalized)

- The mapper/derivation layer is the critical bridge between the two databases and must handle:
  - Data transformation and normalization
  - Daily aggregation (sum for totals, average for metrics)
  - Proper tracking and idempotency

- PostgreSQL uses Entity Framework with composite keys (AdUnitId, Date) on DailyReportResponse to ensure one record per client per day

- MongoDB serves as the source of truth for raw responses; derivation failures should not delete MongoDB data

- Consider implementing a derivation scheduler/background job to continuously process unprocessed MongoDB responses

- The two-tier approach provides:
  - **Flexibility**: MongoDB for flexible schema and bulk storage
  - **Queryability**: PostgreSQL for efficient, normalized queries and aggregations
  - **Auditability**: MongoDB maintains complete response history
