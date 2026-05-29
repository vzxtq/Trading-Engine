# Trading Platform

A full-stack crypto trading platform with real-time order matching, built with .NET 9 and React 18. The platform integrates seamlessly with the public Binance API to stream live market depth and historical charting data, providing a highly realistic and responsive trading experience.

## Tech Stack

**Backend**
- .NET 9, ASP.NET Core
- Clean Architecture + DDD + CQRS (MediatR)
- Entity Framework Core + SQL Server
- SignalR (real-time market data & order updates)
- JWT Authentication

**Frontend**
- React 18 + TypeScript + Vite
- Zustand (global state) + TanStack Query (server state)
- Tailwind CSS + shadcn/ui
- Lightweight Charts (trading view style candlestick charts)
- SignalR client

## Features

- 📈 **Market Data via Binance API:** Pulls historical candlestick data and live order book depth directly from Binance's public endpoints.
- ⚡ **In-Memory Matching Engine:** Custom-built, sharded matching engine using `Channel<T>` per symbol for lightning-fast concurrent order processing.
- 📊 **Real-Time Charting & Updates:** Beautiful interactive candlestick charts that tick live with incoming trades streamed via SignalR.
- 💼 **Full Trading Workflow:** Place limit/market orders, manage open positions, and view execution history instantly.
- 🔐 **Secure Authentication:** Complete JWT-based user authentication system (register, login, sessions).
- 🌙 **Modern Dark UI:** A sleek, premium dark-themed interface crafted with Tailwind CSS for an excellent user experience.