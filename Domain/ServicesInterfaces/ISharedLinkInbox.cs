namespace MyVocaList.Domain.ServicesInterfaces;

/// <summary>Holds a link shared into the app (Android share target, REQ-MSRC-06) until a song form takes it.</summary>
public interface ISharedLinkInbox
{
    /// <summary>Returns the pending shared text and clears it, or null when nothing is pending.</summary>
    string? TakePending();

    /// <summary>Raised when a link arrives while a song form is listening.</summary>
    event EventHandler? LinkReceived;

    /// <summary>Stores the shared text and raises <see cref="LinkReceived"/>.</summary>
    void Receive(string text);

    /// <summary>True while a song form is subscribed to <see cref="LinkReceived"/>.</summary>
    bool HasListener { get; }
}
