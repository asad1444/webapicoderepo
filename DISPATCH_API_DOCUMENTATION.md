# 📋 Dispatch Controller - API Documentation

## Base URL
```
/api/Dispatch
```

---

## 📑 Endpoints Overview

| # | Method | Endpoint | Purpose |
|---|--------|----------|---------|
| 1 | GET | `/tickets` | Get all jobs (Admin Dashboard) |
| 2 | POST | `/tickets` | Create new job |
| 3 | GET | `/{jobId}/matching-companies` | Get zone-matched companies for job |
| 4 | POST | `/bids` | Submit bid on job |
| 5 | POST | `/{jobId}/award` | Award job to company |

---

## 1️⃣ Get All Tickets (Jobs)

### Request
```http
GET /api/Dispatch/tickets
```

### Purpose
Admin dashboard mein saari jobs ka grid dikhane ke liye

### Response
```json
[
  {
    "time": "2026-08-26T14:30:00",
    "clientRequest": "Split AC 1.5 Ton",
    "kindOf": "Not cooling properly",
    "crn": "CRN-20260826-1",
    "crid": "CR-123456",
    "category": "AC Repair",
    "status": "Open",
    "jobID": 1
  }
]
```

### Grid Columns
- **Time**: Job created date/time
- **Client Request**: Asset name (e.g., "Split AC 1.5 Ton")
- **Kind Of**: Issue description
- **CRN**: Auto-generated CR Number
- **CRID**: Client Reference ID
- **Category**: Job category
- **Status**: Current job status
- **JobID**: Unique job identifier

### Status Values
- `Open` - Available for bidding
- `Dispatched` - Assigned to company
- `Pending` - Awaiting technician assignment
- `In Progress` - Technician working
- `Completed` - Job finished

---

## 2️⃣ Create New Ticket (Job)

### Request
```http
POST /api/Dispatch/tickets
Content-Type: application/json
```

### Body
```json
{
  "clientID": 1,
  "category": "AC Repair",
  "clientRequest": "Split AC 1.5 Ton",
  "issueDescription": "Not cooling properly",
  "crid": "CR-123456"
}
```

### Required Fields
- `clientID` (int) - Client ka ID (must exist in Clients table)
- `category` (string) - Job category
- `clientRequest` (string) - Asset name/description
- `issueDescription` (string) - Problem details
- `crid` (string) - Client reference ID

### Auto-Generated Fields
- `crNumber` - Format: `CRN-YYYYMMDD-{count}`
  - Example: `CRN-20260826-1`
- `status` - Default: `"Open"`
- `createdAt` - Current timestamp

### Response
```json
{
  "jobID": 1,
  "clientID": 1,
  "crNumber": "CRN-20260826-1",
  "crid": "CR-123456",
  "category": "AC Repair",
  "clientRequest": "Split AC 1.5 Ton",
  "issueDescription": "Not cooling properly",
  "status": "Open",
  "createdAt": "2026-08-26T14:30:00"
}
```

### Notes
- CRN auto-generates based on current date and job count
- Job automatically goes to Live Operating Room for bidding
- Companies in matching zone will see this job

---

## 3️⃣ Get Matching Companies for Job

### Request
```http
GET /api/Dispatch/{jobId}/matching-companies
```

### Example
```http
GET /api/Dispatch/6/matching-companies
```

### Purpose
Specific job ke liye zone-matched companies dikhana (Admin can see which companies can bid)

### Matching Logic
1. Job ki client ka zone nikalta hai
2. Us zone mein **Active** status wale companies return karta hai

### Response
```json
{
  "jobZone": "Zone-A",
  "availableCompanies": [
    {
      "companyID": 1,
      "companyName": "ABC Services Ltd",
      "serviceType": "AC Services",
      "inchargePhone": "03001234567"
    },
    {
      "companyID": 2,
      "companyName": "XYZ Repair Co",
      "serviceType": "AC & Refrigeration",
      "inchargePhone": "03009876543"
    }
  ]
}
```

### Empty Result
```json
{
  "jobZone": "Zone-B",
  "availableCompanies": []
}
```

Agar koi company us zone mein nahi hai ya sab Inactive hain

---

## 4️⃣ Submit Bid

### Request
```http
POST /api/Dispatch/bids
Content-Type: application/json
```

### Body
```json
{
  "jobID": 1,
  "companyID": 1,
  "estimatedArrival": 30
}
```

### Fields
- `jobID` (int) - Job ka ID jis pe bid kar rahe hain
- `companyID` (int) - Bidding company ka ID
- `estimatedArrival` (int) - ETA in minutes (e.g., 30, 45, 60)

### Auto-Generated Fields
- `bidTime` - Current timestamp
- `status` - Default: `"Pending"`

### Response
```json
{
  "bidID": 1,
  "jobID": 1,
  "companyID": 1,
  "estimatedArrival": 30,
  "bidTime": "2026-08-26T14:35:00",
  "status": "Pending",
  "isWinner": false
}
```

### Notes
- Multiple companies can bid on same job
- 15-minute bid window protocol (not enforced in this endpoint, but in LiveOperatingRoomController)
- Better to use LiveOperatingRoomController's bid endpoint for full auto-award logic

---

## 5️⃣ Award Job to Company

### Request
```http
POST /api/Dispatch/{jobId}/award
Content-Type: application/json
```

### Body
```json
1
```
*(Body mein sirf Company ID bhejein as integer)*

### Example
```http
POST /api/Dispatch/6/award
Content-Type: application/json

1
```

Award Job 6 to Company 1

### Response
```json
{
  "message": "Job awarded successfully",
  "jobID": 6,
  "companyID": 1
}
```

