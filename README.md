# PadiPay 🇳🇬

> **Nigeria's Escrow-Powered Tech Freelance Marketplace**

PadiPay is a modern freelance marketplace and financial escrow platform built with **ASP.NET Core MVC (.NET 10)**, **Entity Framework Core**, and **Microsoft SQL Server**. Designed specifically for the Nigerian tech ecosystem, PadiPay enables tech professionals (software engineers, UI/UX designers, data analysts, DevOps engineers, and technical writers) to offer gigs while giving buyers 100% financial security through an automated escrow engine.

Every user on PadiPay has a **dual role** — you can sell your tech services and hire other talent from a single unified account, with administrators mediating disputes and ensuring platform integrity.

---

## 🌟 Key Features

### 1. Dual-Role User Experience (Sell & Buy Seamlessly)
- Unified account: Switch effortlessly between acting as a **Buyer** and a **Seller**.
- **Tech Profile**: Showcase your bio, headline, tech stack/skills, location (State in Nigeria), and sample portfolio work.
- **Identity & Security**: Cookie-based authentication via ASP.NET Core Identity with role-based access control (`User`, `Admin`).

### 2. Gig / Service Marketplace
- **Tech Categories**: Web Development, Mobile Apps, UI/UX Design, Cloud/DevOps, Data/AI, Cybersecurity, and Technical Writing.
- **Rich Gig Listings**: Title, category, rich description, pricing in Nigerian Naira (`₦`), turnaround time (delivery days), revision allowance, and cover media.
- **Gig Management**: Sellers can create, edit, activate, or pause their gigs.
- **Discovery & Search**: Filter by category, keyword search, price range, and delivery speed.

### 3. Escrow Trust Engine & Order Lifecycle
Money moves safely through a disciplined multi-state workflow:

```
[Order Placed & Escrow Funded] 
       │
       ▼
[Requirements Submitted] ──► [In Progress] ──► [Delivered]
                                                  │
                      ┌───────────────────────────┴───────────────────────────┐
                      ▼                                                       ▼
            [Buyer Accepts Delivery]                               [Revision Requested / Disputed]
                      │                                                       │
                      ▼                                                       ▼
        [Funds Released to Seller]                                 [Admin Arbitration]
                                                               (Refund Buyer or Payout Seller)
```

- **Atomic Financial Transactions**: When an order is created, funds are debited from the buyer's wallet and locked in escrow via EF Core atomic transactions.
- **Proof of Work & Delivery**: Sellers submit live preview links, GitHub repositories, and deliverables.
- **Safe Release**: Funds are credited to the seller's wallet only after the buyer confirms satisfaction.

### 4. Ratings & Client Reviews
- Buyers leave star ratings (1–5) and written reviews after completed orders.
- Dynamic seller reputation scoring and social proof displayed across gig cards and profiles.

### 5. Administrator Back-Office & Dispute Center
- **Dispute Resolution**: If a buyer and seller reach an impasse, platform admins review work submissions, project history, and deliver binding decisions (Refund Buyer or Payout Seller).
- **Platform Analytics**: Track total registered talent, active gigs, escrow volume, and dispute metrics.
- **Category & Content Moderation**: Manage categories and ensure marketplace quality.

### 6. In-App Wallet
- Every user gets an in-app `Wallet` denominated in Naira (`₦`).
- Instant simulated top-up and real-time balance tracking.
- Detailed audit ledger of wallet debits, escrow holds, credits, and payouts.

---

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| **Framework** | ASP.NET Core MVC (.NET 10) |
| **Language** | C# 13 |
| **Database & ORM** | Microsoft SQL Server (LocalDB) + Entity Framework Core |
| **Authentication & RBAC** | ASP.NET Core Identity (Role-based: `User`, `Admin`) |
| **Frontend** | Razor Views, Bootstrap 5, Custom Modern CSS Design System |
| **Architecture** | MVC with Service/Data layer separation & atomic transactions |

---

## 🏛️ Domain Architecture

```
PadiPay/
├── Areas/
│   ├── Admin/                     # Administrative portal (Disputes, Moderation, Stats)
│   └── Identity/                  # Authentication & Profile management
├── Controllers/
│   ├── HomeController.cs          # Landing page & marketplace discovery
│   ├── GigController.cs           # Gig creation, browsing, searching, and detail views
│   ├── OrderController.cs         # Order placement, workflow, delivery & revisions
│   ├── WalletController.cs        # Wallet balance, top-ups, transaction history
│   └── ReviewController.cs        # Rating and review submissions
├── Data/
│   ├── ApplicationDbContext.cs    # EF Core DbContext with relational mappings
│   └── DbInitializer.cs           # Seed data for categories, roles & default admin
├── Models/
│   ├── ApplicationUser.cs         # Extended IdentityUser (Profile, Skills, Bio, Location)
│   ├── Category.cs                # Marketplace categories
│   ├── Gig.cs                     # Gig listings
│   ├── Order.cs                   # Order lifecycle state machine
│   ├── OrderDelivery.cs           # Delivery submissions (files, notes, links)
│   ├── OrderDispute.cs            # Dispute tickets and resolutions
│   ├── Review.cs                  # Client reviews and ratings
│   ├── Transaction.cs             # Escrow ledger transactions
│   ├── Wallet.cs                  # User wallet and balance
│   └── Enums/                     # Status enums (OrderStatus, TransactionStatus, DisputeStatus)
├── ViewModels/                    # Dedicated DTOs and ViewModels for views
└── Views/                         # Razor UI Views & Components
```

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server LocalDB (`(localdb)\mssqllocaldb`) or full SQL Server instance

### Setup & Run

1. **Clone the repository:**
   ```bash
   git clone https://github.com/highfrezh/PadiPay.git
   cd PadiPay
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Apply database migrations:**
   ```bash
   dotnet ef database update
   ```

4. **Launch the application:**
   ```bash
   dotnet run
   ```

5. Open your browser and navigate to the printed localhost URL (e.g. `https://localhost:7198`).

---

## 🗺️ Step-by-Step Implementation Roadmap

- [x] **Milestone 1**: Core Escrow Prototype & Wallet Ledger
- [x] **Milestone 2**: Platform Vision, Architecture & README Specification
- [ ] **Milestone 3**: Extended `ApplicationUser` Profile, Identity Roles (`Admin`/`User`) & Database Seeding
- [ ] **Milestone 4**: Tech Categories & Gig / Service Management (Create, Edit, Pause, Manage)
- [ ] **Milestone 5**: Marketplace Discovery, Search, Filtering & Public Gig/Seller Profiles
- [ ] **Milestone 6**: Complete Gig Order Lifecycle, Delivery Submissions & Escrow Integration
- [ ] **Milestone 7**: Client Reviews, Star Ratings & Seller Reputation Metrics
- [ ] **Milestone 8**: Admin Portal, Dispute Resolution Center & Platform Moderation
- [ ] **Milestone 9**: UI/UX Polish, Nigerian Tech Aesthetics & In-App Activity Notifications

---

## 👨‍💻 Author

Built by [Ibraheem](https://github.com/highfrezh) — transitioning into advanced C#/.NET backend & distributed systems engineering alongside background in Python, FastAPI, and AI/ML systems.
