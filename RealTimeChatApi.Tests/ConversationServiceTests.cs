using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;
using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Infrastructure.Services;
using RealTimeChatApi.Tests.Helpers;

namespace RealTimeChatApi.Tests;

public class ConversationServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IConversationRepository> _mockConversationRepo;
    private readonly Mock<IMessageRepository> _mockMessageRepo;
    private readonly Mock<ILogger<ConversationService>> _mockLogger;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly ConversationService _sut;

    public ConversationServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockConversationRepo = new Mock<IConversationRepository>();
        _mockMessageRepo = new Mock<IMessageRepository>();
        _mockLogger = new Mock<ILogger<ConversationService>>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();

        _mockDateTimeProvider.Setup(x => x.UtcNow)
            .Returns(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        _mockUnitOfWork.Setup(x => x.Conversations).Returns(_mockConversationRepo.Object);
        _mockUnitOfWork.Setup(x => x.Messages).Returns(_mockMessageRepo.Object);

        _sut = new ConversationService(
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockDateTimeProvider.Object);
    }

    #region GetUserConversationsAsync Tests

    [Fact]
    public async Task GetUserConversationsAsync_WithExistingConversations_ReturnsConversationList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUser = TestHelper.CreateTestUser("other@test.com", "Jane", "Smith");
        otherUser.IsOnline = true;

        var conversation = new Conversation
        {
            Id = 1,
            CreatedAtUtc = _mockDateTimeProvider.Object.UtcNow,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = userId, User = TestHelper.CreateTestUser() },
                new() { UserId = otherUser.Id, User = otherUser }
            },
            Messages = new List<Message>
            {
                new()
                {
                    Id = 1,
                    Content = "Hello!",
                    SentAtUtc = _mockDateTimeProvider.Object.UtcNow,
                    SenderId = otherUser.Id
                }
            }
        };

        _mockConversationRepo.Setup(x => x.GetUserConversationsAsync(userId))
            .ReturnsAsync(new List<Conversation> { conversation });

        _mockMessageRepo.Setup(x => x.GetUnreadCountAsync(conversation.Id, userId))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.GetUserConversationsAsync(userId);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
        result[0].OtherUserName.Should().Be("Jane Smith");
        result[0].OtherUserIsOnline.Should().BeTrue();
        result[0].LastMessageContent.Should().Be("Hello!");
        result[0].UnreadCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUserConversationsAsync_WithNoConversations_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockConversationRepo.Setup(x => x.GetUserConversationsAsync(userId))
            .ReturnsAsync(new List<Conversation>());

        // Act
        var result = await _sut.GetUserConversationsAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetConversationByIdAsync Tests

    [Fact]
    public async Task GetConversationByIdAsync_WithValidId_ReturnsConversationDetail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUser = TestHelper.CreateTestUser("other@test.com", "Jane", "Smith");
        var conversationId = 1;

        var conversation = new Conversation
        {
            Id = conversationId,
            CreatedAtUtc = _mockDateTimeProvider.Object.UtcNow,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = userId, User = TestHelper.CreateTestUser() },
                new() { UserId = otherUser.Id, User = otherUser }
            },
            Messages = new List<Message>
            {
                new()
                {
                    Id = 1,
                    ConversationId = conversationId,
                    SenderId = otherUser.Id,
                    Sender = otherUser,
                    Content = "Hello!",
                    SentAtUtc = _mockDateTimeProvider.Object.UtcNow,
                    IsDeleted = false
                }
            }
        };

        _mockConversationRepo.Setup(x => x.GetConversationWithMessagesAsync(conversationId, userId))
            .ReturnsAsync(conversation);

        // Act
        var result = await _sut.GetConversationByIdAsync(conversationId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(conversationId);
        result.OtherUserName.Should().Be("Jane Smith");
        result.Messages.Should().HaveCount(1);
        result.Messages[0].Content.Should().Be("Hello!");
    }

    [Fact]
    public async Task GetConversationByIdAsync_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversationId = 999;

        _mockConversationRepo.Setup(x => x.GetConversationWithMessagesAsync(conversationId, userId))
            .ReturnsAsync((Conversation?)null);

        // Act
        var result = await _sut.GetConversationByIdAsync(conversationId, userId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region StartConversationAsync Tests

    [Fact]
    public async Task StartConversationAsync_WithNewUsers_CreatesConversation()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var otherUser = TestHelper.CreateTestUser("other@test.com");
        otherUser.Id = otherUserId;

        _mockConversationRepo.Setup(x => x.GetConversationBetweenUsersAsync(currentUserId, otherUserId))
            .ReturnsAsync((Conversation?)null);

        _mockConversationRepo.Setup(x => x.AddAsync(It.IsAny<Conversation>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync())
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync())
            .Returns(Task.CompletedTask);

        var createdConversation = new Conversation
        {
            Id = 1,
            CreatedAtUtc = _mockDateTimeProvider.Object.UtcNow,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = currentUserId, User = TestHelper.CreateTestUser() },
                new() { UserId = otherUserId, User = otherUser }
            },
            Messages = new List<Message>()
        };

        _mockConversationRepo.Setup(x => x.GetConversationWithMessagesAsync(It.IsAny<int>(), currentUserId))
            .ReturnsAsync(createdConversation);

        // Act
        var result = await _sut.StartConversationAsync(currentUserId, otherUserId);

        // Assert
        result.Should().NotBeNull();
        result.OtherUserId.Should().Be(otherUserId);

        _mockConversationRepo.Verify(x => x.AddAsync(It.IsAny<Conversation>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task StartConversationAsync_WithExistingConversation_ReturnsExisting()
    {
        // Arrange
        var currentUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var otherUser = TestHelper.CreateTestUser("other@test.com");
        otherUser.Id = otherUserId;

        var existingConversation = new Conversation
        {
            Id = 1,
            CreatedAtUtc = _mockDateTimeProvider.Object.UtcNow,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = currentUserId, User = TestHelper.CreateTestUser() },
                new() { UserId = otherUserId, User = otherUser }
            },
            Messages = new List<Message>()
        };

        _mockConversationRepo.Setup(x => x.GetConversationBetweenUsersAsync(currentUserId, otherUserId))
            .ReturnsAsync(existingConversation);

        _mockConversationRepo.Setup(x => x.GetConversationWithMessagesAsync(existingConversation.Id, currentUserId))
            .ReturnsAsync(existingConversation);

        // Act
        var result = await _sut.StartConversationAsync(currentUserId, otherUserId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);

        _mockConversationRepo.Verify(x => x.AddAsync(It.IsAny<Conversation>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task StartConversationAsync_WithSameUser_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var act = async () => await _sut.StartConversationAsync(userId, userId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(ErrorMessages.CannotMessageYourself);
    }

    #endregion

    #region DeleteConversationAsync Tests

    [Fact]
    public async Task DeleteConversationAsync_WithValidConversation_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversationId = 1;

        var conversation = new Conversation
        {
            Id = conversationId,
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = userId }
            }
        };

        _mockConversationRepo.Setup(x => x.GetByIdAsync(conversationId))
            .ReturnsAsync(conversation);

        _mockConversationRepo.Setup(x => x.IsUserParticipantAsync(conversationId, userId))
            .ReturnsAsync(true);

        _mockConversationRepo.Setup(x => x.Delete(conversation));

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteConversationAsync(conversationId, userId);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.ConversationDeleted);

        _mockConversationRepo.Verify(x => x.Delete(conversation), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteConversationAsync_WithNonExistentConversation_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversationId = 999;

        _mockConversationRepo.Setup(x => x.GetByIdAsync(conversationId))
            .ReturnsAsync((Conversation?)null);

        // Act
        var result = await _sut.DeleteConversationAsync(conversationId, userId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.ConversationNotFound);

        _mockConversationRepo.Verify(x => x.Delete(It.IsAny<Conversation>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConversationAsync_WhenUserNotParticipant_ReturnsFailure()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var conversationId = 1;

        var conversation = new Conversation
        {
            Id = conversationId
        };

        _mockConversationRepo.Setup(x => x.GetByIdAsync(conversationId))
            .ReturnsAsync(conversation);

        _mockConversationRepo.Setup(x => x.IsUserParticipantAsync(conversationId, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.DeleteConversationAsync(conversationId, userId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.NotParticipantInConversation);

        _mockConversationRepo.Verify(x => x.Delete(It.IsAny<Conversation>()), Times.Never);
    }

    #endregion
}