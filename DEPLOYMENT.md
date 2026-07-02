# 🚀 Deployment Guide - Attendance Service

## Từng bước triển khai Docker

### 1️⃣ **Điều kiện tiên quyết**

- ✅ Docker Desktop cài đặt
- ✅ Git cài đặt
- ✅ PowerShell hoặc Bash shell
- ✅ RAM: ≥ 4GB (khuyến nghị 8GB+)
- ✅ Disk space: ≥ 10GB

### 2️⃣ **Clone Repository**

```bash
# Clone từ GitHub (thay URL)
git clone https://github.com/your-org/attendance-service.git
cd attendance-service
```

### 3️⃣ **Xây dựng Docker Image**

#### Option A: Build từ source code

```bash
# Build image
docker build -t attendance-service:latest -f API/Dockerfile .

# Kiểm tra image
docker images | findstr attendance
```

#### Option B: Pull pre-built image (nếu có)

```bash
docker pull your-registry/attendance-service:latest
```

### 4️⃣ **Chạy với docker-compose (Khuyến nghị)**

```bash
# Khởi động tất cả services
docker-compose up -d

# Kiểm tra logs
docker-compose logs -f attendance-api

# Xem trạng thái services
docker-compose ps
```

**Services sẽ start:**
- ✅ SQL Server (port 1433)
- ✅ Attendance API (port 8080, 8081)
- ✅ RabbitMQ Management (port 15672)

### 5️⃣ **Khởi tạo Database**

```bash
# Option A: Sử dụng migrations
cd API
dotnet ef database update

# Option B: Seed sample data
sqlcmd -S localhost -d AttendanceDB -E -i API/Migrations/SeedAttendanceData.sql

# Option C: Docker exec
docker-compose exec mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourPassword123! -d AttendanceDB -i /app/API/Migrations/SeedAttendanceData.sql
```

### 6️⃣ **Verify API Health**

```bash
# Check Swagger API
curl http://localhost:8080/swagger/index.html

# Health check endpoint
curl http://localhost:8081/health

# Sample check-in request
curl -X POST http://localhost:8080/api/attendance/check-in \
  -H "Content-Type: application/json" \
  -d '{"employeeId":"f4b93dbb-d8ae-4b8b-86f8-d86d35dadde9"}'
```

### 7️⃣ **Dừng Services**

```bash
# Stop containers
docker-compose stop

# Remove containers
docker-compose down

# Remove volumes (DATA LOSS - BE CAREFUL)
docker-compose down -v
```

---

## Troubleshooting

### 🔴 Error: "port 1433 is already in use"

```bash
# Kill process on port 1433
netstat -ano | findstr :1433
taskkill /PID <PID> /F

# OR use different port in docker-compose.yml
# ports:
#   - "1434:1433"
```

### 🔴 Error: "ConnectionString timeout"

```bash
# 1. Check SQL Server is running
docker-compose ps

# 2. Verify connection string in appsettings.json
# 3. Check firewall settings
# 4. Verify SQL Server health
docker-compose logs mssql
```

### 🔴 Error: "Cannot connect to Docker daemon"

```bash
# Start Docker Desktop or
# Check if Docker service is running:

# Windows
sc start Docker

# Linux
sudo systemctl start docker
```

### 🔴 Error: "Permission denied" in Docker build

```bash
# Run PowerShell as Administrator OR
# Use WSL 2 backend in Docker Desktop settings
```

---

## Production Deployment

### AWS ECS / Container Services

```bash
# Push to ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin <ECR_URI>
docker tag attendance-service:latest <ECR_URI>/attendance-service:latest
docker push <ECR_URI>/attendance-service:latest

# Deploy to ECS Cluster (see Terraform/CloudFormation configs)
```

### Azure Container Registry

```bash
# Login to ACR
az acr login --name <registry-name>

# Push image
docker tag attendance-service:latest <registry-name>.azurecr.io/attendance-service:latest
docker push <registry-name>.azurecr.io/attendance-service:latest

# Deploy to ACI/AKS
az container create \
  --resource-group <rg-name> \
  --name attendance-api \
  --image <registry-name>.azurecr.io/attendance-service:latest \
  --environment-variables ConnectionStrings__DefaultConnection="Server=..." \
  --ports 8080 8081
```

