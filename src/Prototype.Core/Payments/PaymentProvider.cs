using System.Collections.Concurrent;

namespace Prototype.Core.Payments;

public sealed record CheckoutStart(string ProviderRef, Uri RedirectTo);

public enum CheckoutOutcome
{
    Pending,
    Paid,
    Declined,
    Cancelled
}

/// <summary>
/// The payment provider the college chooses (ParentPay, Stripe, WPM and so on). The journey is the same for all of
/// them: the site hands over the amount and a description, the provider takes the card details on its own page,
/// then sends the payer back and the site checks the outcome with the provider.
/// </summary>
public interface IPaymentProvider
{
    string Name { get; }
    Task<CheckoutStart> StartAsync(Payment payment, Uri returnUrl, Uri cancelUrl, CancellationToken cancellationToken);
    Task<CheckoutOutcome> GetOutcomeAsync(string providerRef, CancellationToken cancellationToken);
}

/// <summary>
/// A stand-in provider for the prototype. Its checkout page is part of this site (<c>/demo-provider/...</c>) and
/// accepts the usual test card numbers; no money moves. Swap in a real provider by registering another
/// <see cref="IPaymentProvider"/>.
/// </summary>
public sealed class DemoPaymentProvider : IPaymentProvider
{
    public const string SuccessCard = "4242424242424242";
    public const string DeclineCard = "4000000000000002";

    private readonly ConcurrentDictionary<string, DemoCheckout> _sessions = new();

    public string Name => "Demo provider";

    public Task<CheckoutStart> StartAsync(Payment payment, Uri returnUrl, Uri cancelUrl, CancellationToken cancellationToken)
    {
        var session = new DemoCheckout($"demo_{Guid.NewGuid():N}", payment.Reference, payment.Amount,
            string.Join(", ", payment.Items.Select(i => i.Description)), payment.PayerEmail, returnUrl, cancelUrl);
        _sessions[session.Id] = session;
        return Task.FromResult(new CheckoutStart(session.Id, new Uri($"/demo-provider/checkout/{session.Id}", UriKind.Relative)));
    }

    public Task<CheckoutOutcome> GetOutcomeAsync(string providerRef, CancellationToken cancellationToken) =>
        Task.FromResult(_sessions.TryGetValue(providerRef, out var session) ? session.Outcome : CheckoutOutcome.Pending);

    public DemoCheckout? Find(string sessionId) => _sessions.GetValueOrDefault(sessionId);

    /// <summary>Takes a test card. Returns null when the card is accepted or declined, or a message when it isn't a test card.</summary>
    public string? Pay(string sessionId, string? cardNumber)
    {
        if (!_sessions.TryGetValue(sessionId, out var session)) return "This payment has expired. Please start again.";

        var digits = new string((cardNumber ?? "").Where(char.IsDigit).ToArray());
        switch (digits)
        {
            case SuccessCard:
                session.Outcome = CheckoutOutcome.Paid;
                return null;
            case DeclineCard:
                session.Outcome = CheckoutOutcome.Declined;
                return null;
            default:
                return "Use the test card 4242 4242 4242 4242. No real payments are taken in this prototype.";
        }
    }

    public void Cancel(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session)) session.Outcome = CheckoutOutcome.Cancelled;
    }
}

public sealed class DemoCheckout(string id, string reference, decimal amount, string description, string? email, Uri returnUrl, Uri cancelUrl)
{
    public string Id { get; } = id;
    public string Reference { get; } = reference;
    public decimal Amount { get; } = amount;
    public string Description { get; } = description;
    public string? Email { get; } = email;
    public Uri ReturnUrl { get; } = returnUrl;
    public Uri CancelUrl { get; } = cancelUrl;
    public CheckoutOutcome Outcome { get; set; } = CheckoutOutcome.Pending;
}
