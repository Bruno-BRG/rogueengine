namespace RogueEngine.Engine.Core;

public sealed class MessageLog
{
    private readonly List<string> _messages = [];
    private readonly int _maxMessages;

    public MessageLog(int maxMessages = 100)
    {
        if (maxMessages <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMessages));
        }

        _maxMessages = maxMessages;
    }

    public IReadOnlyList<string> Messages => _messages;

    public void Add(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        _messages.Add(message);
        if (_messages.Count > _maxMessages)
        {
            _messages.RemoveAt(0);
        }
    }
}
