namespace Prototype.Core.Payments;

public enum PaymentType
{
    /// <summary>A course deposit, paid before enrolment against an application reference.</summary>
    Deposit,

    /// <summary>Materials fees, paid after enrolment against a student ID.</summary>
    Materials,

    /// <summary>Trips and other one-off payments, paid against a student ID.</summary>
    Trip
}

public enum PayerType
{
    Learner,
    ParentCarer
}

public enum PaymentStatus
{
    /// <summary>The learner has been found and the items chosen; payer details still to come.</summary>
    Started,

    /// <summary>Sent to the payment provider.</summary>
    AwaitingPayment,
    Paid,
    Failed
}

public sealed record PaymentItem(string Key, string Description, decimal Amount);

/// <summary>A payment on the website, from the moment the learner is found to the provider confirming it.</summary>
public sealed record Payment
{
    public required Guid Id { get; init; }

    /// <summary>The short reference shown to the payer and on the receipt, e.g. CC-7KQ4M2.</summary>
    public required string Reference { get; init; }

    public required PaymentType Type { get; init; }

    /// <summary>The application reference (deposits) or student ID (everything else).</summary>
    public required string LearnerRef { get; init; }

    public required string LearnerName { get; init; }
    public int? CourseId { get; init; }
    public required IReadOnlyList<PaymentItem> Items { get; init; }
    public PayerType? PayerType { get; init; }
    public string? PayerName { get; init; }
    public string? PayerEmail { get; init; }
    public required PaymentStatus Status { get; init; }
    public string? Provider { get; init; }
    public string? ProviderRef { get; init; }
    public required DateTime CreatedUtc { get; init; }
    public DateTime? PaidUtc { get; init; }

    public decimal Amount => Items.Sum(i => i.Amount);
}

/// <summary>Something a learner owes, read from the college's records.</summary>
public sealed record Charge(string Key, PaymentType Type, string Description, string? Details, decimal Amount, DateOnly? DueDate);

/// <summary>A learner found by their reference and date of birth, with what they can pay for.</summary>
public sealed record LearnerAccount(string LearnerRef, string FirstName, string Surname, int? CourseId, IReadOnlyList<Charge> Charges)
{
    public string FullName => $"{FirstName} {Surname}";
}
