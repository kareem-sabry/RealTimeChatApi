namespace RealTimeChatApi.Core.Constants;

public static class SuccessMessages
{
    // User messages
    public const string RegistrationSuccessful = "User registered successfully.";
    public const string LoginSuccessful = "Login successful.";
    public const string LogoutSuccessful = "User logged out successfully.";

    // Token messages
    public const string TokenRefreshed = "Token refreshed successfully.";

    // Conversation messages
    public const string ConversationCreated = "Conversation created successfully.";
    public const string ConversationDeleted = "Conversation deleted successfully.";

    // Message messages
    public const string MessageSent = "Message sent successfully.";
    public const string MessageDeleted = "Message deleted successfully.";
    public const string MessageMarkedAsRead = "Message marked as read.";
}