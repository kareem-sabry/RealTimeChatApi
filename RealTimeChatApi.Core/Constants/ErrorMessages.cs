namespace RealTimeChatApi.Core.Constants;

public static class ErrorMessages
{
    // User errors
    public const string UserNotFound = "User not found.";
    public const string UserAlreadyExists = "A user with this email already exists.";
    public const string InvalidCredentials = "Invalid email or password.";
    public const string InvalidUserContext = "Invalid user context.";

    // Conversation errors
    public const string ConversationNotFound = "Conversation not found.";
    public const string ConversationAlreadyExists = "Conversation already exists between these users.";
    public const string CannotMessageYourself = "You cannot start a conversation with yourself.";
    public const string NotParticipantInConversation = "You are not a participant in this conversation.";

    // Message errors
    public const string MessageNotFound = "Message not found.";
    public const string MessageTooLong = "Message exceeds maximum length.";
    public const string EmptyMessage = "Message cannot be empty.";
    public const string CannotDeleteOthersMessage = "You can only delete your own messages.";

    // Token errors
    public const string RefreshTokenMissing = "Refresh token is missing.";
    public const string RefreshTokenExpired = "Refresh token has expired. Please log in again.";
    public const string RefreshTokenInvalid = "Invalid refresh token.";

    // General errors
    public const string InvalidId = "Invalid ID provided.";
    public const string OperationFailed = "The operation failed. Please try again.";
}