# OllamaFarmer Docker Usage Guide

OllamaFarmer is available as a Docker container on DockerHub at `merrrp/ollama-farmer`. This guide covers how to deploy and configure the application using Docker.

## Quick Start

### Basic Docker Run Command

```bash
# Pull the latest image
docker pull merrrp/ollama-farmer:latest

# Run the container
docker run -d \
  --name ollama-farmer \
  -p 8080:8080 \
  -p 8081:8081 \
  -e ConnectionStrings__MySql="server=host.docker.internal;uid=root;pwd=your-password;database=ollamafarmer" \
  -e DefaultChatServer__Uri="http://host.docker.internal:11434" \
  merrrp/ollama-farmer:latest
```

### Docker Compose Example

```yaml
version: '3.8'

services:
  ollama-farmer:
    image: merrrp/ollama-farmer:latest
    container_name: ollama-farmer
    ports:
      - "8080:8080"
      - "8081:8081"
    environment:
      - ConnectionStrings__MySql=server=mysql;uid=root;pwd=your-password;database=ollamafarmer
      - DefaultChatServer__Uri=http://ollama:11434
      - DefaultMcpServer__Uri=http://ollama-farmer:8080/api/mcp/
      - FileRepository__RootDirectory=/file_repository/
      - FileRepository__IsAbsolutePath=true
    volumes:
      - ollama_farmer_files:/file_repository
    depends_on:
      - mysql
      - ollama

  mysql:
    image: mysql:8.0
    container_name: ollama-farmer-mysql
    environment:
      - MYSQL_ROOT_PASSWORD=your-password
      - MYSQL_DATABASE=ollamafarmer
    volumes:
      - mysql_data:/var/lib/mysql
    ports:
      - "3306:3306"

  ollama:
    image: ollama/ollama
    container_name: ollama
    ports:
      - "11434:11434"
    volumes:
      - ollama_data:/root/.ollama

volumes:
  mysql_data:
  ollama_data:
  ollama_farmer_files:
```

## Configuration

### Environment Variables

OllamaFarmer uses .NET's configuration system, which allows you to override any `appsettings.json` value using environment variables. Use double underscores (`__`) to represent nested JSON properties.

#### Database Configuration

| Environment Variable | Description | Default | Example |
|---------------------|-------------|---------|---------|
| `ConnectionStrings__MySql` | MySQL connection string | `""` | `server=localhost;uid=root;pwd=password;database=ollamafarmer` |

#### Default Chat Server Configuration

| Environment Variable | Description | Default | Example |
|---------------------|-------------|---------|---------|
| `DefaultChatServer__Name` | Default Ollama server name | `"Local Ollama Server"` | `"Production Ollama"` |
| `DefaultChatServer__Description` | Default Ollama server description | `"Default local Ollama server instance"` | `"Production Ollama instance"` |
| `DefaultChatServer__Uri` | Default Ollama server URL | `"http://localhost:11434"` | `"http://ollama.company.com:11434"` |

#### Default MCP Server Configuration

| Environment Variable | Description | Default | Example |
|---------------------|-------------|---------|---------|
| `DefaultMcpServer__Name` | Default MCP server name | `"Local MCP Server"` | `"Production MCP"` |
| `DefaultMcpServer__Description` | Default MCP server description | `"Default local MCP server instance"` | `"Production MCP instance"` |
| `DefaultMcpServer__Uri` | Default MCP server URL | `"http://localhost:5280/api/mcp/"` | `"http://mcp.company.com:5280/api/mcp/"` |

#### File Repository Configuration

| Environment Variable | Description | Default | Example |
|---------------------|-------------|---------|---------|
| `FileRepository__RootDirectory` | File storage directory | `file_repository` | `/file_repository/` |
| `FileRepository__IsAbsolutePath` | Whether the path is absolute | `false` | `true` |
| `FileRepository__MaxFileSizeInBytes` | Maximum file upload size | `104857600` (100MB) | `209715200` (200MB) |
| `FileRepository__AllowedFileExtensions__0` | First allowed extension | `.txt` | `.pdf` |
| `FileRepository__AllowedFileExtensions__1` | Second allowed extension | `.json` | `.docx` |

#### Logging Configuration

| Environment Variable | Description | Default | Example |
|---------------------|-------------|---------|---------|
| `Logging__LogLevel__Default` | Default log level | `Information` | `Debug` |
| `Logging__LogLevel__Microsoft.AspNetCore` | ASP.NET Core log level | `Warning` | `Information` |

#### Serilog Configuration (Optional)

| Environment Variable | Description | Example |
|---------------------|-------------|---------|
| `Serilog__WriteTo__0__Name` | Serilog sink name | `Seq` |
| `Serilog__WriteTo__0__Args__serverUrl` | Seq server URL | `http://seq:5341` |

### Ports

- **8080**: HTTP port
- **8081**: HTTPS port

### Volumes

#### Recommended Volume Mounts

1. **File Repository**: `/file_repository` - For persistent file storage
2. **Logs** (optional): `/app/logs` - For log file persistence

