# Changelog

Tất cả các thay đổi đáng chú ý cho dự án này được ghi lại trong tệp này.

Dự án theo dõi [Semantic Versioning](https://semver.org/vi/)

## [Unreleased]

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

---

## [1.0.0] - 2026-01-XX

### Added

- ✨ **Check-in/Check-out System**
  - Employee attendance logging with time tracking
  - Automatic late detection and grace period handling
  - Early checkout detection

- ✨ **Attendance Analytics**
  - Monthly attendance summaries for payroll integration
  - Late/Early checkout statistics
  - Standard workday calculation (0.0-1.0 scale)
  - Overtime hour tracking

- ✨ **Leave Integration**
  - Leave request synchronization with HR Service
  - Leave summary and history endpoints
  - Leave deduction from attendance

- ✨ **API Endpoints**
  - `POST /api/attendance/check-in` - Employee check-in
  - `POST /api/attendance/check-out` - Employee check-out
  - `GET /api/attendance/history/{employeeId}` - Attendance history
  - `GET /api/attendance/monthly-summary/{employeeId}` - Monthly summary
  - `GET /api/attendance/leave-summary/{employeeId}` - Leave summary
  - `POST /api/attendance/close-month` - Close month for payroll

- ✨ **Database**
  - Entity Framework Core integration with SQL Server
  - Migration system for schema management
  - Seed data for testing (23 sample attendance records)
  - Status tracking (Valid, Late, EarlyLeave, Absent, OnLeave)

- ✨ **Docker Support**
  - Multi-stage Dockerfile for optimized image size
  - Docker Compose for local development with SQL Server & RabbitMQ
  - Environment variable configuration for deployment

- ✨ **Messaging**
  - MassTransit integration for async communication
  - RabbitMQ support for message bus

- ✨ **Security**
  - Input validation
  - Employee status verification (Active/Inactive)
  - Connection string encryption
  - HTTPS enforcement in production

- ✨ **CI/CD Pipelines**
  - GitHub Actions workflow for automated builds
  - Docker image publishing to GitHub Container Registry
  - Unit tests and code coverage
  - Security scanning (Trivy, Grype, TruffleHog)
  - Code quality analysis

- 📖 **Documentation**
  - Comprehensive README with feature overview
  - Deployment guide for Docker and Kubernetes
  - Contributing guidelines
  - API documentation (Swagger/OpenAPI)

### Changed

- Improved project structure for .NET 10 best practices
- Optimized Docker image size using publish-based multi-stage builds
- Enhanced error handling and logging

### Fixed

- Fixed assembly naming issue with spaces in csproj filename
- Resolved Docker build permission errors
- Fixed TypeName nullable reference warnings

### Security

- Ensured sensitive credentials excluded from git commits
- Added .gitignore for development artifacts
- Implemented secure connection string handling

---

## Migration Guide

### From Previous Versions

#### Database Schema Changes
```bash
cd API
dotnet ef migrations add MigrationName
dotnet ef database update
```

---

## Development Changelog

### Commits Included

**Initial Setup**
- `6fbad4d` - chore: Attendance Service backend implementation
- `3e232e9` - build: Fix Dockerfile and add seed data

**Documentation & DevOps**
- `99d9b7b` - docs: Add comprehensive README for Attendance Service
- `2b9c2a1` - docs: Add comprehensive deployment guide
- `96260d8` - devops: Add docker-compose for multi-container setup
- `1af0a0f` - ci: Add GitHub Actions workflows
- `85a858f` - docs: Add comprehensive contributing guidelines

---

## Known Issues

- [ ] MassTransit hosted service warning (AddMassTransitHostedService is obsolete)
- [ ] AutoMapper package vulnerability (NU1903) - version needs update
- [ ] Nullable reference warnings in some services (CS8618, CS8629)

## Roadmap

### Next Release (v1.1.0)
- [ ] WebSocket support for real-time attendance updates
- [ ] Mobile app integration
- [ ] Advanced reporting dashboard
- [ ] Biometric device integration
- [ ] Multi-language support

### Future (v2.0.0)
- [ ] Machine learning for anomaly detection
- [ ] AI-powered shift optimization
- [ ] Mobile app (iOS/Android)
- [ ] Microservices architecture refactoring
- [ ] Kubernetes deployment ready

---

## Contributors

- **Nguyễn Đình Bảo Linh** (nlinh3040@gmail.com) - Founder & Lead Developer

---

## License

This project is licensed under a proprietary license - BTL Full Stack Project.

For more information, see the [LICENSE](LICENSE) file.

---

## Support

- 📧 Email: nlinh3040@gmail.com
- 🐛 Issues: https://github.com/Linhbonsochin/FULL_Stack/issues
- 📚 Wiki: https://github.com/Linhbonsochin/FULL_Stack/wiki

---

## Changelog Format

This changelog follows the [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format.

### Categories

- **Added** for new features
- **Changed** for changes in existing functionality
- **Deprecated** for soon-to-be removed features
- **Removed** for now removed features
- **Fixed** for any bug fixes
- **Security** for security updates

---

Last Updated: 2026-01-XX
