# Twitter Clone - ASP.NET Core 8 Backend

Backend API cho Twitter Clone sử dụng ASP.NET Core 8, SQL Server, và SignalR.

## Công nghệ

- **Framework**: ASP.NET Core 8 Web API
- **Database**: SQL Server
- **Authentication**: JWT + Google OAuth 2.0
- **ORM**: Entity Framework Core 8
- **Real-time**: SignalR
- **Storage**: Local File System

## Cấu trúc Project

```
TwitterClone.Api/
├── Controllers/          # API Controllers
│   ├── AuthController.cs
│   ├── UsersController.cs
│   ├── TweetsController.cs
│   ├── BookmarksController.cs
│   └── UploadController.cs
├── Services/            # Business Logic
│   ├── AuthService.cs
│   ├── UserService.cs
│   ├── TweetService.cs
│   └── BookmarkService.cs
├── Models/
│   ├── Entities/        # Database Entities
│   └── DTOs/            # Data Transfer Objects
├── Data/                # DbContext
├── Hubs/                # SignalR Hubs
├── Mappings/            # AutoMapper Profiles
└── uploads/             # Local File Storage
```

## Setup

### 1. Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB hoặc Express)
- Google OAuth Client ID & Secret

### 2. Cấu hình Database

Mở `appsettings.json` và cập nhật connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=TwitterCloneDb;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3. Cấu hình Google OAuth

Truy cập [Google Cloud Console](https://console.cloud.google.com/) để tạo OAuth credentials, sau đó cập nhật:

```json
"Authentication": {
  "Google": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  },
  "Jwt": {
    "SecretKey": "YOUR_SECRET_KEY_AT_LEAST_32_CHARACTERS_LONG",
    "Issuer": "TwitterCloneApi",
    "Audience": "TwitterCloneApp"
  }
}
```

### 4. Chạy Migration

```bash
cd TwitterClone.Api
dotnet ef database update
```

### 5. Chạy Application

```bash
dotnet run
```

API sẽ chạy tại: `https://localhost:7xxx` và `http://localhost:5xxx`

## API Endpoints

### Authentication

- `POST /api/auth/google` - Đăng nhập với Google
- `POST /api/auth/refresh` - Refresh JWT token
- `POST /api/auth/signout` - Đăng xuất

### Users

- `GET /api/users` - Lấy danh sách users
- `GET /api/users/{id}` - Lấy user theo ID
- `GET /api/users/username/{username}` - Lấy user theo username
- `GET /api/users/check-username/{username}` - Kiểm tra username khả dụng
- `PATCH /api/users/{id}` - Cập nhật profile
- `PATCH /api/users/{id}/username` - Cập nhật username
- `PATCH /api/users/{id}/theme` - Cập nhật theme
- `GET /api/users/{id}/followers` - Lấy followers
- `GET /api/users/{id}/following` - Lấy following
- `POST /api/users/{id}/follow` - Follow user
- `DELETE /api/users/{id}/follow/{targetUserId}` - Unfollow user
- `POST /api/users/{id}/pin-tweet` - Pin tweet
- `DELETE /api/users/{id}/pin-tweet` - Unpin tweet

### Tweets

- `GET /api/tweets` - Lấy timeline
- `GET /api/tweets/{id}` - Lấy tweet theo ID
- `POST /api/tweets` - Tạo tweet mới
- `DELETE /api/tweets/{id}` - Xóa tweet
- `GET /api/tweets/{id}/replies` - Lấy replies
- `GET /api/tweets/user/{userId}` - Lấy tweets của user
- `GET /api/tweets/user/{userId}/media` - Lấy media tweets
- `GET /api/tweets/user/{userId}/likes` - Lấy liked tweets
- `POST /api/tweets/{id}/like` - Like tweet
- `DELETE /api/tweets/{id}/like` - Unlike tweet
- `POST /api/tweets/{id}/retweet` - Retweet
- `DELETE /api/tweets/{id}/retweet` - Undo retweet

### Bookmarks

- `GET /api/bookmarks/user/{userId}` - Lấy bookmarks
- `POST /api/bookmarks/tweet/{tweetId}` - Bookmark tweet
- `DELETE /api/bookmarks/tweet/{tweetId}` - Remove bookmark
- `DELETE /api/bookmarks/user/{userId}` - Clear all bookmarks

### Upload

- `POST /api/upload/images` - Upload hình ảnh/video (multipart/form-data)

### SignalR Hubs

- `/hubs/user` - Real-time user updates
- `/hubs/tweet` - Real-time tweet updates
- `/hubs/bookmark` - Real-time bookmark updates

## Database Schema

### Users

- Id, Name, Username (unique), Email (unique)
- Bio, PhotoURL, CoverPhotoURL, Website, Location
- Verified, Theme, Accent
- Following, Followers (JSON arrays)
- TotalTweets, TotalPhotos
- PinnedTweet, CreatedAt, UpdatedAt

### Tweets

- Id, Text, Images (JSON)
- ParentId, ParentUsername (for replies)
- CreatedBy (Foreign Key → Users)
- UserLikes, UserRetweets (JSON arrays)
- UserReplies (count)
- CreatedAt, UpdatedAt

### Bookmarks

- Id, UserId, TweetId
- Composite unique index on (UserId, TweetId)
- CreatedAt

### UserStats

- Id, UserId
- Likes, Tweets (JSON arrays of tweet IDs)
- UpdatedAt

## File Upload

Files được lưu tại: `./uploads/images/{userId}/{imageId}.{ext}`

**Giới hạn:**

- Images: 20MB (png, jpg, gif, webp, svg, avif)
- Videos: 50MB (mp4, mov, webm, avi, mkv)
- Tối đa 4 files mỗi lần upload

## Authentication

API sử dụng JWT Bearer tokens. Include token trong header:

```
Authorization: Bearer {your_jwt_token}
```

## Admin User

User với username `ccrsxx` có quyền admin:

- Xóa bất kỳ tweet nào
- Tweet tối đa 560 ký tự (thay vì 280)

## Development Notes

- Tất cả arrays (Following, Followers, UserLikes, etc.) được lưu dưới dạng JSON strings trong SQL Server
- Real-time updates sử dụng SignalR (frontend cần subscribe vào các hubs)
- CORS đã được cấu hình cho `http://localhost:3000`

## TODO (Optional Enhancements)

- [ ] Implement refresh token storage in database
- [ ] Add rate limiting middleware
- [ ] Add Redis caching
- [ ] Implement search functionality
- [ ] Add email notifications
- [ ] Add Twitter API proxy for trends
- [ ] Add comprehensive logging
- [ ] Add unit and integration tests
