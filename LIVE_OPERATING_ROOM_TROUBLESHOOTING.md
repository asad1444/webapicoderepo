# 🔧 Live Operating Room - Troubleshooting Guide

## Problem: No Data Showing in Live Operating Room API

When calling `GET /api/Company/{companyId}/LiveOperatingRoom/requests` and getting empty results (`TotalVisible: 0`), follow this systematic troubleshooting guide.

---

## 🎯 Quick Diagnostic Steps

### Step 1: Use the Debug Tool
Open `debug-live-operating-room.html` in your browser and click **"Run Complete Diagnostic"**. This will automatically check:
- ✅ Company exists
- ✅ Clients exist
- ✅ Jobs exist with Status="Open"
- ✅ Zone matching between company and clients
- ✅ Live Operating Room API response

---

## 🔍 Common Issues & Solutions

### Issue 1: Company Does Not Exist
**Symptom:** API returns `404 - Company not found`

**Check:**
```http
GET /api/Companies/{companyId}
```

**Solution:**
- Create a company first using `POST /api/Companies`
- Or use a valid existing Company ID

---

### Issue 2: No Open Jobs in Database
**Symptom:** API returns `TotalVisible: 0`, but diagnostic shows "Total Jobs: 0 (Open: 0)"

**Check:**
```http
GET /api/Dispatch/tickets
```

**What to look for:**
- Are there any jobs in the database?
- Do any jobs have `Status = "Open"`?

**Solution:**
1. Create a test job using `POST /api/Dispatch/tickets`:
```json
{
  "clientID": 1,
  "category": "AC Repair",
  "clientRequest": "1.5 Ton Split AC",
  "issueDescription": "Not cooling properly",
  "crid": "CR-123456"
}
```

2. Or use the debug tool's "Create Sample Data" button

---

### Issue 3: Zone Mismatch (Most Common Issue!)
**Symptom:** Jobs exist with Status="Open", but API returns `TotalVisible: 0`

**Root Cause:** The zone matching logic filters jobs based on geographic zones:

#### Zone Matching Rules:
| Broadcast Level | Visibility Rule |
|----------------|-----------------|
| **Level 0** (Default) | Job shows ONLY if `Client.Zone == Company.Zone` (exact match) |
| **Level 1** (After 15 min) | Job shows if `Client.Area == Company.City` OR zone match |
| **Level 2** (After 30 min) | Job shows to ALL companies |

**Check Zone Matching:**
Use the debug tool's "Check Zone Matching Logic" button to see:
- Company Zone vs Job/Client Zones
- Which jobs should be visible
- Why jobs are filtered out

**Example:**
```
Company: { Zone: "Zone-A", City: "Karachi" }
Client:  { Zone: "Zone-B", Area: "Karachi" }
Job:     { BroadcastZoneLevel: 0 }

Result: ❌ Job will NOT show (Zone mismatch at Level 0)
```

**Solutions:**

**Option A: Match Zones**
- Ensure Client and Company have the same `Zone` value
- Update client zone: `PUT /api/Clients/{id}`
```json
{
  "zone": "Zone-A"  // Match with company zone
}
```

**Option B: Expand Broadcast Level**
- Manually set job to Level 1 or 2 to expand broadcast
- Update job: `PUT /api/Dispatch/tickets/{id}`
```json
{
  "broadcastZoneLevel": 2  // Shows to all companies
}
```

**Option C: Wait for Auto-Expansion**
- System automatically expands zones after bid window expires (15 minutes)
- Level 0 → Level 1 (after 15 min with no bids)
- Level 1 → Level 2 (after another 15 min with no bids)

---

### Issue 4: No Clients in Database
**Symptom:** Diagnostic shows "Total Clients: 0"

**Solution:**
Create a client first:
```http
POST /api/Clients
```
```json
{
  "clientCode": "CL-001",
  "clientName": "ABC Corporation",
  "phone": "03001234567",
  "email": "abc@example.com",
  "area": "Karachi",
  "zone": "Zone-A",
  "status": "Active"
}
```

---

