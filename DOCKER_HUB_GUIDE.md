# 🐳 Docker Hub Deployment Guide

Hướng dẫn đẩy Docker image lên Docker Hub hoặc GitHub Container Registry.

## 🔐 Prepare Credentials

### Docker Hub

```bash
# Login to Docker Hub
docker login -u <username> -p <password>

# Or use PAT (Personal Access Token)
cat ~/my_pat.txt | docker login -u <username> --password-stdin
```

### GitHub Container Registry (GHCR)

```bash
# Create Personal Access Token:
# 1. Go to GitHub Settings > Developer settings > Personal access tokens
# 2. Generate new token with scopes: write:packages, read:packages
# 3. Copy token

# Login to GHCR
echo "<YOUR_GITHUB_PAT>" | docker login ghcr.io -u <github-username> --password-stdin
```

---

## 📤 Push to Docker Hub

### 1. Tag Image

```bash
# Build locally
docker build -t attendance-service:latest -f API/Dockerfile .

# Tag for Docker Hub
docker tag attendance-service:latest <docker-hub-username>/attendance-service:latest
docker tag attendance-service:latest <docker-hub-username>/attendance-service:1.0.0
```

### 2. Push to Docker Hub

```bash
docker push <docker-hub-username>/attendance-service:latest
docker push <docker-hub-username>/attendance-service:1.0.0
```

### 3. Verify on Docker Hub

Visit: `https://hub.docker.com/r/<docker-hub-username>/attendance-service`

---

## 📤 Push to GitHub Container Registry

### 1. Tag Image

```bash
# Build locally
docker build -t attendance-service:latest -f API/Dockerfile .

# Tag for GHCR
docker tag attendance-service:latest ghcr.io/<github-username>/attendance-service:latest
docker tag attendance-service:latest ghcr.io/<github-username>/attendance-service:1.0.0
```

### 2. Push to GHCR

```bash
docker push ghcr.io/<github-username>/attendance-service:latest
docker push ghcr.io/<github-username>/attendance-service:1.0.0
```

### 3. Verify on GitHub

Visit: `https://github.com/<username>?tab=packages`

---

## 🔄 Automated Push with GitHub Actions

The workflows in `.github/workflows/build-and-deploy.yml` automatically:

1. ✅ Build Docker image
2. ✅ Push to GitHub Container Registry
3. ✅ Tag with commit SHA and "latest"
4. ✅ Cache image layers for faster rebuilds

**No manual push needed!** Just commit to `main` branch.

---

## 🚀 Pull & Run from Registry

### From Docker Hub

```bash
# Pull image
docker pull <docker-hub-username>/attendance-service:latest

# Run container
docker run -d \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=AttendanceDB;..." \
  <docker-hub-username>/attendance-service:latest
```

### From GitHub Container Registry

```bash
# Pull image
docker pull ghcr.io/<github-username>/attendance-service:latest

# Run container
docker run -d \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=AttendanceDB;..." \
  ghcr.io/<github-username>/attendance-service:latest
```

---

## 🏢 Private Registry (Docker Compose Example)

```yaml
services:
  attendance-api:
	image: ghcr.io/linhbonsochin/attendance-service:latest
	pull_policy: always
	ports:
	  - "8080:8080"
	environment:
	  - ConnectionStrings__DefaultConnection=Server=mssql,1433;Database=AttendanceDB;User Id=sa;Password=YourPassword123!;
	depends_on:
	  mssql:
		condition: service_healthy
```

---

## 📝 Image Versioning Strategy

### Version Tags
```bash
# Production release
docker tag attendance-service:latest <repo>/attendance-service:1.0.0
docker tag attendance-service:latest <repo>/attendance-service:1.0
docker tag attendance-service:latest <repo>/attendance-service:latest

# Pre-release
docker tag attendance-service:latest <repo>/attendance-service:1.1.0-rc.1

# Development
docker tag attendance-service:latest <repo>/attendance-service:dev
docker tag attendance-service:latest <repo>/attendance-service:staging
```

### Push All Tags
```bash
docker push <repo>/attendance-service --all-tags
```

---

## 🔒 Security Tips

✅ **Do's:**
- ✅ Use PAT instead of password
- ✅ Store PAT in GitHub Secrets
- ✅ Scan images before pushing: `docker scan <image>`
- ✅ Sign images: `docker image sign`
- ✅ Use private repositories for sensitive images

❌ **Don'ts:**
- ❌ Don't hardcode credentials in Dockerfile
- ❌ Don't push images with sensitive data
- ❌ Don't use public registries for private code

### Scan Image for Vulnerabilities

```bash
# Using docker scan (requires login)
docker scan attendance-service:latest

# Using Trivy
trivy image attendance-service:latest
```

---

## 📊 Registry Comparison

| Feature | Docker Hub | GitHub Container Registry | GitLab | 
|---------|-----------|---------------------------|--------|
| Public Repositories | ✅ Unlimited | ✅ Unlimited | ✅ Unlimited |
| Private Repositories | ⚠️ Limited (1 free) | ✅ Unlimited | ✅ Unlimited |
| Bandwidth | 100GB/6hr | Unlimited | Unlimited |
| Price | $5/month | Free for open source | $0-99/mo |
| Integration | Docker CLI | GitHub Actions | GitLab CI |

---

## 🎯 Complete Workflow Example

```bash
#!/bin/bash
set -e

# Variables
REGISTRY="ghcr.io"
USERNAME="linhbonsochin"
IMAGE_NAME="attendance-service"
VERSION="1.0.0"

# Build
echo "🔨 Building Docker image..."
docker build -t $IMAGE_NAME:latest -f API/Dockerfile .

# Tag
echo "🏷️  Tagging image..."
docker tag $IMAGE_NAME:latest $REGISTRY/$USERNAME/$IMAGE_NAME:$VERSION
docker tag $IMAGE_NAME:latest $REGISTRY/$USERNAME/$IMAGE_NAME:latest

# Login
echo "🔑 Logging in to registry..."
echo $REGISTRY_PASSWORD | docker login $REGISTRY -u $USERNAME --password-stdin

# Push
echo "📤 Pushing to registry..."
docker push $REGISTRY/$USERNAME/$IMAGE_NAME:$VERSION
docker push $REGISTRY/$USERNAME/$IMAGE_NAME:latest

# Cleanup
echo "🧹 Cleaning up..."
docker logout $REGISTRY

echo "✅ Done! Image available at: $REGISTRY/$USERNAME/$IMAGE_NAME:$VERSION"
```

---

## 🔗 Useful Links

- [Docker Hub Documentation](https://docs.docker.com/docker-hub/)
- [GitHub Container Registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
- [Docker Security Best Practices](https://docs.docker.com/engine/security/)
- [Container Image Vulnerability Scanning](https://docs.docker.com/engine/scan/)

---

**Happy containerizing! 🚀**