### Kubernetes

```bash
# Create deployment
kubectl apply -f k8s/deployment.yaml

# Check status
kubectl get pods -n attendance

# View logs
kubectl logs -f deployment/attendance-api -n attendance

# Scale replicas
kubectl scale deployment/attendance-api --replicas=3 -n attendance
```

---

## Performance Tuning

### Memory Configuration

```bash
# Adjust JVM memory (if using Java-based services)
docker run \
  -e DOTNET_GCHeapCount=2 \
  -m 2g \
  attendance-service:latest
```

### Database Connection Pooling

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=mssql;Database=AttendanceDB;...Connection Lifetime=30;Max Pool Size=100;"
  }
}
```

### Logging Optimization

```bash
# Enable structured logging
docker-compose exec attendance-api \
  curl -X POST http://localhost:8081/log \
  -H "Content-Type: application/json" \
  -d '{"level":"Information"}'
```

---

## Backup & Recovery

### Backup Database

```bash
# Full backup
docker-compose exec mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourPassword123! \
  -Q "BACKUP DATABASE AttendanceDB TO DISK = '/var/opt/mssql/backup/AttendanceDB.bak'"

# Incremental backup
docker-compose exec mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourPassword123! \
  -Q "BACKUP DATABASE AttendanceDB TO DISK = '/var/opt/mssql/backup/AttendanceDB_incremental.bak' WITH DIFFERENTIAL"
```

### Restore Database

```bash
docker-compose exec mssql /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourPassword123! \
  -Q "RESTORE DATABASE AttendanceDB FROM DISK = '/var/opt/mssql/backup/AttendanceDB.bak'"
```

---

## Monitoring & Logging

### Prometheus Metrics

```bash
curl http://localhost:8081/metrics
```

### Application Insights

```json
{
  "ApplicationInsights": {
	"InstrumentationKey": "your-key-here",
	"SamplingSettings": {
	  "isEnabled": true,
	  "maxTelemetryItemsPerSecond": 20
	}
  }
}
```

### Container Logs

```bash
# Real-time logs
docker-compose logs -f attendance-api

# Tail last 100 lines
docker-compose logs --tail=100 attendance-api

# Export logs
docker-compose logs attendance-api > container-logs.txt
```

---

## Security Best Practices

✅ **Do's:**
- ✅ Use strong passwords (SA password ≠ sa)
- ✅ Rotate connection strings regularly
- ✅ Use secrets management (Azure Key Vault, HashiCorp Vault)
- ✅ Enable SSL/TLS for API endpoints
- ✅ Use environment-specific appsettings
- ✅ Enable audit logging
- ✅ Scan images for vulnerabilities: `docker scan attendance-service:latest`

❌ **Don'ts:**
- ❌ Don't hardcode passwords in Dockerfile
- ❌ Don't run containers as root
- ❌ Don't expose internal services to public
- ❌ Don't skip security updates

### Image Scanning

```bash
# Scan for vulnerabilities
docker scan attendance-service:latest

# Or use Trivy
trivy image attendance-service:latest
```

---

## Health Checks & Monitoring

### Endpoint Status

```bash
# Check API health
curl -i http://localhost:8080/health

# Database connectivity
curl -i http://localhost:8080/health/db

# RabbitMQ connectivity
curl -i http://localhost:8080/health/queue
```

### Automated Restarts

Docker Compose automatically restarts failed services with `restart: unless-stopped` policy.

---

## Rollback Strategy

```bash
# Keep previous image
docker tag attendance-service:latest attendance-service:v1.0.0

# Rollback to previous version
docker-compose down
docker-compose up -d  # Uses v1.0.0 from docker-compose.yml
```

---

**📞 Support & Feedback**

- 📧 Email: nlinh3040@gmail.com
- 🐛 Issues: [GitHub Issues](https://github.com/your-org/attendance-service/issues)
- 📚 Docs: [API Documentation](http://localhost:8080/swagger)