### Issue 5: Jobs Have Wrong Status
**Symptom:** Jobs exist but Status is not "Open"

**Valid Job Statuses:**
- `"Open"` - Available for bidding ✅
- `"Pending"` - Assigned to company, awaiting technician
- `"In Progress"` - Technician assigned
- `"Completed"` - Job finished
- `"Cancelled"` - Job cancelled

**Solution:**
Update job status to "Open":
```http
PUT /api/Dispatch/tickets/{id}
```
```json
{
  "status": "Open"
}
```

---

## 🧪 Testing Workflow

### Complete Test Flow:

#### 1. Create Company
```http
POST /api/Companies
Content-Type: application/json

{
  "companyName": "Test Company Ltd",
  "inchargeName": "John Doe",
  "inchargePhone": "03001234567",
  "city": "Karachi",
  "zone": "Zone-A",
  "serviceType": "AC Services",
  "status": "Active",
  "email": "test@company.com"
}
```

#### 2. Create Client (SAME ZONE as Company)
```http
POST /api/Clients
Content-Type: application/json

{
  "clientCode": "CL-001",
  "clientName": "ABC Corporation",
  "phone": "03009876543",
  "email": "abc@example.com",
  "area": "Karachi",
  "zone": "Zone-A",  ⚠️ MUST MATCH COMPANY ZONE
  "status": "Active"
}
```

#### 3. Create Job for Client
```http
POST /api/Dispatch/tickets
Content-Type: application/json

{
  "clientID": 1,  // Use the ClientID from step 2
  "category": "AC Repair",
  "clientRequest": "1.5 Ton Split AC",
  "issueDescription": "Not cooling properly",
  "crid": "CR-001"
}
```

#### 4. Check Live Operating Room
```http
GET /api/Company/1/LiveOperatingRoom/requests
```

**Expected Response:**
```json
{
  "companyZone": "Zone-A",
  "companyCity": "Karachi",
  "totalVisible": 1,
  "jobs": [
    {
      "jobID": 1,
      "crNumber": "CR-001",
      "category": "AC Repair",
      "clientZone": "Zone-A",
      "zoneLevelLabel": "Zone: Zone-A",
      "minutesRemaining": 15.0,
      "bidWindowExpired": false
    }
  ]
}
```

---

## 🔬 Advanced Debugging

### Check Database Directly (if using SQL Server)

**Check Companies:**
```sql
SELECT CompanyID, CompanyName, Zone, City, Status 
FROM Companies;
```

**Check Clients:**
```sql
SELECT ClientID, ClientName, Zone, Area, Status 
FROM Clients;
```

**Check Open Jobs:**
```sql
SELECT j.JobID, j.CRID, j.Status, j.BroadcastZoneLevel,
       c.ClientName, c.Zone AS ClientZone, c.Area AS ClientCity
FROM Jobs j
LEFT JOIN Clients c ON j.ClientID = c.ClientID
WHERE j.Status = 'Open';
```

**Check Zone Matching for Company ID 1:**
```sql
SELECT 
    j.JobID,
    j.Status,
    j.BroadcastZoneLevel,
    c.Zone AS ClientZone,
    c.Area AS ClientCity,
    comp.Zone AS CompanyZone,
    comp.City AS CompanyCity,
    CASE 
        WHEN j.BroadcastZoneLevel = 0 AND c.Zone = comp.Zone THEN 'VISIBLE'
        WHEN j.BroadcastZoneLevel = 1 AND (c.Area = comp.City OR c.Zone = comp.Zone) THEN 'VISIBLE'
        WHEN j.BroadcastZoneLevel >= 2 THEN 'VISIBLE'
        ELSE 'NOT VISIBLE'
    END AS Visibility
FROM Jobs j
LEFT JOIN Clients c ON j.ClientID = c.ClientID
CROSS JOIN Companies comp
WHERE j.Status = 'Open' AND comp.CompanyID = 1;
```

---

## 📊 Zone Matching Logic Deep Dive

### Code Reference (`IsJobVisibleToCompany` method):

