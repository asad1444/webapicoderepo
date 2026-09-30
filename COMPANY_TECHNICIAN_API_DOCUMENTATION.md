# 🛠️ Company Technician Management API Documentation

## Base URL
```
https://localhost:7296/api/Company/{companyId}/CompanyTechnician
```

---

## 📋 Table of Contents
1. [Add Technician](#1-add-technician)
2. [List All Technicians](#2-list-all-technicians)
3. [Get Technician Details](#3-get-technician-details)
4. [Update Technician](#4-update-technician)
5. [Delete/Deactivate Technician](#5-deletedelete-technician)
6. [Reset Technician Password](#6-reset-technician-password)
7. [Get Technician Statistics](#7-get-technician-statistics)

---

## 1. Add Technician

### Endpoint
```http
POST /api/Company/{companyId}/CompanyTechnician/add
```

### Description
Company adds a new technician to their team.

### Request Body
```json
{
  "fullName": "Ali Hassan",
  "email": "ali@example.com",
  "phone": "+92-300-1234567",
  "cnic": "1234567890123",
  "designation": "Senior Technician",
  "password": "secure123",
  "confirmPassword": "secure123"
}
```

### Required Fields
- **fullName** (string, max 150 chars) - Full name of technician
- **email** (string, valid email, max 150 chars) - Unique email address
- **phone** (string, max 20 chars) - Unique phone number
- **cnic** (string, exactly 13 digits) - National ID number
- **designation** (string, max 100 chars) - Job title/designation
- **password** (string, min 6 chars) - Login password
- **confirmPassword** (string) - Must match password

### Validation Rules
✅ Email must be unique  
✅ Phone must be unique  
✅ CNIC must be exactly 13 digits  
✅ Password and Confirm Password must match  
✅ Auto-generates Technician Code: `TECH-{companyId}-0001`  
✅ Password is hashed with BCrypt  
✅ Status automatically set to "Active"  

### Success Response (200 OK)
```json
{
  "message": "Technician added successfully.",
  "technicianID": 101,
  "technicianCode": "TECH-1-0001",
  "fullName": "Ali Hassan",
  "phone": "+92-300-1234567",
  "email": "ali@example.com",
  "defaultPassword": "secure123"
}
```

### Error Responses

**400 Bad Request - Duplicate Phone**
```json
{
  "message": "A technician with this phone number already exists."
}
```

**400 Bad Request - Duplicate Email**
```json
{
  "message": "A technician with this email already exists."
}
```

**400 Bad Request - Invalid CNIC**
```json
{
  "message": "CNIC must be exactly 13 digits."
}
```

**400 Bad Request - Password Mismatch**
```json
{
  "message": "Password and Confirm Password do not match."
}
```

---

## 2. List All Technicians

### Endpoint
```http
GET /api/Company/{companyId}/CompanyTechnician/list?status={status}
```

### Description
Get all technicians for a company with optional status filter.

### Query Parameters
- **status** (optional, default: "all")
  - `all` - All technicians
  - `Active` - Active technicians only
  - `Inactive` - Inactive technicians only
  - `Pending` - Pending approval (if applicable)

### Example Request
```
GET /api/Company/1/CompanyTechnician/list?status=Active
```

### Success Response (200 OK)
```json
{
  "companyID": 1,
  "totalTechnicians": 5,
  "technicians": [
    {
      "technicianID": 101,
      "technicianCode": "TECH-1-0001",
      "fullName": "Ali Hassan",
      "email": "ali@example.com",
      "phone": "+92-300-1234567",
      "photo": null,
      "designation": "Senior Technician",
      "liveStatus": "Online",
      "approvalStatus": "Active",
      "dateOfJoining": "2026-08-26T10:30:00",
      "totalJobsSolved": 45,
      "rating": 4.5,
      "walletBalance": 15000,
      "serviceAreas": "Karachi, Lahore",
      "skills": "AC Repair, Installation",
      "createdAt": "2026-08-26T10:30:00"
    }
  ]
}
```

---

## 3. Get Technician Details

### Endpoint
```http
GET /api/Company/{companyId}/CompanyTechnician/{technicianId}
```

### Description
Get detailed information about a specific technician.

### Example Request
```
GET /api/Company/1/CompanyTechnician/101
```

### Success Response (200 OK)
```json
{
  "technicianID": 101,
  "technicianCode": "TECH-1-0001",
  "fullName": "Ali Hassan",
  "email": "ali@example.com",
  "phone": "+92-300-1234567",
  "photo": "https://example.com/photo.jpg",
  "designation": "Senior Technician",
  "liveStatus": "Online",
  "approvalStatus": "Active",
  "dateOfJoining": "2026-08-26T10:30:00",
  "serviceAreas": "Karachi, Lahore",
  "skills": "AC Repair, Installation, Maintenance",
  "licenseCertification": "HVAC Certified",
  "totalJobsSolved": 45,
  "rating": 4.5,
  "walletBalance": 15000,
  "createdAt": "2026-08-26T10:30:00"
}
```

### Error Response (404 Not Found)
```json
{
  "message": "Technician not found or does not belong to your company."
}
```

---

## 4. Update Technician

### Endpoint
```http
PUT /api/Company/{companyId}/CompanyTechnician/{technicianId}/update
```

### Description
Update technician information (all fields optional).

### Request Body
```json
{
  "fullName": "Ali Hassan Updated",
  "email": "ali.new@example.com",
  "phone": "+92-301-7654321",
  "designation": "Lead Technician",
  "serviceAreas": "Karachi, Lahore, Islamabad",
  "skills": "AC Repair, Installation, Maintenance, Troubleshooting",
  "licenseCertification": "HVAC Certified Level 2"
}
```

### Optional Fields
- **fullName** (string)
- **email** (string) - Must be unique
- **phone** (string) - Must be unique
- **designation** (string)
- **serviceAreas** (string)
- **skills** (string)
- **licenseCertification** (string)

### Success Response (200 OK)
```json
{
  "message": "Technician updated successfully.",
  "technicianID": 101,
  "fullName": "Ali Hassan Updated"
}
```

### Error Responses

**400 Bad Request - Duplicate Email**
```json
{
  "message": "Email already exists."
}
```

**400 Bad Request - Duplicate Phone**
```json
{
  "message": "Phone number already exists."
}
```

---

## 5. Delete/Deactivate Technician

### Endpoint
```http
DELETE /api/Company/{companyId}/CompanyTechnician/{technicianId}/delete
```

### Description
Soft delete (deactivate) a technician. Sets status to "Inactive" and live status to "Offline".

### Example Request
```
DELETE /api/Company/1/CompanyTechnician/101/delete
```

### Success Response (200 OK)
```json
{
  "message": "Technician deactivated successfully."
}
```

### Error Responses

**400 Bad Request - Active Jobs**
```json
{
  "message": "Cannot delete technician with active jobs. Please reassign or complete jobs first."
}
```

**404 Not Found**
```json
{
  "message": "Technician not found or does not belong to your company."
}
```

---

## 6. Reset Technician Password

### Endpoint
```http
POST /api/Company/{companyId}/CompanyTechnician/{technicianId}/reset-password
```

### Description
Company can reset a technician's password.

### Request Body
```json
{
  "newPassword": "newSecure123",
  "confirmPassword": "newSecure123"
}
```

### Required Fields
- **newPassword** (string, min 6 chars)
- **confirmPassword** (string) - Must match newPassword

### Success Response (200 OK)
```json
{
  "message": "Password reset successfully.",
  "technicianID": 101,
  "newPassword": "newSecure123"
}
```

### Error Responses

**400 Bad Request - Password Mismatch**
```json
{
  "message": "Passwords do not match."
}
```

**404 Not Found**
```json
{
  "message": "Technician not found or does not belong to your company."
}
```

---

## 7. Get Technician Statistics

### Endpoint
```http
GET /api/Company/{companyId}/CompanyTechnician/stats
```

### Description
Get comprehensive statistics about company technicians.

### Example Request
```
GET /api/Company/1/CompanyTechnician/stats
```

### Success Response (200 OK)
```json
{
  "companyID": 1,
  "totalTechnicians": 15,
  "activeTechnicians": 12,
  "inactiveTechnicians": 3,
  "onlineTechnicians": 8,
  "onJobTechnicians": 3,
  "offlineTechnicians": 4,
  "availableForWork": 5
}
```

### Statistics Explained
- **totalTechnicians**: All technicians (active + inactive)
- **activeTechnicians**: Status = "Active"
- **inactiveTechnicians**: Status = "Inactive"
- **onlineTechnicians**: LiveStatus = "Online"
- **onJobTechnicians**: LiveStatus = "On a Job"
- **offlineTechnicians**: LiveStatus = "Offline"
- **availableForWork**: Online but not on a job (onlineTechnicians - onJobTechnicians)

---

## 🔐 Authentication

All endpoints require company authentication. Include the JWT token in the Authorization header:

```http
Authorization: Bearer {company_jwt_token}
```

---

## 📊 Complete Workflow Example

### Step 1: Company Adds Technician
```http
POST /api/Company/1/CompanyTechnician/add
Content-Type: application/json

{
  "fullName": "Ali Hassan",
  "email": "ali@example.com",
  "phone": "+92-300-1234567",
  "cnic": "1234567890123",
  "designation": "Senior Technician",
  "password": "secure123",
  "confirmPassword": "secure123"
}
```

**Response:**
```json
{
  "message": "Technician added successfully.",
  "technicianID": 101,
  "technicianCode": "TECH-1-0001",
  "defaultPassword": "secure123"
}
```

### Step 2: View All Technicians
```http
GET /api/Company/1/CompanyTechnician/list?status=Active
```

### Step 3: Get Statistics
```http
GET /api/Company/1/CompanyTechnician/stats
```

### Step 4: Update Technician
```http
PUT /api/Company/1/CompanyTechnician/101/update
Content-Type: application/json

{
  "designation": "Lead Technician",
  "skills": "AC Repair, Installation, Advanced Troubleshooting"
}
```

### Step 5: Reset Password (if needed)
```http
POST /api/Company/1/CompanyTechnician/101/reset-password
Content-Type: application/json

{
  "newPassword": "newPass456",
  "confirmPassword": "newPass456"
}
```

---

## 🎯 Key Features

✅ **Auto-Generated Codes**: Unique technician codes like `TECH-1-0001`  
✅ **BCrypt Security**: All passwords hashed with BCrypt  
✅ **Unique Validation**: Email and phone uniqueness enforced  
✅ **Soft Delete**: Deactivate instead of permanent deletion  
✅ **Real-Time Stats**: Live statistics about technician availability  
✅ **CNIC Validation**: Ensures 13-digit Pakistani CNIC format  
✅ **Active Job Protection**: Cannot delete technicians with active jobs  

---

## 🧪 Testing

Use the provided HTML test page:
```
test-company-technician.html
```

Open in browser and test all APIs with a visual interface.

---

## 📝 Notes

1. **CNIC Format**: Must be exactly 13 digits (Pakistani National ID format)
2. **Designation**: Required field for proper technician classification
3. **Auto-Approval**: Company-added technicians are automatically approved (Status = "Active")
4. **Password Security**: Passwords are hashed with BCrypt before storage
5. **Technician Code**: Auto-generated as `TECH-{CompanyID}-{Sequential Number}`
6. **Live Status**: Automatically set to "Offline" on creation
7. **Default Password**: Technicians must change password on first login (IsDefaultPassword = true)

---

## 🚀 Integration Example (JavaScript)

```javascript
// Add Technician
async function addTechnician(companyId, technicianData) {
  const response = await fetch(
    `https://localhost:7296/api/Company/${companyId}/CompanyTechnician/add`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${companyToken}`
      },
      body: JSON.stringify(technicianData)
    }
  );
  return await response.json();
}

// Usage
const result = await addTechnician(1, {
  fullName: "Ali Hassan",
  email: "ali@example.com",
  phone: "+92-300-1234567",
  cnic: "1234567890123",
  designation: "Senior Technician",
  password: "secure123",
  confirmPassword: "secure123"
});

console.log(result);
```

---

**Last Updated**: August 26, 2026  
**Version**: 1.0  
**API Base URL**: `https://localhost:7296/api`
