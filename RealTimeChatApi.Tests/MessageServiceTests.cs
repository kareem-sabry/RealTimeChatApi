using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RealTimeChatApi.Application.Interfaces;
using RealTimeChatApi.Core.Constants;
using RealTimeChatApi.Core.Entities;
using RealTimeChatApi.Core.Enums;
using RealTimeChatApi.Core.Models;
using RealTimeChatApi.Infrastructure.Data;
using RealTimeChatApi.Infrastructure.Services;
using RealTimeChatApi.Tests.Helpers;

namespace RealTimeChatApi.Tests.Unit.Services;

public class MessageServiceTests : IDisposable
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMessageRepository> _mockMessageRepo;
    private readonly Mock<IConversationRepository> _mockConversationRepo;
    private readonly Mock<ILogger<MessageService>> _mockLogger;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly AppDbContext _context;
    private readonly MessageService _sut;

    public MessageServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMessageRepo = new Mock<IMessageRepository>();
        _mockConversationRepo = new Mock<IConversationRepository>();
        _mockLogger = new Mock<ILogger<MessageService>>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();

        // in-memory database for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var mockHttpContextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var mockDateProvider = new Mock<IDateTimeProvider>();

        _context = new AppDbContext(options, mockHttpContextAccessor.Object, mockDateProvider.Object);

        _mockDateTimeProvider.Setup(x => x.UtcNow)
            .Returns(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        _mockUnitOfWork.Setup(x => x.Messages).Returns(_mockMessageRepo.Object);
        _mockUnitOfWork.Setup(x => x.Conversations).Returns(_mockConversationRepo.Object);

        _sut = new MessageService(
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockDateTimeProvider.Object,
            _context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    #region SendMessageAsync Tests

    [Fact]
    public async Task SendMessageAsync_WithValidData_ReturnsMessageDto()
    {
        // Arrange
        var conversationId = 1;
        var senderId = Guid.NewGuid();
        var content = "Hello, world!";
        var sender = TestHelper.CreateTestUser("sender@test.com", "John", "Doe");
        sender.Id = senderId;

        _mockConversationRepo.Setup(x => x.IsUserParticipantAsync(conversationId, senderId))
            .ReturnsAsync(true);

        _mockMessageRepo.Setup(x => x.AddAsync(It.IsAny<Message>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        var savedMessage = new Message
        {
            Id = 1,
            ConversationId = conversationId,
            SenderId = senderId,
            Sender = sender,
            Content = content,
            Status = MessageStatus.Sent,
            SentAtUtc = _mockDateTimeProvider.Object.UtcNow,
            IsDeleted = false
        };

        _mockMessageRepo.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(savedMessage);

        // Act
        var result = await _sut.SendMessageAsync(conversationId, senderId, content);

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().Be(content);
        result.SenderId.Should().Be(senderId);
        result.SenderName.Should().Be("John Doe");
        result.Status.Should().Be(MessageStatus.Sent);

        _mockMessageRepo.Verify(x => x.AddAsync(It.Is<Message>(m =>
            m.ConversationId == conversationId &&
            m.SenderId == senderId &&
            m.Content == content &&
            m.Status == MessageStatus.Sent &&
            m.SentAtUtc == _mockDateTimeProvider.Object.UtcNow)), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyContent_ThrowsException()
    {
        // Arrange
        var conversationId = 1;
        var senderId = Guid.NewGuid();
        var content = "";

        // Act
        var act = async () => await _sut.SendMessageAsync(conversationId, senderId, content);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage(ErrorMessages.EmptyMessage);

        _mockMessageRepo.Verify(x => x.AddAsync(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_WithTooLongContent_ThrowsException()
    {
        // Arrange
        var conversationId = 1;
        var senderId = Guid.NewGuid();
        var content = new string('x', ApplicationConstants.MaxMessageLength + 1);

        // Act
        var act = async () => await _sut.SendMessageAsync(conversationId, senderId, content);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage(ErrorMessages.MessageTooLong);

        _mockMessageRepo.Verify(x => x.AddAsync(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task SendMessageAsync_WhenUserNotParticipant_ThrowsException()
    {
        // Arrange
        var conversationId = 1;
        var senderId = Guid.NewGuid();
        var content = "Hello";

        _mockConversationRepo.Setup(x => x.IsUserParticipantAsync(conversationId, senderId))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.SendMessageAsync(conversationId, senderId, content);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(ErrorMessages.NotParticipantInConversation);

        _mockMessageRepo.Verify(x => x.AddAsync(It.IsAny<Message>()), Times.Never);
    }

    #endregion

    #region GetConversationMessagesAsync Tests

    [Fact]
    public async Task GetConversationMessagesAsync_WithValidData_ReturnsPagedMessages()
    {
        // Arrange
        var conversationId = 1;
        var userId = Guid.NewGuid();
        var sender = TestHelper.CreateTestUser("sender@test.com", "Jane", "Smith");

        var parameters = new QueryParameters
        {
            PageNumber = 1,
            PageSize = 20
        };

        _mockConversationRepo.Setup(x => x.IsUserParticipantAsync(conversationId, userId))
            .ReturnsAsync(true);

        var messages = new List<Message>
        {
            new()
            {
                Id = 1,
                ConversationId = conversationId,
                SenderId = sender.Id,
                Sender = sender,
                Content = "Message 1",
                SentAtUtc = _mockDateTimeProvider.Object.UtcNow,
                Status = MessageStatus.Sent,
                IsDeleted = false
            }
        };

        var pagedResult = new PagedResult<Message>(messages, 1, 1, 20);

        _mockMessageRepo.Setup(x => x.GetConversationMessagesAsync(conversationId, parameters))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _sut.GetConversationMessagesAsync(conversationId, userId, parameters);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().Content.Should().Be("Message 1");
        result.Items.First().SenderName.Should().Be("Jane Smith");
    }

    [Fact]
    public async Task GetConversationMessagesAsync_WhenUserNotParticipant_ThrowsException()
    {
        // Arrange
        var conversationId = 1;
        var userId = Guid.NewGuid();
        var parameters = new QueryParameters();

        _mockConversationRepo.Setup(x => x.IsUserParticipantAsync(conversationId, userId))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.GetConversationMessagesAsync(conversationId, userId, parameters);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(ErrorMessages.NotParticipantInConversation);
    }

    #endregion

    #region DeleteMessageAsync Tests

    [Fact]
    public async Task DeleteMessageAsync_WithOwnMessage_ReturnsSuccess()
    {
        // Arrange
        var messageId = 1;
        var userId = Guid.NewGuid();

        var message = new Message
        {
            Id = messageId,
            SenderId = userId,
            Content = "Original content",
            IsDeleted = false
        };

        _mockMessageRepo.Setup(x => x.GetByIdAsync(messageId))
            .ReturnsAsync(message);

        _mockMessageRepo.Setup(x => x.Update(message));

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteMessageAsync(messageId, userId);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.MessageDeleted);

        message.IsDeleted.Should().BeTrue();
        message.Content.Should().Be("[Message deleted]");

        _mockMessageRepo.Verify(x => x.Update(message), Times.Once);
    }

    [Fact]
    public async Task DeleteMessageAsync_WithNonExistentMessage_ReturnsFailure()
    {
        // Arrange
        var messageId = 999;
        var userId = Guid.NewGuid();

        _mockMessageRepo.Setup(x => x.GetByIdAsync(messageId))
            .ReturnsAsync((Message?)null);

        // Act
        var result = await _sut.DeleteMessageAsync(messageId, userId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.MessageNotFound);

        _mockMessageRepo.Verify(x => x.Update(It.IsAny<Message>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMessageAsync_WithOtherUsersMessage_ReturnsFailure()
    {
        // Arrange
        var messageId = 1;
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var message = new Message
        {
            Id = messageId,
            SenderId = otherUserId,
            Content = "Someone else's message"
        };

        _mockMessageRepo.Setup(x => x.GetByIdAsync(messageId))
            .ReturnsAsync(message);

        // Act
        var result = await _sut.DeleteMessageAsync(messageId, userId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.CannotDeleteOthersMessage);

        _mockMessageRepo.Verify(x => x.Update(It.IsAny<Message>()), Times.Never);
    }

    #endregion

    #region MarkMessagesAsReadAsync Tests

    [Fact]
    public async Task MarkMessagesAsReadAsync_WithUnreadMessages_ReturnsSuccess()
    {
        // Arrange
        var conversationId = 1;
        var userId = Guid.NewGuid();

        _mockConversationRepo.Setup(x => x.IsUserParticipantAsync(conversationId, userId))
            .ReturnsAsync(true);

        var unreadMessages = new List<Message>
        {
            new()
            {
                Id = 1,
                Status = MessageStatus.Sent,
                SenderId = Guid.NewGuid()
            },
            new()
            {
                Id = 2,
                Status = MessageStatus.Delivered,
                SenderId = Guid.NewGuid()
            }
        };

        _mockMessageRepo.Setup(x => x.GetUnreadMessagesAsync(conversationId, userId))
            .ReturnsAsync(unreadMessages);

        // Add participant to in-memory database
        var participant = new ConversationParticipant
        {
            ConversationId = conversationId,
            UserId = userId,
            JoinedAtUtc = _mockDateTimeProvider.Object.UtcNow,
            CreatedAtUtc = _mockDateTimeProvider.Object.UtcNow
        };

        _context.ConversationParticipants.Add(participant);
        await _context.SaveChangesAsync();

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.MarkMessagesAsReadAsync(conversationId, userId);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Message.Should().Be(SuccessMessages.MessageMarkedAsRead);

        unreadMessages.Should().AllSatisfy(m => m.Status.Should().Be(MessageStatus.Read));

        _mockMessageRepo.Verify(x => x.Update(It.IsAny<Message>()), Times.Exactly(2));
    }

    [Fact]
    public async Task MarkMessagesAsReadAsync_WhenUserNotParticipant_ReturnsFailure()
    {
        // Arrange
        var conversationId = 1;
        var userId = Guid.NewGuid();

        _mockConversationRepo.Setup(x => x.IsUserParticipantAsync(conversationId, userId))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.MarkMessagesAsReadAsync(conversationId, userId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be(ErrorMessages.NotParticipantInConversation);

        _mockMessageRepo.Verify(x => x.Update(It.IsAny<Message>()), Times.Never);
    }

    #endregion

    #region GetUnreadCountAsync Tests

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var conversationId = 1;
        var userId = Guid.NewGuid();

        _mockMessageRepo.Setup(x => x.GetUnreadCountAsync(conversationId, userId))
            .ReturnsAsync(5);

        // Act
        var result = await _sut.GetUnreadCountAsync(conversationId, userId);

        // Assert
        result.Should().Be(5);
    }

    #endregion
}