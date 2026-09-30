# 🏢 Company Authentication API Documentation

## Base URL
```
http://localhost:5186/api/company/auth
```

---

## 📋 Table of Contents
1. [Register Company](#1-register-company)
2. [Login](#2-login)
3. [Change Password](#3-change-password)
4. [Forgot Password (3 Steps)](#4-forgot-password)
5. [Get Profile](#5-get-profile)

---

## 1️⃣ Register Company

**Endpoint:** `POST /api/company/auth/register`  
**Authentication:** Not Required  
**Description:** Register a new company. Status will be "Pending" until admin approves.

### Request Body:
```json
{
  "CompanyName": "ABC Services Ltd",
  "Email": "company@example.com",
  "Password": "password123",
  "InchargeName": "Ahmed Khan",
  "InchargePhone": "03001234567",
  "City": "Karachi",
  "Zone": "North Karachi",
  "ServiceType": "HVAC & AC Services",
  "CompanyLogo": "https://example.com/logo.png" (optional)
}
```

### Response (Success - 200):
```json
{
  "success": true,
  "message": "Company registered successfully! Your account is pending admin approval.",
  "data": {
    "CompanyID": 1,
    "CompanyName": "ABC Services Ltd",
    "Email": "company@example.com",
    "Status": "Pending"
  },
  "note": "You will receive a notification once your account is approved by the admin."
}
```

### Response (Error - 400):
```json
{
  "message": "Email already registered. Please use a different email."
}
```

---

## 2️⃣ Login

**Endpoint:** `POST /api/company/auth/login`  
**Authentication:** Not Required  
**Description:** Login using Email OR Phone + Password. Returns JWT token.

### Request Body:
```json
{
  "Identifier": "company@example.com",  // OR "03001234567"
  "Password": "password123"
}
```

### Response (Success - 200):
```json
{
  "success": true,
  "message": "Login successful!",
  "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "company": {
    "CompanyID": 1,
    "CompanyName": "ABC Services Ltd",
    "InchargeName": "Ahmed Khan",
    "Email": "company@example.com",
    "Phone": "03001234567",
    "CompanyLogo": "https://example.com/logo.png",
    "City": "Karachi",
    "Zone": "North Karachi",
    "ServiceType": "HVAC & AC Services",
    "Status": "Active",
    "IsDefaultPassword": false
  },
  "statistics": {
    "TotalTechnicians": 10,
    "ActiveTechnicians": 8,
    "OnlineTechnicians": 5,
    "TotalJobs": 150,
    "CompletedJobs": 120,
    "SuccessRate": 80.0
  },
  "note": null
}
```

### Response (Error - Pending Approval):
```json
{
  "message": "Your account is pending admin approval. Please wait for approval.",
  "status": "Pending"
}
```

### Response (Error - Suspended):
```json
{
  "message": "Your account has been suspended. Please contact admin support.",
  "status": "Suspended"
}
```

### Response (Error - Invalid Credentials):
```json
{
  "message": "Invalid credentials. Please check your email/phone and password."
}
```

---

## 3️⃣ Change Password

**Endpoint:** `POST /api/company/auth/change-password`  
**Authentication:** Required (JWT Token)  
**Description:** Change password for logged-in company.

### Headers:
```
Authorization: Bearer {your_jwt_token}
```

### Request Body:
```json
{
  "CurrentPassword": "oldpassword123",
  "NewPassword": "newpassword456",
  "ConfirmPassword": "newpassword456"
}
```

### Response (Success - 200):
```json
{
  "success": true,
  "message": "Password changed successfully!",
  "CompanyID": 1,
  "CompanyName": "ABC Services Ltd"
}
```

### Response (Error - 400):
```json
{
  "message": "Current password is incorrect."
}
```

---

## 4️⃣ Forgot Password (3-Step Process)

### Step 1: Request OTP

**Endpoint:** `POST /api/company/auth/forgot-password`  
**Authentication:** Not Required  
**Description:** Request OTP for password reset. OTP sent via SMS.

#### Request Body:
```json
{
  "Identifier": "company@example.com"  // OR "03001234567"
}
```

#### Response (Success - 200):
```json
{
  "success": true,
  "message": "OTP sent successfully to your registered phone number.",
  "DebugOTP": "123456",  // ONLY in Development mode
  "SmsSent": true,
  "ExpiresIn": "15 minutes",
  "note": "Please check your SMS and enter the OTP."
}
```

---

### Step 2: Verify OTP

**Endpoint:** `POST /api/company/auth/verify-otp`  
**Authentication:** Not Required  
**Description:** Verify OTP before password reset.

#### Request Body:
```json
{
  "Identifier": "company@example.com",  // Same as Step 1
  "Otp": "123456"
}
```

#### Response (Success - 200):
```json
{
  "success": true,
  "message": "OTP verified successfully! You can now reset your password.",
  "Identifier": "company@example.com",
  "Otp": "123456"
}
```

#### Response (Error - Expired):
```json
{
  "success": false,
  "message": "OTP has expired. Please request a new one.",
  "expired": true
}
```

#### Response (Error - Invalid):
```json
{
  "success": false,
  "message": "Invalid OTP. Please check and try again.",
  "expired": false
}
```

---

### Step 3: Reset Password

**Endpoint:** `POST /api/company/auth/reset-password`  
**Authentication:** Not Required  
**Description:** Set new password using verified OTP.

#### Request Body:
```json
{
  "Identifier": "company@example.com",  // Same as previous steps
  "Otp": "123456",  // Verified OTP from Step 2
  "NewPassword": "newpassword789",
  "ConfirmPassword": "newpassword789"
}
```

#### Response (Success - 200):
```json
{
  "success": true,
  "message": "Password reset successfully! You can now login with your new password.",
  "CompanyName": "ABC Services Ltd",
  "Email": "company@example.com"
}
```

#### Response (Error - 400):
```json
{
  "message": "Invalid OTP."
}
```

---

## 5️⃣ Get Profile

**Endpoint:** `GET /api/company/auth/profile`  
**Authentication:** Required (JWT Token)  
**Description:** Get current logged-in company profile and statistics.

### Headers:
```
Authorization: Bearer {your_jwt_token}
```

### Response (Success - 200):
```json
{
  "success": true,
  "company": {
    "CompanyID": 1,
    "CompanyName": "ABC Services Ltd",
    "CompanyLogo": "https://example.com/logo.png",
    "InchargeName": "Ahmed Khan",
    "InchargePhone": "03001234567",
    "Email": "company@example.com",
    "City": "Karachi",
    "Zone": "North Karachi",
    "ServiceType": "HVAC & AC Services",
    "Status": "Active",
    "CreatedAt": "2026-08-25T10:30:00",
    "IsDefaultPassword": false
  },
  "statistics": {
    "TotalTechnicians": 10,
    "ActiveTechnicians": 8,
    "OnlineTechnicians": 5,
    "OfflineTechnicians": 5,
    "TotalJobs": 150,
    "CompletedJobs": 120,
    "PendingJobs": 15,
    "SuccessRate": 80.0
  }
}
```

---

## 🔒 Security Features

✅ **BCrypt Password Hashing** - Industry-standard encryption  
✅ **JWT Token Authentication** - Secure stateless auth  
✅ **OTP-based Password Reset** - SMS verification (15 min expiry)  
✅ **Status-based Access Control** - Pending/Active/Suspended  
✅ **Email/Phone Uniqueness** - Prevents duplicate registrations  
✅ **Password Length Validation** - Minimum 6 characters  
✅ **Token Expiry** - 7 days (configurable in appsettings.json)  
✅ **User Enumeration Prevention** - Generic error messages for forgot password

---

## 📝 Status Flow

```
Register → Status: "Pending"
   ↓
Admin Approves → Status: "Active"
   ↓
Company can Login & Access all features
   ↓
Admin can Suspend → Status: "Suspended"
```

---

## 🔧 Configuration

### JWT Settings (appsettings.json):
```json
{
  "Jwt": {
    "Key": "your-super-secret-jwt-key-min-32-characters-long",
    "Issuer": "SmartProManAPI",
    "Audience": "SmartProManClients",
    "ExpiryInDays": 7
  }
}
```

### SMS Service:
- Development: Uses `ConsoleSmsService` (logs OTP to console)
- Production: Use `TwilioSmsService` or custom SMS provider

To switch SMS provider in `Program.cs`:
```csharp
// Development (Console logging)
builder.Services.AddScoped<ISmsService, ConsoleSmsService>();

// Production (Twilio)
builder.Services.AddScoped<ISmsService, TwilioSmsService>();
```

---

## 🧪 Testing

### Using HTML Test Page:
1. Open `test-company-auth.html` in browser
2. Test all APIs visually
3. Token auto-saved in localStorage

### Using Postman/Thunder Client:
1. Import endpoints from this documentation
2. Save Token from login response
3. Add Token to Authorization header for protected endpoints

---

## ⚠️ Important Notes

1. **Remove DebugOTP in Production:**  
   The `DebugOTP` field in forgot-password response should only be visible in Development mode.

2. **Database Migration Required:**  
   Run migration to add new fields to Company table:
   ```bash
   dotnet ef migrations add AddCompanyAuthFields
   dotnet ef database update
   ```

3. **Authorization Header Format:**
   ```
   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```

4. **OTP Expiry:**  
   OTP tokens expire after 15 minutes. User must request new OTP if expired.

5. **Password Requirements:**
   - Minimum 6 characters
   - Case-sensitive
   - No special character requirements (can be added)

---

## 🚀 Next Steps

After Company Login, companies can access:
- **Live Operating Room** - `/api/company/{companyId}/LiveOperatingRoom/*`
- **Dashboard** - `/api/company/dashboard`
- **Technician Management** - `/api/company/{companyId}/technician-control-center`
- **DWR (Daily Work Reports)** - `/api/company/dwr`
- **Master Cards** - `/api/company/master-cards`

---

## 📞 Support

For API issues or questions, contact the development team.

---

**Last Updated:** August 26, 2026  
**API Version:** 1.0  
**Author:** SmartProMan Development Team
