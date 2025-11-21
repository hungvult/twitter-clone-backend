# Hướng dẫn kết nối Frontend với Backend ASP.NET

## 1. Cấu hình Backend

### Bước 1: Cập nhật `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=TwitterCloneDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    },
    "Jwt": {
      "SecretKey": "your-secret-key-at-least-32-characters-long-for-security",
      "Issuer": "TwitterCloneApi",
      "Audience": "TwitterCloneApp",
      "AccessTokenExpirationMinutes": 60,
      "RefreshTokenExpirationDays": 30
    }
  },
  "Storage": {
    "LocalPath": "./uploads"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:3001"]
  }
}
```

### Bước 2: Chạy Migration

```bash
cd TwitterClone.Api
dotnet ef database update
```

### Bước 3: Chạy Backend

```bash
dotnet run
```

Backend sẽ chạy tại:

- HTTPS: `https://localhost:7xxx`
- HTTP: `http://localhost:5xxx`

Kiểm tra Swagger UI tại: `https://localhost:7xxx/swagger`

---

## 2. Cấu hình Frontend

### Bước 1: Tạo `.env.local` trong folder frontend

```env
# API Base URL
NEXT_PUBLIC_API_URL=http://localhost:5xxx

# Google OAuth (same credentials as backend)
NEXT_PUBLIC_GOOGLE_CLIENT_ID=YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com

# SignalR Hubs
NEXT_PUBLIC_USER_HUB_URL=http://localhost:5xxx/hubs/user
NEXT_PUBLIC_TWEET_HUB_URL=http://localhost:5xxx/hubs/tweet
NEXT_PUBLIC_BOOKMARK_HUB_URL=http://localhost:5xxx/hubs/bookmark
```

### Bước 2: Cài đặt packages cần thiết

```bash
npm install @microsoft/signalr
npm install @react-oauth/google
```

### Bước 3: Tạo API client

Tạo file `src/lib/api/client.ts`:

```typescript
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

export class ApiClient {
  private token: string | null = null;

  setToken(token: string) {
    this.token = token;
    localStorage.setItem("jwt_token", token);
  }

  getToken() {
    if (!this.token) {
      this.token = localStorage.getItem("jwt_token");
    }
    return this.token;
  }

  clearToken() {
    this.token = null;
    localStorage.removeItem("jwt_token");
  }

  async request<T>(endpoint: string, options?: RequestInit): Promise<T> {
    const token = this.getToken();
    const headers: HeadersInit = {
      "Content-Type": "application/json",
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options?.headers,
    };

    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      ...options,
      headers,
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || "API request failed");
    }

    return response.json();
  }

  // Auth
  async loginWithGoogle(idToken: string) {
    return this.request<{
      accessToken: string;
      refreshToken: string;
      user: any;
    }>("/api/auth/google", {
      method: "POST",
      body: JSON.stringify({ idToken }),
    });
  }

  // Users
  async getUser(id: string) {
    return this.request<any>(`/api/users/${id}`);
  }

  async updateProfile(id: string, data: any) {
    return this.request<any>(`/api/users/${id}`, {
      method: "PATCH",
      body: JSON.stringify(data),
    });
  }

  async followUser(id: string) {
    return this.request<any>(`/api/users/${id}/follow`, {
      method: "POST",
    });
  }

  async unfollowUser(id: string, targetId: string) {
    return this.request<any>(`/api/users/${id}/follow/${targetId}`, {
      method: "DELETE",
    });
  }

  // Tweets
  async getTweets(limit: number = 20, before?: string) {
    const query = new URLSearchParams({ limit: limit.toString() });
    if (before) query.append("before", before);
    return this.request<any[]>(`/api/tweets?${query}`);
  }

  async getTweet(id: string) {
    return this.request<any>(`/api/tweets/${id}`);
  }

  async createTweet(data: any) {
    return this.request<any>("/api/tweets", {
      method: "POST",
      body: JSON.stringify(data),
    });
  }

  async deleteTweet(id: string) {
    return this.request<void>(`/api/tweets/${id}`, {
      method: "DELETE",
    });
  }

  async likeTweet(id: string) {
    return this.request<any>(`/api/tweets/${id}/like`, {
      method: "POST",
    });
  }

  async unlikeTweet(id: string) {
    return this.request<any>(`/api/tweets/${id}/like`, {
      method: "DELETE",
    });
  }

  async retweet(id: string) {
    return this.request<any>(`/api/tweets/${id}/retweet`, {
      method: "POST",
    });
  }

  async unretweet(id: string) {
    return this.request<any>(`/api/tweets/${id}/retweet`, {
      method: "DELETE",
    });
  }

  // Bookmarks
  async getBookmarks(userId: string) {
    return this.request<any[]>(`/api/bookmarks/user/${userId}`);
  }

  async bookmarkTweet(tweetId: string) {
    return this.request<any>(`/api/bookmarks/tweet/${tweetId}`, {
      method: "POST",
    });
  }

  async removeBookmark(tweetId: string) {
    return this.request<any>(`/api/bookmarks/tweet/${tweetId}`, {
      method: "DELETE",
    });
  }

  // Upload
  async uploadImages(files: File[]) {
    const formData = new FormData();
    files.forEach((file) => formData.append("files", file));

    const token = this.getToken();
    const response = await fetch(`${API_BASE_URL}/api/upload/images`, {
      method: "POST",
      headers: {
        ...(token && { Authorization: `Bearer ${token}` }),
      },
      body: formData,
    });

    if (!response.ok) {
      throw new Error("Upload failed");
    }

    return response.json();
  }
}

export const apiClient = new ApiClient();
```