```csharp
private static bool IsJobVisibleToCompany(Job job, CompanyModel company)
{
    var clientZone = job.Client?.Zone;
    var clientArea = job.Client?.Area;

    return job.BroadcastZoneLevel switch
    {
        // Level 0: exact zone match
        0 => string.Equals(clientZone, company.Zone, StringComparison.OrdinalIgnoreCase),
        
        // Level 1: same city/area OR zone match
        1 => string.Equals(clientArea, company.City, StringComparison.OrdinalIgnoreCase)
          || string.Equals(clientZone, company.Zone, StringComparison.OrdinalIgnoreCase),
        
        // Level 2: all companies see it
        _ => true
    };
}
```

### Example Scenarios:

**Scenario 1: Exact Zone Match (Level 0)**
```
Company: Zone="North", City="Karachi"
Client:  Zone="North", Area="Karachi"
Job:     BroadcastZoneLevel=0
Result:  ✅ VISIBLE (Zone match)
```

**Scenario 2: Zone Mismatch (Level 0)**
```
Company: Zone="North", City="Karachi"
Client:  Zone="South", Area="Karachi"
Job:     BroadcastZoneLevel=0
Result:  ❌ NOT VISIBLE (Zone doesn't match, Level 0 requires exact match)
```

**Scenario 3: City Match (Level 1)**
```
Company: Zone="North", City="Karachi"
Client:  Zone="South", Area="Karachi"
Job:     BroadcastZoneLevel=1
Result:  ✅ VISIBLE (City/Area match at Level 1)
```

**Scenario 4: All Zones (Level 2)**
```
Company: Zone="North", City="Karachi"
Client:  Zone="South", Area="Lahore"
Job:     BroadcastZoneLevel=2
Result:  ✅ VISIBLE (Level 2 shows to everyone)
```

---

## 🛠️ Quick Fixes Using Debug Tool

### Option 1: Auto-Create Sample Data
1. Open `debug-live-operating-room.html`
2. Click **"Create Sample Data"**
3. This creates:
   - ✅ Company with Zone="Zone-A"
   - ✅ Client with Zone="Zone-A" (matching)
   - ✅ Job with Status="Open"
4. Run diagnostic again

### Option 2: Manual Testing
1. Click **"Run Complete Diagnostic"**
2. Review issues and solutions
3. Use **"Check Zone Matching Logic"** to see why jobs aren't showing
4. Fix zone mismatches in your database

---

## ✅ Verification Checklist

Before contacting support, verify:

- [ ] Company exists in database (check `GET /api/Companies/{id}`)
- [ ] At least one client exists (check `GET /api/Clients`)
- [ ] At least one job with `Status = "Open"` (check `GET /api/Dispatch/tickets`)
- [ ] Client's Zone matches Company's Zone (for Level 0 broadcast)
- [ ] Job's BroadcastZoneLevel is appropriate (0, 1, or 2)
- [ ] API base URL is correct in test page
- [ ] Company ID is correct
- [ ] HTTPS certificate is trusted (for localhost testing)

---

## 📞 Still Having Issues?

If you've followed all steps and still see no data:

1. **Check Server Logs**
   - Look for exceptions in Visual Studio output window
   - Check if API endpoints are being called

2. **Verify Database Connection**
   - Check `appsettings.json` connection string
   - Ensure migrations are applied: `dotnet ef database update`

3. **Test with Postman/Thunder Client**
   - Eliminate browser issues by testing with API client
   - Check response headers and status codes

4. **Enable Detailed Logging**
   Add to `appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.EntityFrameworkCore": "Information"
       }
     }
   }
   ```

---

## 📚 Related Documentation

- [COMPANY_AUTH_API_DOCUMENTATION.md](./COMPANY_AUTH_API_DOCUMENTATION.md) - Company authentication
- [COMPANY_TECHNICIAN_API_DOCUMENTATION.md](./COMPANY_TECHNICIAN_API_DOCUMENTATION.md) - Technician management
- `test-live-operating-room.html` - Basic testing page
- `debug-live-operating-room.html` - Advanced diagnostic tool

---

**Last Updated:** August 26, 2026  
**Version:** 1.0
