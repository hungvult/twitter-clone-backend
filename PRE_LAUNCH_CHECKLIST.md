# Twitter Clone Backend - Pre-Launch Checklist

## ✅ Configuration Checklist

Trước khi chạy application, hãy đảm bảo các bước sau đã hoàn thành:

### 1. Database Configuration

- [ ] SQL Server đã được cài đặt và đang chạy
- [ ] Connection string trong `appsettings.json` đã được cập nhật
- [ ] Test connection string bằng SQL Server Management Studio hoặc Azure Data Studio

**Connection String Example:**

```json
"DefaultConnection": "Server=localhost;Database=TwitterCloneDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

Hoặc với username/password:

```json
"DefaultConnection": "Server=localhost;Database=TwitterCloneDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

### 2. Google OAuth Setup

- [ ] Truy cập [Google Cloud Console](https://console.cloud.google.com/)
- [ ] Tạo project mới hoặc chọn project có sẵn
- [ ] Enable Google+ API
- [ ] Tạo OAuth 2.0 credentials (Web application)
- [ ] Thêm Authorized redirect URIs:
  - `http://localhost:3000` (frontend development)
  - `https://localhost:7xxx` (backend development)
- [ ] Copy Client ID và Client Secret
- [ ] Cập nhật vào `appsettings.json`:

```json
"Authentication": {
  "Google": {
    "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
    "ClientSecret": "YOUR_CLIENT_SECRET"
  }
}
```

### 3. JWT Secret Key

- [ ] Generate một secret key mạnh (tối thiểu 32 ký tự)
- [ ] Cập nhật vào `appsettings.json`:

```json
"Jwt": {
  "SecretKey": "your-very-strong-secret-key-at-least-32-characters-long",
  "Issuer": "TwitterCloneApi",
  "Audience": "TwitterCloneApp"
}
```

**Generate Secret Key (PowerShell):**

```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

**Generate Secret Key (Bash):**

```bash
openssl rand -base64 32
```

### 4. CORS Configuration

- [ ] Cập nhật frontend URLs trong `appsettings.json`:

```json
"Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",
    "http://localhost:3001"
  ]
}
```

### 5. File Storage

- [ ] Folder `uploads/images` sẽ được tạo tự động
- [ ] Hoặc tạo thủ công: `mkdir -p uploads/images`
- [ ] Đảm bảo application có quyền write vào folder này

---

## ✅ Pre-Run Checklist

### 1. Verify Configuration

```bash
# Check appsettings.json không còn placeholders
grep -i "YOUR_" appsettings.json
```

Nếu có kết quả → cần cập nhật values

### 2. Build Project

```bash
cd TwitterClone.Api
dotnet build
```

Expected output: `Build succeeded`

### 3. Run Migrations

```bash
dotnet ef database update
```

Expected output: `Done.`

Verify tables created:

```sql
USE TwitterCloneDb;
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES;
```

Should see: `Users`, `Tweets`, `Bookmarks`, `UserStats`

### 4. Test Run

```bash
dotnet run
```

Expected output:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7xxx
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5xxx
```

### 5. Test Swagger

- [ ] Open browser: `https://localhost:7xxx/swagger`
- [ ] Verify all endpoints are listed
- [ ] Test `/api/auth/google` endpoint (should return 400 without token)

---

## ✅ Testing Checklist

### Manual API Testing

1. **Test Auth Endpoint** (requires Google token)

   ```bash
   curl -X POST https://localhost:7xxx/api/auth/google \
     -H "Content-Type: application/json" \
     -d '{"idToken": "YOUR_GOOGLE_ID_TOKEN"}'
   ```

2. **Test Protected Endpoint**

   ```bash
   curl -X GET https://localhost:7xxx/api/users \
     -H "Authorization: Bearer YOUR_JWT_TOKEN"
   ```

3. **Test File Upload**
   ```bash
   curl -X POST https://localhost:7xxx/api/upload/images \
     -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     -F "files=@image.jpg"
   ```

