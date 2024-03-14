using BoroHFR.Data;
using BoroHFR.Hubs.Exceptions;
using BoroHFR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Hubs;

[Authorize("User")]
class ChatHub : Hub
{
    private readonly BoroHfrDbContext _dbContext;
    public ChatHub(BoroHfrDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    public async Task SubscribeToConversation(Guid conversationId)
    {
        var id = new ConversationId(conversationId);
        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            throw new NotMemberOfConversationException();
        }
        var defGroup = await _dbContext.Groups.Where(x => x.Id == user.Class.DefaultGroupId).FirstOrDefaultAsync();
        var conversation = await _dbContext.Conversations.Where(x=> x.Group==defGroup || x.Members.Contains(user) || (x.IsOpen && x.Group.Members.Contains(user)) ).FirstOrDefaultAsync(x=>x.Id == id);
        if (conversation is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        }
        else
        {
            throw new NotMemberOfConversationException();
        }
    }

    public async Task UnsubscribeFromConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId,conversationId.ToString());
    }

    public async Task<ConversationMessageId> SendMessage(Guid conversationId, string message, Guid[] attachments)
    {
        message = message.Trim();
        if (message.Length == 0)
            throw new ArgumentNullException(nameof(message));
        var id = new ConversationId(conversationId);
        var user = await GetCurrentUserAsync();
        var defGroup = await _dbContext.Groups.Where(x => x.Id == user.Class.DefaultGroupId).FirstOrDefaultAsync();
        var conversation = await _dbContext.Conversations.Where(x => x.Group == defGroup || x.Members.Contains(user) || (x.IsOpen && x.Group.Members.Contains(user))).FirstOrDefaultAsync(x => x.Id == id);

        if (conversation is not null)
        {
            var msg = new ConversationMessage() { Message = message, Sender = user, Conversation=conversation, SendTime = DateTimeOffset.UtcNow };
            conversation.Messages.Add(msg);
            var attachmentIds = attachments.Select(x => new FileId(x)).ToArray();
            var dbAttachments = await _dbContext.Files.Where(x=>attachmentIds.Contains(x.Id) && x.ClassId == user.ClassId).ToListAsync();
            msg.Attachments = dbAttachments;
            await _dbContext.SaveChangesAsync();

            await Clients.Group(conversationId.ToString())
                .SendAsync("ReceiveMessage", conversationId, msg.Sender.Id.value.ToString(), msg.Sender.Username, msg.Message, msg.Id.value.ToString(), msg.SendTime, msg.Attachments.Select(x=>new { x.Id, x.DownloadName }));
            return msg.Id;
        }
        else
        {
            throw new NotMemberOfConversationException();
        }
    }

    private async Task<User> GetCurrentUserAsync()
    {
        UserId id = new UserId(Guid.Parse(Context.User!.Claims.Single(x => x.Type == "Id").Value));
        var user = await _dbContext.Users.Include(u=>u.Class).FirstOrDefaultAsync(x=>x.Id==id);
        if (user is null)
        {
            throw new UnauthorizedAccessException();
        }
        return user;
    }
}