Example with volumes:
```bash
docker run -d \
  --name ollama-farmer \
  -p 8080:8080 \
  -p 8081:8081 \
  -v ollama_farmer_files:/file_repository \
  -v ollama_farmer_logs:/app/logs \
  -e ConnectionStrings__MySql="server=host.docker.internal;uid=root;pwd=your-password;database=ollamafarmer" \
  -e DefaultChatServer__Uri="http://host.docker.internal:11434" \
  merrrp/ollama-farmer:latest
```

## Complete Setup Examples

### Example 1: Standalone with External Services

This example assumes you have MySQL and Ollama running externally (on the host or other containers).

```bash
# Stop and remove existing container
docker stop ollama-farmer 2>/dev/null || true
docker rm ollama-farmer 2>/dev/null || true

# Run OllamaFarmer
docker run -d \
  --name ollama-farmer \
  -p 8080:8080 \
  -p 8081:8081 \
  -v ollama_farmer_files:/file_repository \
  -e ConnectionStrings__MySql="server=host.docker.internal;uid=root;pwd=ollama-farmer-mysql;database=ollamafarmer" \
  -e DefaultChatServer__Uri="http://host.docker.internal:11434" \
  -e FileRepository__RootDirectory="/file_repository/" \
  -e FileRepository__IsAbsolutePath="true" \
  merrrp/ollama-farmer:latest
```

### Example 2: Full Stack with Docker Compose

Create a `docker-compose.yml` file:

```yaml
version: '3.8'

services:
  ollama-farmer:
    image: merrrp/ollama-farmer:latest
    container_name: ollama-farmer
    restart: unless-stopped
    ports:
      - "8080:8080"
      - "8081:8081"
    environment:
      - ConnectionStrings__MySql=server=mysql;uid=root;pwd=secure-password-123;database=ollamafarmer
      - DefaultChatServer__Uri=http://ollama:11434
      - DefaultMcpServer__Uri=http://ollama-farmer:8080/api/mcp/
      - FileRepository__RootDirectory=/file_repository/
      - FileRepository__IsAbsolutePath=true
      - Logging__LogLevel__Default=Information
    volumes:
      - ollama_farmer_files:/file_repository
      - ollama_farmer_logs:/app/logs
    depends_on:
      mysql:
        condition: service_healthy
      ollama:
        condition: service_started
    networks:
      - ollama-network

  mysql:
    image: mysql:8.0
    container_name: ollama-farmer-mysql
    restart: unless-stopped
    environment:
      - MYSQL_ROOT_PASSWORD=secure-password-123
      - MYSQL_DATABASE=ollamafarmer
      - MYSQL_CHARSET=utf8mb4
      - MYSQL_COLLATION=utf8mb4_unicode_ci
    volumes:
      - mysql_data:/var/lib/mysql
    ports:
      - "3306:3306"
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      timeout: 20s
      retries: 10
    networks:
      - ollama-network

  ollama:
    image: ollama/ollama:latest
    container_name: ollama
    restart: unless-stopped
    ports:
      - "11434:11434"
    volumes:
      - ollama_data:/root/.ollama
    networks:
      - ollama-network

volumes:
  mysql_data:
    driver: local
  ollama_data:
    driver: local
  ollama_farmer_files:
    driver: local
  ollama_farmer_logs:
    driver: local

networks:
  ollama-network:
    driver: bridge
```

Run with:
```bash
docker-compose up -d
```

## Troubleshooting

### Common Issues

1. **Database Connection Issues**
   - Ensure MySQL is running and accessible
   - Check the connection string format
   - Verify database exists and user has permissions

2. **Ollama Connection Issues**
   - Ensure Ollama is running on the specified host and port
   - Check firewall settings
   - Use `host.docker.internal` for services running on the host

3. **File Upload Issues**
   - Check volume mount permissions
   - Verify `FileRepository__RootDirectory` path
   - Ensure the container user (UID 99) has write access

4. **SSL/HTTPS Issues**
   - The container runs on HTTP by default
   - For HTTPS, configure a reverse proxy (nginx, traefik, etc.)

### Debugging

View logs:
```bash
docker logs ollama-farmer
```

Access container shell:
```bash
docker exec -it ollama-farmer bash
```

Check configuration:
```bash
docker exec ollama-farmer cat /app/appsettings.json
```

## Security Considerations

1. **Database**: Use strong passwords and consider using Docker secrets
2. **File Storage**: Ensure proper volume permissions
3. **Network**: Use Docker networks to isolate services
4. **HTTPS**: Use a reverse proxy for SSL termination in production
5. **Updates**: Regularly update the container image

## Performance Tuning

1. **Memory**: Allocate sufficient memory for the container
2. **CPU**: Consider CPU limits based on usage
3. **Storage**: Use appropriate volume drivers for performance
4. **Logging**: Configure log rotation to prevent disk space issues

Example with resource limits:
```bash
docker run -d \
  --name ollama-farmer \
  --memory="2g" \
  --cpus="1.5" \
  -p 8080:8080 \
  -p 8081:8081 \
  # ... other options
  merrrp/ollama-farmer:latest
```
