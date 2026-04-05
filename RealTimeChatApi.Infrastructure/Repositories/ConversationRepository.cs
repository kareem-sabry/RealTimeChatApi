using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Infrastructure.Data;

namespace RealTimeChatApi.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly AppDbContext _context;

    public ConversationRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<Conversation?> GetByIdAsync(int id)
    {
        return await _context.Conversations.Include(c=>c.Participants).ThenInclude(p=>p.User).FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Conversation?> GetConversationBetweenUsersAsync(Guid user1Id, Guid user2Id)
    {
        return await _context.Conversations.Include(c => c.Participants).Where(c => c.Participants.Count == 2 &&
            c.Participants.Any(p => p.UserId == user1Id) &&
            c.Participants.Any(p => p.UserId == user2Id)).FirstOrDefaultAsync();
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(Guid userId)
    {
        return await _context.Conversations
            .Include(c => c.Participants)
            .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAtUtc).Take(1))
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.Messages.Max(m => (DateTime?)m.SentAtUtc) ?? c.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<Conversation?> GetConversationWithMessagesAsync(int conversationId, Guid userId)
    {
        return await _context.Conversations.Include(c => c.Participants).ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.SentAtUtc).Take(50)).ThenInclude(m => m.Sender)
            .Where(c => c.Id == conversationId && c.Participants.Any(p => p.UserId == userId)).FirstOrDefaultAsync();
    }

    public async Task AddAsync(Conversation conversation)
    {
        await _context.Conversations.AddAsync(conversation);
    }

    public void Delete(Conversation conversation)
    {
        _context.Conversations.Remove(conversation);
    }

    public async Task<bool> IsUserParticipantAsync(int conversationId, Guid userId)
    {
        return await _context.ConversationParticipants.AnyAsync(cp =>
            cp.ConversationId == conversationId && cp.UserId == userId);
    }

    public async Task UpdateParticipantLastReadMessageAsync(int conversationId, Guid userId, int lastReadMessageId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (participant != null)
        {
            participant.LastReadMessageId = lastReadMessageId;
        }
    }
}
}