### What Happens
1. **Winning Bid Updated:**
   - `isWinner = true`
   - `status = "Accepted"`

2. **Other Bids:**
   - Remain as `status = "Pending"` (not rejected in this endpoint)

3. **Job Updated:**
   - `companyID` set to winner
   - `status = "Dispatched"`
   - `assignedAt = DateTime.Now`

4. **Next Steps:**
   - Job appears in company's Won Jobs
   - Company assigns technician
   - Job status changes to "In Progress"

### Error Response
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "traceId": "..."
}
```
Job ID not found

---

## 🌐 Complete URLs (Localhost Example)

Base: `https://localhost:7296/api`

```
1. Get All Tickets:
   GET https://localhost:7296/api/Dispatch/tickets

2. Create New Ticket:
   POST https://localhost:7296/api/Dispatch/tickets

3. Get Matching Companies for Job 6:
   GET https://localhost:7296/api/Dispatch/6/matching-companies

4. Submit Bid:
   POST https://localhost:7296/api/Dispatch/bids

5. Award Job 6 to Company:
   POST https://localhost:7296/api/Dispatch/6/award
```

---

## 📱 Integration Examples

### JavaScript (Fetch API)

#### Get All Tickets
```javascript
const apiBase = 'https://localhost:7296/api';

async function getAllTickets() {
    const response = await fetch(`${apiBase}/Dispatch/tickets`);
    const tickets = await response.json();
    
    // Display in table
    tickets.forEach(ticket => {
        console.log(`${ticket.crn} - ${ticket.category} - ${ticket.status}`);
    });
}
```

#### Create New Ticket
```javascript
async function createTicket() {
    const data = {
        clientID: 1,
        category: "AC Repair",
        clientRequest: "Split AC 1.5 Ton",
        issueDescription: "Not cooling properly",
        crid: "CR-123456"
    };
    
    const response = await fetch(`${apiBase}/Dispatch/tickets`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
    
    const result = await response.json();
    console.log('Job Created:', result);
}
```

#### Get Matching Companies
```javascript
async function getMatchingCompanies(jobId) {
    const response = await fetch(`${apiBase}/Dispatch/${jobId}/matching-companies`);
    const result = await response.json();
    
    console.log(`Zone: ${result.jobZone}`);
    console.log(`Companies: ${result.availableCompanies.length}`);
}
```

#### Submit Bid
```javascript
async function submitBid(jobId, companyId, eta) {
    const data = {
        jobID: jobId,
        companyID: companyId,
        estimatedArrival: eta
    };
    
    const response = await fetch(`${apiBase}/Dispatch/bids`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data)
    });
    
    const result = await response.json();
    console.log('Bid Submitted:', result);
}
```

#### Award Job
```javascript
async function awardJob(jobId, companyId) {
    const response = await fetch(`${apiBase}/Dispatch/${jobId}/award`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(companyId) // Just company ID as integer
    });
    
    const result = await response.json();
    console.log('Job Awarded:', result);
}
```

### C# (HttpClient)

```csharp
using System.Net.Http;
using System.Text;
using System.Text.Json;

// Get All Tickets
var client = new HttpClient();
var response = await client.GetAsync("https://localhost:7296/api/Dispatch/tickets");
var tickets = await response.Content.ReadAsStringAsync();

// Create New Ticket
var newJob = new {
    clientID = 1,
    category = "AC Repair",
    clientRequest = "Split AC 1.5 Ton",
    issueDescription = "Not cooling properly",
    crid = "CR-123456"
};

var json = JsonSerializer.Serialize(newJob);
var content = new StringContent(json, Encoding.UTF8, "application/json");
var createResponse = await client.PostAsync("https://localhost:7296/api/Dispatch/tickets", content);
```

---

## 🔄 Typical Workflow

### Admin Creates Job → Companies Bid → Job Awarded

```
1. Admin creates job
   POST /api/Dispatch/tickets
   
2. Job goes "Open" and appears in Live Operating Room
   
3. Admin checks which companies can bid
   GET /api/Dispatch/{jobId}/matching-companies
   
4. Companies submit bids (via Live Operating Room)
   POST /api/Dispatch/bids
   
5. Admin awards job to best company
   POST /api/Dispatch/{jobId}/award
   
6. Job status changes to "Dispatched"
   
7. Company assigns technician
   
8. Job status changes to "In Progress"
```

---

## 🎯 Important Notes

### Zone Matching
- Endpoint #3 shows only **Active** companies in job's zone
- If no companies found, job may need zone expansion (see LiveOperatingRoomController)

### CRN Auto-Generation
- Format: `CRN-YYYYMMDD-{count}`
- Uses total job count, so CRN is unique per day
- Example: First job on Aug 26, 2026 = `CRN-20260826-1`

### Bid Window Protocol
- This controller has basic bidding
- Full 15-minute protocol with auto-award is in **LiveOperatingRoomController**
- For production, use LiveOperatingRoom endpoints

### Status Flow
```
Open → Dispatched → Pending → In Progress → Completed
```

- **Open**: Just created, available for bids
- **Dispatched**: Awarded to company, awaiting technician
- **Pending**: Technician selected (not in this controller)
- **In Progress**: Technician working
- **Completed**: Job done

---

## 🧪 Testing

Use the test page: `test-dispatch.html`

1. Open in browser
2. Configure API URL
3. Test all endpoints with one click
4. View responses in formatted tables

---

## 📚 Related Documentation

- **LiveOperatingRoomController** - Advanced bidding with auto-award
- **CompanyTechnicianController** - Technician assignment
- **DashboardController** - Admin overview stats

---

**Last Updated:** August 26, 2026  
**Version:** 1.0