### Bước 4: Setup SignalR

Tạo file `src/lib/signalr/connection.ts`:

```typescript
import * as signalR from "@microsoft/signalr";

const USER_HUB_URL =
  process.env.NEXT_PUBLIC_USER_HUB_URL || "http://localhost:5000/hubs/user";
const TWEET_HUB_URL =
  process.env.NEXT_PUBLIC_TWEET_HUB_URL || "http://localhost:5000/hubs/tweet";
const BOOKMARK_HUB_URL =
  process.env.NEXT_PUBLIC_BOOKMARK_HUB_URL ||
  "http://localhost:5000/hubs/bookmark";

export class SignalRService {
  private userConnection: signalR.HubConnection | null = null;
  private tweetConnection: signalR.HubConnection | null = null;
  private bookmarkConnection: signalR.HubConnection | null = null;

  async connectUserHub(token: string) {
    this.userConnection = new signalR.HubConnectionBuilder()
      .withUrl(USER_HUB_URL, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    await this.userConnection.start();
    return this.userConnection;
  }

  async connectTweetHub(token: string) {
    this.tweetConnection = new signalR.HubConnectionBuilder()
      .withUrl(TWEET_HUB_URL, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    await this.tweetConnection.start();
    return this.tweetConnection;
  }

  async subscribeToUser(userId: string) {
    if (this.userConnection) {
      await this.userConnection.invoke("SubscribeToUser", userId);
    }
  }

  async subscribeToTweet(tweetId: string) {
    if (this.tweetConnection) {
      await this.tweetConnection.invoke("SubscribeToTweet", tweetId);
    }
  }

  onUserUpdated(callback: (user: any) => void) {
    this.userConnection?.on("UserUpdated", callback);
  }

  onTweetLiked(callback: (data: any) => void) {
    this.tweetConnection?.on("TweetLiked", callback);
  }

  onTweetRetweeted(callback: (data: any) => void) {
    this.tweetConnection?.on("TweetRetweeted", callback);
  }

  disconnect() {
    this.userConnection?.stop();
    this.tweetConnection?.stop();
    this.bookmarkConnection?.stop();
  }
}

export const signalRService = new SignalRService();
```

### Bước 5: Migrate Firebase code sang API calls

Thay thế các Firebase functions trong `src/lib/firebase/`:

**Auth (`src/lib/firebase/auth.ts`):**

```typescript
import { apiClient } from "@/lib/api/client";
import { GoogleAuthProvider, signInWithPopup } from "firebase/auth";
import { auth } from "./app";

export async function signInWithGoogle() {
  const provider = new GoogleAuthProvider();
  const result = await signInWithPopup(auth, provider);

  // Get ID token from Firebase
  const idToken = await result.user.getIdToken();

  // Send to backend
  const response = await apiClient.loginWithGoogle(idToken);
  apiClient.setToken(response.accessToken);

  return response.user;
}
```

**Users (`src/lib/firebase/users.ts`):**

```typescript
import { apiClient } from "@/lib/api/client";

export async function getUser(id: string) {
  return apiClient.getUser(id);
}

export async function updateUserProfile(id: string, data: any) {
  return apiClient.updateProfile(id, data);
}

export async function followUser(targetUserId: string) {
  return apiClient.followUser(targetUserId);
}

export async function unfollowUser(
  currentUserId: string,
  targetUserId: string
) {
  return apiClient.unfollowUser(currentUserId, targetUserId);
}
```

