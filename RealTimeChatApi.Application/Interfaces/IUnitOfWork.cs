namespace RealTimeChatApi.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IConversationRepository Conversations { get; }
    IMessageRepository Messages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}