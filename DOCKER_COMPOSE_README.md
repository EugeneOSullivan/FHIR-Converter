# FHIR-Converter Docker Compose Setup

This Docker Compose configuration provides a complete development and production-ready environment for the FHIR-Converter API with monitoring, load balancing, and security features.

## What Docker Compose Gives You

### 🚀 **Core Benefits**

1. **Complete Development Environment**
   - Single command to start all services
   - Consistent environment across team members
   - No need to install dependencies locally

2. **Production-Like Setup**
   - Reverse proxy with SSL termination
   - Load balancing capabilities
   - Health checks and monitoring

3. **Monitoring & Observability**
   - Prometheus metrics collection
   - Grafana dashboards for visualization
   - Performance monitoring and alerting

4. **Security Features**
   - SSL/TLS encryption
   - Rate limiting
   - Security headers
   - Network isolation

### 📁 **Service Architecture**

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Client/Browser│    │   Load Balancer │    │   FHIR-Converter│
│                 │◄──►│   (Nginx)       │◄──►│   API           │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │   Prometheus    │
                       │   (Monitoring)  │
                       └─────────────────┘
                                │
                                ▼
                       ┌─────────────────┐
                       │   Grafana       │
                       │   (Dashboards)  │
                       └─────────────────┘
```

## 🎯 **Usage Scenarios**

### **Basic Development**
```bash
# Start just the API service
docker-compose up fhir-converter-api

# Access API at http://localhost:8080
```

### **Production-Like Environment**
```bash
# Start with nginx reverse proxy
docker-compose --profile production up

# Access via HTTPS at https://localhost
```

### **Full Monitoring Stack**
```bash
# Start everything including monitoring
docker-compose --profile monitoring up

# Access:
# - API: http://localhost:8080
# - Grafana: http://localhost:3000 (admin/admin)
# - Prometheus: http://localhost:9090
```

## 🔧 **Configuration Options**

### **Environment Variables**
- `ASPNETCORE_ENVIRONMENT`: Set to Development/Production
- `FHIR_CONVERTER__TEMPLATE_DIRECTORY`: Path to templates
- `FHIR_CONVERTER__ENABLE_LOGGING`: Enable/disable logging
- `FHIR_CONVERTER__ENABLE_METRICS`: Enable/disable metrics

### **Volume Mounts**
- `./data/Templates` → `/app/templates`: FHIR templates
- `./data/SampleData` → `/app/sample-data`: Test data
- `~/.aspnet/https` → `/https`: SSL certificates

### **Ports**
- `8080`: HTTP API
- `8081`: HTTPS API
- `80/443`: Nginx (production profile)
- `3000`: Grafana (monitoring profile)
- `9090`: Prometheus (monitoring profile)

## 🛠 **Commands Reference**

### **Start Services**
```bash
# Start all services
docker-compose up

# Start in background
docker-compose up -d

# Start specific profiles
docker-compose --profile production up
docker-compose --profile monitoring up
```

### **Stop Services**
```bash
# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

### **View Logs**
```bash
# All services
docker-compose logs

# Specific service
docker-compose logs fhir-converter-api

# Follow logs
docker-compose logs -f
```

### **Rebuild Services**
```bash
# Rebuild and start
docker-compose up --build

# Rebuild specific service
docker-compose build fhir-converter-api
```

## 📊 **Monitoring Features**

### **Health Checks**
- API health endpoint: `http://localhost:8080/health`
- Automatic container restart on failure
- 30-second check intervals

### **Metrics Collection**
- Request rates and response times
- Error rates and status codes
- Resource usage (CPU, memory)
- Custom FHIR conversion metrics

### **Grafana Dashboards**
- Real-time API performance
- Conversion success/failure rates
- System resource utilization
- Custom alerts and notifications

## 🔒 **Security Features**

### **SSL/TLS**
- Automatic HTTPS redirection
- Modern cipher suites
- HSTS headers

### **Rate Limiting**
- 10 requests per second per IP
- Burst allowance of 20 requests
- Configurable limits

### **Security Headers**
- X-Frame-Options: DENY
- X-Content-Type-Options: nosniff
- X-XSS-Protection: enabled
- Strict-Transport-Security

## 🧪 **Testing with Docker Compose**

### **API Testing**
```bash
# Test health endpoint
curl http://localhost:8080/health

# Test conversion endpoint
curl -X POST http://localhost:8080/api/convert/hl7v2 \
  -H "Content-Type: application/json" \
  -d @data/SampleData/Hl7v2/ADT-A01-01.hl7

# Test with HTTPS (production profile)
curl -k https://localhost/api/convert/hl7v2 \
  -H "Content-Type: application/json" \
  -d @data/SampleData/Hl7v2/ADT-A01-01.hl7
```

### **Load Testing**
```bash
# Install hey (load testing tool)
go install github.com/rakyll/hey@latest

# Run load test
hey -n 1000 -c 10 http://localhost:8080/health
```

## 🚨 **Troubleshooting**

### **Common Issues**

1. **Port Already in Use**
   ```bash
   # Check what's using the port
   lsof -i :8080
   
   # Stop conflicting services
   docker-compose down
   ```

2. **SSL Certificate Issues**
   ```bash
   # Generate development certificate
   dotnet dev-certs https --trust
   ```

3. **Template Loading Issues**
   ```bash
   # Check volume mounts
   docker-compose exec fhir-converter-api ls /app/templates
   ```

4. **Memory Issues**
   ```bash
   # Increase Docker memory limit
   # Docker Desktop → Settings → Resources → Memory
   ```

### **Logs and Debugging**
```bash
# View all logs
docker-compose logs

# View specific service logs
docker-compose logs fhir-converter-api

# Access container shell
docker-compose exec fhir-converter-api /bin/bash
```

## 📈 **Scaling Options**

### **Horizontal Scaling**
```bash
# Scale API instances
docker-compose up --scale fhir-converter-api=3
```

### **Load Balancer Configuration**
- Nginx automatically load balances multiple API instances
- Health checks ensure only healthy instances receive traffic
- Session affinity can be configured if needed

## 🔄 **CI/CD Integration**

### **GitHub Actions Example**
```yaml
- name: Build and Test with Docker Compose
  run: |
    docker-compose up --build -d
    docker-compose exec -T fhir-converter-api dotnet test
    docker-compose down
```

### **Production Deployment**
```bash
# Build production images
docker-compose -f docker-compose.yml -f docker-compose.prod.yml build

# Deploy to production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## 📝 **Next Steps**

1. **Customize Configuration**: Modify environment variables for your needs
2. **Add Custom Dashboards**: Create Grafana dashboards for your metrics
3. **Set Up Alerts**: Configure Prometheus alerting rules
4. **Security Hardening**: Review and enhance security configurations
5. **Performance Tuning**: Optimize based on monitoring data 