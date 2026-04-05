using Microsoft.EntityFrameworkCore;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Core.Models;
using RealTimeChatApi.Infrastructure.Data;

namespace RealTimeChatApi.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly AppDbContext _context;

    public MessageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Message>> GetConversationMessagesAsync(int conversationId, QueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.SentAtUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var messages = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Message>(messages, totalCount, parameters.PageNumber, parameters.PageSize);
    }

    public async Task AddAsync(Message message, CancellationToken cancellationToken = default)
    {
        await _context.Messages.AddAsync(message, cancellationToken);
    }

    public void Update(Message message)
    {
        _context.Messages.Update(message);
    }

    public void Delete(Message message)
    {
        _context.Messages.Remove(message);
    }

    public async Task<int> GetUnreadCountAsync(int conversationId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId, cancellationToken);

        if (participant == null)
            return 0;

        var lastReadMessageId = participant.LastReadMessageId ?? 0;

        return await _context.Messages
            .Where(m => m.ConversationId == conversationId &&
                        m.Id > lastReadMessageId &&
                        m.SenderId != userId &&
                        !m.IsDeleted)
            .CountAsync(cancellationToken);
    }

    public async Task<List<Message>> GetUnreadMessagesAsync(int conversationId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId, cancellationToken);

        if (participant == null)
            return new List<Message>();

        var lastReadMessageId = participant.LastReadMessageId ?? 0;

        return await _context.Messages
            .Where(m => m.ConversationId == conversationId &&
                        m.Id > lastReadMessageId &&
                        m.SenderId != userId &&
                        !m.IsDeleted)
            .ToListAsync(cancellationToken);
    }
}