using System.Security.Cryptography;

namespace Prototype.Core.Payments;

public sealed record PaymentResult(Payment? Payment, string? Error)
{
    public static PaymentResult Fail(string error) => new(null, error);
    public static PaymentResult Ok(Payment payment) => new(payment, null);
}

/// <summary>
/// The steps of a payment: find the learner, choose what to pay for, say who is paying, then hand over to the
/// payment provider and record the outcome.
/// </summary>
public sealed class PaymentService(ILearnerRecords learners, IPaymentStore store, IPaymentProvider provider, TimeProvider time)
{
    // No 0/O, 1/I/L: references are read out over the phone
    private const string ReferenceAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public const string NotFound = "We couldn't find a match for those details. Check the reference and date of birth, " +
        "or contact the student finance team.";

    public async Task<(LearnerAccount? Account, string? Error)> FindLearnerAsync(PaymentType type, string? reference, DateOnly dateOfBirth, CancellationToken cancellationToken)
    {
        var normalised = NormaliseReference(type, reference);
        if (normalised is null) return (null, $"Enter your {type.ReferenceLabel().ToLowerInvariant()}");

        // One message whichever part is wrong, so the form can't be used to check whether a reference exists
        var account = await learners.FindAsync(type, normalised, dateOfBirth, cancellationToken);
        return account is null ? (null, NotFound) : (account, null);
    }

    public async Task<PaymentResult> StartAsync(PaymentType type, LearnerAccount account, IReadOnlyCollection<string> chosenKeys, CancellationToken cancellationToken)
    {
        var chosen = account.Charges.Where(c => chosenKeys.Contains(c.Key)).ToList();
        if (chosen.Count == 0) return PaymentResult.Fail("Choose at least one thing to pay for");

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Reference = NewReference(),
            Type = type,
            LearnerRef = account.LearnerRef,
            LearnerName = account.FullName,
            CourseId = account.CourseId,
            Items = chosen.Select(c => new PaymentItem(c.Key, c.Description, c.Amount)).ToList(),
            Status = PaymentStatus.Started,
            CreatedUtc = time.GetUtcNow().UtcDateTime
        };
        await store.AddAsync(payment, cancellationToken);
        return PaymentResult.Ok(payment);
    }

    public async Task<PaymentResult> SetPayerAsync(Guid id, PayerType? payerType, string? name, string? email, CancellationToken cancellationToken)
    {
        var payment = await store.GetAsync(id, cancellationToken);
        if (payment is null || payment.Status != PaymentStatus.Started) return PaymentResult.Fail("This payment has already been made or has expired. Please start again.");

        var errors = PayerErrors(payerType, name, email);
        if (errors.Count > 0) return PaymentResult.Fail(errors.First().Value);

        var updated = payment with { PayerType = payerType, PayerName = name!.Trim(), PayerEmail = email!.Trim() };
        await store.UpdateAsync(updated, cancellationToken);
        return PaymentResult.Ok(updated);
    }

    /// <summary>Hands the payment to the provider. Returns where to send the payer.</summary>
    public async Task<(Uri? RedirectTo, string? Error)> CheckoutAsync(Guid id, Uri returnUrl, Uri cancelUrl, CancellationToken cancellationToken)
    {
        var payment = await store.GetAsync(id, cancellationToken);
        if (payment is null || payment.PayerName is null || payment.Status is PaymentStatus.Paid)
            return (null, "This payment has already been made or has expired. Please start again.");

        var start = await provider.StartAsync(payment, returnUrl, cancelUrl, cancellationToken);
        await store.UpdateAsync(payment with { Status = PaymentStatus.AwaitingPayment, Provider = provider.Name, ProviderRef = start.ProviderRef }, cancellationToken);
        return (start.RedirectTo, null);
    }

    /// <summary>
    /// Asks the provider what happened, rather than trusting the return address, and records it. Safe to call
    /// more than once.
    /// </summary>
    public async Task<Payment?> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var payment = await store.GetAsync(id, cancellationToken);
        if (payment?.ProviderRef is null || payment.Status != PaymentStatus.AwaitingPayment) return payment;

        var outcome = await provider.GetOutcomeAsync(payment.ProviderRef, cancellationToken);
        var updated = outcome switch
        {
            CheckoutOutcome.Paid => payment with { Status = PaymentStatus.Paid, PaidUtc = time.GetUtcNow().UtcDateTime },
            CheckoutOutcome.Declined => payment with { Status = PaymentStatus.Failed },
            // Cancelled: back to the review step so they can try again
            CheckoutOutcome.Cancelled => payment with { Status = PaymentStatus.Started },
            _ => payment
        };
        if (updated != payment) await store.UpdateAsync(updated, cancellationToken);
        return updated;
    }

    /// <summary>
    /// After a declined card: a new payment for the same items and payer, ready to check and pay again. The failed
    /// one stays on record. Null if the payment can't be retried.
    /// </summary>
    public async Task<Payment?> RetryAsync(Guid failedId, CancellationToken cancellationToken)
    {
        var failed = await store.GetAsync(failedId, cancellationToken);
        if (failed is not { Status: PaymentStatus.Failed }) return null;

        var retry = failed with
        {
            Id = Guid.NewGuid(),
            Reference = NewReference(),
            Status = PaymentStatus.Started,
            Provider = null,
            ProviderRef = null,
            CreatedUtc = time.GetUtcNow().UtcDateTime,
            PaidUtc = null
        };
        await store.AddAsync(retry, cancellationToken);
        return retry;
    }

    public Task<Payment?> GetAsync(Guid id, CancellationToken cancellationToken) => store.GetAsync(id, cancellationToken);

    /// <summary>Field name → message, in the order the fields appear on the form.</summary>
    public static IReadOnlyDictionary<string, string> PayerErrors(PayerType? payerType, string? name, string? email)
    {
        var errors = new Dictionary<string, string>();
        if (payerType is null) errors["payer"] = "Tell us who is paying";
        if (string.IsNullOrWhiteSpace(name)) errors["name"] = "Enter the name of the person paying";
        if (string.IsNullOrWhiteSpace(email)) errors["email"] = "Enter an email address for the receipt";
        else if (!LooksLikeEmail(email.Trim())) errors["email"] = "Enter an email address in the correct format, like name@example.com";
        return errors;
    }

    internal static string? NormaliseReference(PaymentType type, string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        var value = new string(reference.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
        // People often leave off the prefix
        if (type == PaymentType.Deposit && value.All(char.IsDigit)) value = "APP-" + value;
        return value;
    }

    internal static bool LooksLikeEmail(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 && at == email.LastIndexOf('@') && email.IndexOf('.', at) > at + 1 && !email.EndsWith('.') && !email.Contains(' ');
    }

    private static string NewReference() =>
        "CC-" + RandomNumberGenerator.GetString(ReferenceAlphabet, 6);
}