### Database Verification

```sql
-- Check Users table
SELECT * FROM Users;

-- Check Tweets table
SELECT * FROM Tweets;

-- Check Bookmarks table
SELECT * FROM Bookmarks;

-- Check UserStats table
SELECT * FROM UserStats;
```

### SignalR Testing

- [ ] Install SignalR client library
- [ ] Connect to `/hubs/user` with JWT token
- [ ] Verify connection successful

---

## ✅ Common Issues & Solutions

### Issue 1: Database Connection Failed

```
Error: A network-related or instance-specific error occurred
```

**Solutions:**

- [ ] Verify SQL Server is running
- [ ] Check connection string
- [ ] Check firewall settings
- [ ] Try using `Server=localhost\\SQLEXPRESS`

### Issue 2: Google OAuth Failed

```
Error: Invalid token
```

**Solutions:**

- [ ] Verify Google Client ID in frontend matches backend
- [ ] Check token hasn't expired
- [ ] Ensure Google+ API is enabled

### Issue 3: CORS Error

```
Error: blocked by CORS policy
```

**Solutions:**

- [ ] Add frontend URL to `Cors:AllowedOrigins`
- [ ] Restart backend after config change
- [ ] Check frontend is using correct API URL

### Issue 4: File Upload Failed

```
Error: File too large
```

**Solutions:**

- [ ] Check file size limits (20MB images, 50MB videos)
- [ ] Verify file extension is allowed
- [ ] Check uploads folder exists and is writable

### Issue 5: Migration Failed

```
Error: Cannot create database
```

**Solutions:**

- [ ] Verify SQL Server allows database creation
- [ ] Try creating database manually first
- [ ] Check user permissions

---

## ✅ Production Readiness Checklist

Before deploying to production:

### Security

- [ ] Change JWT secret key to production value
- [ ] Use strong database password
- [ ] Enable HTTPS only
- [ ] Update CORS to production domain only
- [ ] Remove Swagger in production (or protect it)
- [ ] Enable rate limiting
- [ ] Add logging framework (Serilog)

### Database

- [ ] Use production SQL Server instance
- [ ] Setup automated backups
- [ ] Configure connection pooling
- [ ] Add database indexes if needed
- [ ] Setup monitoring

### Storage

- [ ] Consider migrating to Azure Blob Storage or AWS S3
- [ ] Setup CDN for static files
- [ ] Implement file cleanup strategy

### Monitoring

- [ ] Setup Application Insights / CloudWatch
- [ ] Add health check endpoints
- [ ] Configure alerts
- [ ] Setup logging aggregation

### Performance

- [ ] Add Redis caching
- [ ] Optimize database queries
- [ ] Enable response compression
- [ ] Add pagination to all list endpoints

---

## ✅ Final Verification

Run through this checklist before considering the backend "ready":

1. **Configuration**

   - [ ] All placeholders replaced in appsettings.json
   - [ ] Google OAuth working
   - [ ] JWT tokens generating correctly

2. **Database**

   - [ ] All tables created
   - [ ] Can insert/update/delete records
   - [ ] Indexes working

3. **API Endpoints**

   - [ ] Auth endpoints working
   - [ ] User endpoints working
   - [ ] Tweet endpoints working
   - [ ] Bookmark endpoints working
   - [ ] Upload endpoint working

4. **Real-time**

   - [ ] SignalR hubs connectable
   - [ ] Real-time updates working

5. **Frontend Integration**
   - [ ] Frontend can call API
   - [ ] Authentication flow works end-to-end
   - [ ] File uploads working from frontend
   - [ ] SignalR connected from frontend

---

## 🚀 Ready to Launch!

If all checkboxes are checked, your Twitter Clone Backend is ready!

Run:

```bash
# Windows
.\start.bat

# Linux/Mac
./start.sh
```

Or:

```bash
dotnet run
```

Then access:

- API: `http://localhost:5xxx`
- Swagger: `https://localhost:7xxx/swagger`

Happy coding! 🎉
