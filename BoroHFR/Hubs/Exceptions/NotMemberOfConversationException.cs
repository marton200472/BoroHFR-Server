using Microsoft.AspNetCore.SignalR;

namespace BoroHFR.Hubs.Exceptions;

public class NotMemberOfConversationException : HubException
{
    public NotMemberOfConversationException() : base("not_member_of_conversation") { }
}