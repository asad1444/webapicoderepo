# 🚀 Live Operating Room - Quick Start Guide

## Problem: Data Not Showing?

Follow these 3 simple steps:

---

## Step 1: Open Debug Tool 🔧

Open this file in your browser:
```
debug-live-operating-room.html
```

---

## Step 2: Run Diagnostic 🔍

1. Enter your API URL (default: `https://localhost:7296/api`)
2. Enter Company ID (default: `1`)
3. Click **"Run Complete Diagnostic"**

The tool will automatically check:
- ✅ Company exists
- ✅ Clients exist  
- ✅ Jobs exist
- ✅ Zone matching
- ✅ API response

---

## Step 3: Follow Solutions 💡

The diagnostic will show you exactly what's wrong and how to fix it.

### Most Common Issues:

#### Issue #1: No Data in Database
**Solution:** Click **"Create Sample Data"** button
- Creates test company, client, and job
- All with matching zones

#### Issue #2: Zone Mismatch
**Problem:** Company Zone ≠ Client Zone

**Example:**
```
Company Zone: "Zone-A"
Client Zone:  "Zone-B"
Result: ❌ Job won't show
```

**Solution:** Click **"Check Zone Matching Logic"** to see the mismatch, then:
- Update client zone to match company zone, OR
- Change job BroadcastZoneLevel to 2 (shows to all)

#### Issue #3: Wrong Job Status
**Problem:** Jobs exist but Status ≠ "Open"

**Solution:** Update job status to "Open" in database

---

## ⚡ Quick Test Flow

### If You Have No Data:
1. Click **"Create Sample Data"** ✅
2. Click **"Run Complete Diagnostic"** ✅  
3. Click **"Test Live Operating Room API"** ✅
4. You should see 1 job!

### If You Have Data But No Jobs Showing:
1. Click **"Check Zone Matching Logic"** 🗺️
2. See which zones don't match
3. Fix zone mismatch in your database

---

## 📖 Need More Details?

See the full troubleshooting guide:
```
LIVE_OPERATING_ROOM_TROUBLESHOOTING.md
```

---

## 🎯 Expected Result

After fixing issues, when you call:
```http
GET /api/Company/1/LiveOperatingRoom/requests
```

You should get:
```json
{
  "companyZone": "Zone-A",
  "companyCity": "Karachi",
  "totalVisible": 1,  ✅ At least 1 job!
  "jobs": [
    {
      "jobID": 1,
      "crNumber": "CR-001",
      "category": "AC Repair",
      "clientZone": "Zone-A",
      "minutesRemaining": 15.0
    }
  ]
}
```

---

## 🔑 Key Points to Remember

1. **Zone Matching is Critical!**
   - Company Zone must match Client Zone (for Level 0)
   - Check zones using the debug tool

2. **Job Status Must Be "Open"**
   - Only "Open" jobs appear in Live Operating Room
   - Other statuses: Pending, In Progress, Completed

3. **Use the Debug Tool First**
   - Don't guess! Run the diagnostic
   - It will tell you exactly what's wrong

---

**Happy Debugging! 🎉**
