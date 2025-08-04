
# OllamaFarmer

[![Release AMD64](https://github.com/Merp4/OllamaFarmer/actions/workflows/docker-release-amd64.yml/badge.svg)](https://github.com/Merp4/OllamaFarmer/actions/workflows/docker-release-amd64.yml)
&nbsp;&nbsp;&nbsp;
[![CI AMD64](https://github.com/Merp4/OllamaFarmer/actions/workflows/docker-ci-amd64.yml/badge.svg)](https://github.com/Merp4/OllamaFarmer/actions/workflows/docker-ci-amd64.yml)

A full-stack application for managing and interacting with Ollama AI models, featuring a React TypeScript frontend and ASP.NET Core backend with Entity Framework and Model Context Protocol (MCP) integration.

This project is in its infancy, breaking changes with updates are to be expected. It is primarily designed for personal use and learning purposes.

That said, breaking changes are to be kept to a minimum.

⚠️ `Todo: DockerHub link & documentation` ⚠️

## 🚀 Features

- **Chat Management**: Create, manage, and interact with AI chat sessions
- **Model Support**: Full integration with Ollama models and capabilities
- **Tool Integration**: Model Context Protocol (MCP) tools for enhanced AI interactions
- **Multi-Server Support**: Connect to multiple Ollama instances via `chatServerId`
- **Real-time Updates**: SignalR integration
- **File Management**: Upload and manage images and files for AI interactions
- **Modern UI**: React TypeScript frontend with Vite build system

## Screeshots

### Model management

![Screenshot 01](docs/screenshot_01.png)

### Chat management

![Screenshot 02](docs/screenshot_02.png)

### Image/file support

[Image by @jossiahfarrow](https://www.pexels.com/@josiahfarrow/)
![Screenshot 02](docs/screenshot_03_josiahfarrow.png)

### Thinking Filter

![Screenshot 04](docs/screenshot_04.png)
![Screenshot 05](docs/screenshot_05.png)

### MCP tools

`Tool call get_random_number completed successfully.`
![Screenshot 06](image.png)

## 🏗️ Architecture

### Backend (`OllamaFarmer.Server`)

- **ASP.NET Core 9** with modern minimal APIs
- **Entity Framework Core** with MySQL/MariaDB support
- **Microsoft.Extensions.AI** for standardized AI integrations
- **Model Context Protocol (MCP)** for tool integrations
- **SignalR** for real-time communication
- **OpenAPI/Swagger** for API documentation

### Frontend (`ollamafarmer.client`)

- **React 18** with TypeScript
- **Vite** for fast development and building
- **OpenAPI Code Generation** for type-safe API client
- **Modern React patterns** with hooks and context

## 📦 Setup

### Prerequisites

- Docker (for Ollama and database)
- .NET 9 SDK
- Node.js 18+ and npm

### 1. Ollama Setup

Official DockerHub: [ollama/ollama](https://hub.docker.com/r/ollama/ollama)

```bash
# Pull latest Ollama image
docker pull ollama/ollama:latest

# Run Ollama (CPU version)
docker run -d -v $env:APPDATA\ollama-docker:/root/.ollama -p 11434:11434 --name ollama ollama/ollama:latest

# Install models
docker exec -it ollama ollama run llama3
```

[Browse available models](https://ollama.com/library)

### 2. Database Setup

#### MySQL (Recommended)

```bash
docker run --name ollamafarmer-mysql \
  -v $env:APPDATA\ollamafarmer\mysql:/var/lib/mysql \
  -e MYSQL_ROOT_PASSWORD=ollama-expo-mysql \
  -e MYSQL_DATABASE=ollamafarmer \
  -p 3306:3306 -d mysql:latest
```

#### MariaDB (Alternative)

```bash
docker run --name ollamafarmer-mariadb \
  -v $env:APPDATA\ollamafarmer\mariadb:/var/lib/mysql:Z \
  -e MARIADB_ROOT_PASSWORD=ollama-expo-mysql \
  -e MARIADB_DATABASE=ollamafarmer \
  -p 3306:3306 -d mariadb:latest
```

### 3. Backend Setup

```bash
cd OllamaFarmer.Server

# Restore packages
dotnet restore

# Configure connection string in appsettings.Development.json
# Default: "server=127.0.0.1;uid=root;pwd=ollama-expo-mysql;database=ollamafarmer"

# Run database migrations
dotnet ef database update

# Start the server
dotnet run
```

### 4. Frontend Setup

```bash
cd ollamafarmer.client

# Install dependencies
npm install

# Start development server
npm run dev
```

## 🔧 Development

### API Schema Updates

The project uses OpenAPI code generation for type-safe frontend API clients:

```bash
# Regenerate API types after backend changes
cd ollamafarmer.client
npm run build-api
```

### Database Migrations

```bash
cd OllamaFarmer.Server

# Create new migration
dotnet ef migrations add MigrationName

# Apply migrations
dotnet ef database update

# View migration status
dotnet ef migrations list
```

### Key Configuration Files

- `OllamaFarmer.Server/appsettings.Development.json` - Backend configuration
- `ollamafarmer.client/vite.config.ts` - Frontend build configuration
- `OllamaFarmer.Server/Data/AppDbContextFactory.cs` - EF design-time factory

## 🔗 API Endpoints

### Chat Management

- `GET /api/AppChat` - List all chats
- `GET /api/AppChat/{id}` - Get chat details
- `POST /api/AppChat` - Create new chat
- `PUT /api/AppChat/{id}` - Update chat
- `DELETE /api/AppChat/{id}` - Delete chat

### Message Operations

- `POST /api/AppChat/{id}` - Send message to chat
- `PUT /api/AppChat/{id}/{messageId}` - Edit message
- `DELETE /api/AppChat/{id}/{messageId}` - Delete message
- `POST /api/AppChat/{id}/resubmit` - Resubmit last message

### System

- `GET /api/ChatServer` - List available Ollama servers
- `GET /api/Tools` - List available MCP tools
- `POST /api/File/upload` - Upload files/images

## 🛠️ Troubleshooting

### Common Issues

1. **Migration Errors**: Use the `AppDbContextFactory` for design-time operations
2. **API Type Mismatches**: Regenerate API types with `npm run build-api`
3. **Database Connection**: Verify MySQL/MariaDB container is running
4. **Ollama Connection**: Ensure Ollama is accessible on port 11434

### Build Verification

```bash
# Backend build
cd OllamaFarmer.Server && dotnet build

# Frontend build
cd ollamafarmer.client && npm run build
```