**Tweets (`src/lib/firebase/tweets.ts`):**

```typescript
import { apiClient } from "@/lib/api/client";

export async function getTweets(limit: number = 20) {
  return apiClient.getTweets(limit);
}

export async function createTweet(data: any) {
  return apiClient.createTweet(data);
}

export async function deleteTweet(id: string) {
  return apiClient.deleteTweet(id);
}

export async function likeTweet(id: string) {
  return apiClient.likeTweet(id);
}

export async function retweet(id: string) {
  return apiClient.retweet(id);
}
```

---

## 3. Mapping Firebase → ASP.NET API

| Firebase Function            | ASP.NET Endpoint                    | Method |
| ---------------------------- | ----------------------------------- | ------ |
| `signInWithGoogle()`         | `/api/auth/google`                  | POST   |
| `getUser(id)`                | `/api/users/{id}`                   | GET    |
| `updateUser(id, data)`       | `/api/users/{id}`                   | PATCH  |
| `followUser(id)`             | `/api/users/{id}/follow`            | POST   |
| `unfollowUser(id, targetId)` | `/api/users/{id}/follow/{targetId}` | DELETE |
| `getTweets()`                | `/api/tweets`                       | GET    |
| `getTweet(id)`               | `/api/tweets/{id}`                  | GET    |
| `createTweet(data)`          | `/api/tweets`                       | POST   |
| `deleteTweet(id)`            | `/api/tweets/{id}`                  | DELETE |
| `likeTweet(id)`              | `/api/tweets/{id}/like`             | POST   |
| `unlikeTweet(id)`            | `/api/tweets/{id}/like`             | DELETE |
| `retweet(id)`                | `/api/tweets/{id}/retweet`          | POST   |
| `unretweet(id)`              | `/api/tweets/{id}/retweet`          | DELETE |
| `getBookmarks(userId)`       | `/api/bookmarks/user/{userId}`      | GET    |
| `bookmarkTweet(tweetId)`     | `/api/bookmarks/tweet/{tweetId}`    | POST   |
| `removeBookmark(tweetId)`    | `/api/bookmarks/tweet/{tweetId}`    | DELETE |
| Firebase Storage upload      | `/api/upload/images`                | POST   |

---

## 4. Testing Checklist

### Backend Tests:

- [ ] Chạy `dotnet run` thành công
- [ ] Truy cập Swagger UI
- [ ] Test POST `/api/auth/google` với valid Google token
- [ ] Test protected endpoints với JWT token

### Frontend Tests:

- [ ] Google OAuth login thành công
- [ ] JWT token được lưu vào localStorage
- [ ] API calls include Authorization header
- [ ] Timeline hiển thị tweets
- [ ] Create tweet thành công
- [ ] Like/Unlike hoạt động
- [ ] Follow/Unfollow hoạt động
- [ ] Upload ảnh thành công
- [ ] SignalR real-time updates hoạt động

---

## 5. Common Issues & Solutions

### Issue: CORS Error

**Solution:** Kiểm tra frontend URL có trong `Cors:AllowedOrigins` trong appsettings.json

### Issue: 401 Unauthorized

**Solution:** Kiểm tra JWT token trong localStorage và Authorization header

### Issue: Google OAuth fails

**Solution:** Đảm bảo Google ClientId giống nhau ở frontend và backend

### Issue: File upload fails

**Solution:** Kiểm tra file size limit và file types

### Issue: SignalR connection fails

**Solution:** Đảm bảo JWT token valid và hub URLs correct

---

## 6. Development Workflow

1. **Backend changes:**

   ```bash
   cd TwitterClone.Api
   dotnet watch run  # Auto-reload on changes
   ```

2. **Frontend changes:**

   ```bash
   cd twitter-clone-frontend
   npm run dev
   ```

3. **Database changes:**
   ```bash
   dotnet ef migrations add MigrationName
   dotnet ef database update
   ```

---

## 7. Production Deployment (Future)

### Backend:

- [ ] Deploy to Azure App Service / AWS / DigitalOcean
- [ ] Setup production SQL Server database
- [ ] Configure environment variables
- [ ] Enable HTTPS
- [ ] Update CORS for production domain

### Frontend:

- [ ] Update `NEXT_PUBLIC_API_URL` to production URL
- [ ] Deploy to Vercel / Netlify
- [ ] Update Google OAuth redirect URIs

---

Để được hỗ trợ thêm, xem:

- `README.md` - Setup instructions
- `IMPLEMENTATION_SUMMARY.md` - Feature overview
- Swagger UI - API documentation
