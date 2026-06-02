<div align="center">

<img src="./docs/assets/trading-engine-logo.png" alt="Trading Engine" width="480" />

<br />

**A real-time trading platform, built end-to-end from matching engine to UI**

</div>

---

## Overview

Trading Engine is a monorepo containing a **.NET 9 backend** and a **React 18 frontend** for a fully functional trading platform. Users register, fund their accounts, and place limit or market orders. Orders flow into a sharded in-memory matching engine that pairs buyers with sellers, settles trades atomically, and pushes live updates to every connected client over SignalR — no polling.

The backend is built on **Clean Architecture** and **DDD**, with strict layer boundaries enforced throughout. The frontend communicates with the API over REST and maintains a live connection via SignalR for real-time order book and execution updates. Market charts and order book data are sourced from the Binance API, providing real-time market depth and candlestick data for supported trading pairs.

---

## Architecture

```
TradingPlatformBackend/
├── TradingPlatform.Domain          # Entities, value objects, domain events — zero dependencies
├── TradingPlatform.Application     # CQRS commands & queries, repository interfaces
├── TradingPlatform.MatchingEngine  # In-memory order book, sharded per-symbol workers
├── TradingPlatform.Infrastructure  # EF Core, repositories, JWT, BCrypt
├── TradingEngineApi                # ASP.NET Core controllers + SignalR hubs
└── TradingPlatform.IntegrationTests

TradingPlatformFrontend/
└── src/
    ├── features/auth               # Login, registration, profile management
    └── features/trading            # Dashboard, chart, order book, orders panel
```

---

## Backend

- **Matching Engine** — sharded per-symbol workers running price-time priority order books inside bounded channels. No shared mutable state between shards.
- **Binance Market Data Integration** — consumes real-time Binance API streams and REST endpoints for candlestick data, ticker information, and live order book snapshots.
- **Execution pipeline** — after a match, a dispatcher fans results to independent handlers: DB persistence, account settlement, and SignalR notifications.
- **CQRS + MediatR** — commands mutate state; queries are read-only projections. Handlers are small and focused.
- **Domain-Driven Design** — all business logic lives in entities and value objects. `Money`, `Price`, `Quantity`, `Symbol` are immutable value objects with EF converters.
- **Entity Framework Core 9** — value object converters, owned types, compiled queries.
- **Auth** — JWT access tokens + rotating refresh tokens, BCrypt password hashing.

---

## Frontend

- **Real-time** order book and recent trade feed over SignalR
- **Live market data** powered by Binance API for candlestick charts, ticker prices, and order book depth
- **Candlestick chart** powered by Lightweight Charts (TradingView)
- **Limit & market orders**, open positions, execution history
- **TanStack Query** for server state, **Zustand** for client state
- **shadcn/ui** + Tailwind CSS with a premium dark theme

---

## Tech Stack

| Layer | Technologies |
|---|---|
| **Backend** | .NET 9, ASP.NET Core, EF Core 9, MediatR, FluentValidation, BCrypt |
| **Matching Engine** | System.Threading.Channels, sharded workers, price-time priority |
| **Market Data** | Binance API |
| **Database** | SQL Server, EF Core migrations |
| **Realtime** | SignalR |
| **Frontend** | React 18, TypeScript, Vite, Tailwind CSS, shadcn/ui |
| **State** | Zustand (client), TanStack Query (server) |
| **Charts** | Lightweight Charts (TradingView) |
| **Auth** | JWT + refresh tokens |

---

<div align="center">

Built with precision. Made to trade.

</div>