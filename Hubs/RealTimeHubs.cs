using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TwitterClone.Api.Hubs;

[Authorize]
public class UserHub : Hub
{
    public async Task SubscribeToUser(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task UnsubscribeFromUser(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    // Methods to be called from server-side
    public async Task NotifyUserUpdated(string userId, object userData)
    {
        await Clients.Group($"user_{userId}").SendAsync("UserUpdated", userData);
    }

    public async Task NotifyFollowersChanged(string userId, int followerCount)
    {
        await Clients.Group($"user_{userId}").SendAsync("FollowersChanged", followerCount);
    }
}

[Authorize]
public class TweetHub : Hub
{
    public async Task SubscribeToTweet(string tweetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tweet_{tweetId}");
    }

    public async Task UnsubscribeFromTweet(string tweetId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tweet_{tweetId}");
    }

    // Methods to be called from server-side
    public async Task NotifyTweetLiked(string tweetId, int likeCount)
    {
        await Clients.Group($"tweet_{tweetId}").SendAsync("TweetLiked", new { tweetId, likeCount });
    }

    public async Task NotifyTweetRetweeted(string tweetId, int retweetCount)
    {
        await Clients.Group($"tweet_{tweetId}").SendAsync("TweetRetweeted", new { tweetId, retweetCount });
    }

    public async Task NotifyReplyAdded(string tweetId, object replyData)
    {
        await Clients.Group($"tweet_{tweetId}").SendAsync("ReplyAdded", replyData);
    }
}

[Authorize]
public class BookmarkHub : Hub
{
    public async Task SubscribeToBookmarks(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"bookmarks_{userId}");
    }

    public async Task UnsubscribeFromBookmarks(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"bookmarks_{userId}");
    }

    // Methods to be called from server-side
    public async Task NotifyBookmarkAdded(string userId, object bookmarkData)
    {
        await Clients.Group($"bookmarks_{userId}").SendAsync("BookmarkAdded", bookmarkData);
    }

    public async Task NotifyBookmarkRemoved(string userId, string tweetId)
    {
        await Clients.Group($"bookmarks_{userId}").SendAsync("BookmarkRemoved", tweetId);
    }
}
