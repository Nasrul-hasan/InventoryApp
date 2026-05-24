# Inventory Management App

A full-featured web application for managing custom inventories built with ASP.NET Core MVC.

##  Live Demo
[https://inventoryapp-0pys.onrender.com](https://inventoryapp-0pys.onrender.com)

##  Features

- **Custom Inventories** — Create inventories with custom fields (text, numeric, checkbox, etc.)
- **Custom Items** — Add items with field values and custom IDs
- **Real-time Comments** — Live discussion using SignalR
- **Full-text Search** — Search across all inventories and items
- **Admin Panel** — User management (block/unblock/delete/roles)
- **Google & Facebook OAuth** — Social login support
- **Dark/Light Theme** — User preference saved
- **Bilingual** — English and Bengali (বাংলা) support
- **Tag System** — Tag cloud with autocomplete
- **Access Control** — Public/private inventories with user-level access
- **Statistics** — Per-inventory analytics
- **Markdown** — Description field supports Markdown

## Tech Stack

- **Backend:** ASP.NET Core MVC (.NET 8)
- **Database:** PostgreSQL (Supabase) / SQL Server (local)
- **ORM:** Entity Framework Core
- **Real-time:** SignalR
- **Auth:** ASP.NET Identity + Google + Facebook OAuth
- **Frontend:** Bootstrap 5, JavaScript
- **Deployment:** Render

## Run Locally

```bash
git clone https://github.com/Nasrul-hasan/InventoryApp.git
cd InventoryApp
dotnet restore
dotnet run
```

## Environment Variables

```
ConnectionStrings__DefaultConnection=your_connection_string
Authentication__Google__ClientId=your_google_client_id
Authentication__Google__ClientSecret=your_google_secret
Authentication__Facebook__AppId=your_facebook_app_id
Authentication__Facebook__AppSecret=your_facebook_secret
```
