# 📱 Mobile Provider Bill Payment System – API Project  
**SE 4458 – Software Architecture & Design of Modern Large-Scale Systems**  
**Midterm 1 – Group 1**

This repository contains the implementation of a **Mobile Provider Bill Payment System**, supporting bill querying, detailed bill retrieval, bank integrations, website bill payments, and admin operations such as adding bills (single or batch).  
The system is built using a **microservices-inspired architecture** with a **.NET 9 REST API**, a **YARP API Gateway**, and a **PostgreSQL database**.

---

## 🚀 Project Architecture

The project is deployed in **3 major components**:

### **1. API Gateway (YARP)**  
🔗 **URL:** https://mobile-billing-gateway-2kqo.onrender.com/swagger/index.html

Responsibilities:
- Reverse proxy routing to backend API  
- Request/response logging (visible in Render dashboard)  
- Rate limiting (3 bill summary calls/day per subscriber)  
- `GatewaySecret` header enforcement → API only accessible through gateway  
- Security boundary for all clients

---

### **2. Backend REST API (.NET 9)**  
🔗 **URL:** https://bill-pay-api.onrender.com/swagger/index.html 

Includes **5 controllers**:

#### **AuthController**
- Register users  
- Login to receive **JWT token**  
- Required for all protected endpoints  

#### **BankingAppController**
- Fetch all **unpaid bills** for a subscriber  

#### **MobileProviderAppController**
- Query **monthly bill summary** (rate-limited endpoint)  
- Query **detailed bill information**  
  - Includes pagination for detailed JSONB data  

#### **SubscribersController**
- Basic CRUD operations for subscribers  

#### **WebsiteController**
- Pay bills (supports partial payments)  
- Add bill manually  
- Add bills via **CSV batch upload**

---

### **3. PostgreSQL Database**
Database name: `billing-postgres`

Holds:
- Subscribers  
- Bills  
- Payments  
- Users

---

## 🧱 Data Model (ER Overview)

### **Bill**
| Column         | Type     | Description                         |
|----------------|----------|-------------------------------------|
| id             | int      | Primary key                         |
| subscriber_id  | int      | FK → Subscriber                     |
| bill_month     | date/int | Month of the bill                   |
| bill_total     | decimal  | Total bill amount                   |
| bill_details   | jsonb    | Detailed bill info (JSONB storage)  |
| is_paid        | bool     | Payment status                      |
| amount_paid    | decimal  | Sum of all payments                 |

### **Subscriber**
| Column        | Type   | Description              |
|---------------|--------|--------------------------|
| id            | int    | Primary key              |
| subscriber_no | string | Subscriber unique number |
| full_name     | string | Subscriber full name     |

### **Payment**
| Column       | Type      | Description               |
|--------------|-----------|---------------------------|
| id           | int       | Primary key               |
| bill_id      | int       | FK → Bill                 |
| amount       | decimal   | Paid amount               |
| status       | string    | Payment result            |
| payment_date | timestamp | When payment was recorded |

### **Users**
| Column       | Type      | Description               |
|--------------|-----------|---------------------------|
| id           | string       | Primary key            |
| username     | string       | name of user           |
| password_hash| string   | password in hash           |


## 📌 Implemented Features

### ✔️ Mobile Provider App
- Query **monthly bill summary**  
- Query **detailed bill information** (with pagination)  
- **Rate limit: 3 calls/day per subscriber** on summary endpoint  

### ✔️ Banking App
- Retrieve **all unpaid bills** for a subscriber   

### ✔️ Website
- **Pay bills**  
  - Supports **partial payments**  
  - Remaining balance stored until fully paid  
- **Add bill** for a subscriber  
- **Batch add bills via CSV upload**

### ✔️ Authentication
- JWT-based authentication  
- Required for all protected endpoints  
- Includes registration and login via `AuthController`

### ✔️ Logging & Monitoring
- All gateway traffic logged (viewable in Render logs)  
- Gateway enforces `GatewaySecret` for backend access  
- Rate limiting and request tracing handled at gateway level  


## 🧪 Issues Encountered

### ❌ Ocelot Gateway Problems
- Swagger integration conflicts  
- Difficulty customizing logging  
- Eventually replaced by **YARP**, which worked smoothly

### ❌ JSONB Storage Challenges
- Required explicit `.HasColumnType("jsonb")`  
- Needed correct serialization for bill detail objects  


### ❌ Render Hosting Delays
- Free tier causes cold starts  
- Initial requests may have added latency  

---

## 📦 Source Code
🔗 **REST API:** https://github.com/DuruGncy/bill-pay-api

🔗 **GATEWAY:** https://github.com/DuruGncy/mobile-billing-gateway

---

