using Dapper;
using Prototype.Core.CollegeData;

namespace Prototype.Core.Payments;

public interface ILearnerRecords
{
    /// <summary>Finds the learner only when both the reference and the date of birth match.</summary>
    Task<LearnerAccount?> FindAsync(PaymentType type, string reference, DateOnly dateOfBirth, CancellationToken cancellationToken);
}

public interface IPaymentStore
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);
    Task<Payment?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Payment?> FindByProviderRefAsync(string providerRef, CancellationToken cancellationToken);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken);
}

/// <summary>
/// Reads applications and student charges. In the prototype these come from the demo_ tables; in the live build
/// they come from UNIT-e and the college's finance records, by whichever connection the college's IT team agrees.
/// </summary>
public sealed class SqlLearnerRecords(CollegeDatabase database) : ILearnerRecords
{
    public async Task<LearnerAccount?> FindAsync(PaymentType type, string reference, DateOnly dateOfBirth, CancellationToken cancellationToken)
    {
        var dob = dateOfBirth.ToDateTime(TimeOnly.MinValue);
        await using var connection = await database.OpenAsync(cancellationToken);

        if (type == PaymentType.Deposit)
        {
            var app = await connection.QuerySingleOrDefaultAsync<ApplicationRow>(new CommandDefinition("""
                SELECT a.APP_REF, a.FIRST_NAME, a.SURNAME, a.COURSE_ID, a.DEPOSIT, a.DEPOSIT_PAID, c.C_TITLE
                FROM dbo.demo_applications a
                LEFT JOIN dbo.courses c ON c.C_ID = a.COURSE_ID
                WHERE a.APP_REF = @reference AND a.DOB = @dob
                """, new { reference, dob }, cancellationToken: cancellationToken));
            if (app is null) return null;

            Charge[] charges = app.DEPOSIT_PAID
                ? []
                : [new Charge(app.APP_REF, PaymentType.Deposit, $"Course deposit: {app.C_TITLE?.Trim() ?? "your course"}", "Secures your place. It comes off your course fees when you enrol.", app.DEPOSIT, null)];
            return new LearnerAccount(app.APP_REF, app.FIRST_NAME, app.SURNAME, app.COURSE_ID, charges);
        }

        var student = await connection.QuerySingleOrDefaultAsync<StudentRow>(new CommandDefinition("""
            SELECT STUDENT_ID, FIRST_NAME, SURNAME, COURSE_ID FROM dbo.demo_students
            WHERE STUDENT_ID = @reference AND DOB = @dob
            """, new { reference, dob }, cancellationToken: cancellationToken));
        if (student is null) return null;

        var rows = await connection.QueryAsync<ChargeRow>(new CommandDefinition("""
            SELECT CHARGE_ID, DESCRIPTION, DETAILS, AMOUNT, DUE_DATE FROM dbo.demo_student_charges
            WHERE STUDENT_ID = @reference AND CHARGE_TYPE = @chargeType AND PAID = 0
            ORDER BY DUE_DATE, CHARGE_ID
            """, new { reference, chargeType = type == PaymentType.Materials ? "Materials" : "Trip" }, cancellationToken: cancellationToken));

        return new LearnerAccount(student.STUDENT_ID, student.FIRST_NAME, student.SURNAME, student.COURSE_ID,
            rows.Select(r => new Charge(r.CHARGE_ID.ToString(), type, r.DESCRIPTION, r.DETAILS, r.AMOUNT,
                r.DUE_DATE is { } due ? DateOnly.FromDateTime(due) : null)).ToList());
    }

    private sealed class ApplicationRow
    {
        public string APP_REF { get; init; } = "";
        public string FIRST_NAME { get; init; } = "";
        public string SURNAME { get; init; } = "";
        public int COURSE_ID { get; init; }
        public decimal DEPOSIT { get; init; }
        public bool DEPOSIT_PAID { get; init; }
        public string? C_TITLE { get; init; }
    }
    private sealed class StudentRow
    {
        public string STUDENT_ID { get; init; } = "";
        public string FIRST_NAME { get; init; } = "";
        public string SURNAME { get; init; } = "";
        public int COURSE_ID { get; init; }
    }
    private sealed class ChargeRow
    {
        public int CHARGE_ID { get; init; }
        public string DESCRIPTION { get; init; } = "";
        public string? DETAILS { get; init; }
        public decimal AMOUNT { get; init; }
        public DateTime? DUE_DATE { get; init; }
    }
}

/// <summary>The website's own payment records, in tables the college's finance team can query directly.</summary>
public sealed class SqlPaymentStore(CollegeDatabase database) : IPaymentStore
{
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO dbo.web_payments (PAYMENT_ID, REFERENCE, PAYMENT_TYPE, LEARNER_REF, LEARNER_NAME, COURSE_ID, AMOUNT,
                PAYER_TYPE, PAYER_NAME, PAYER_EMAIL, STATUS, PROVIDER, PROVIDER_REF, CREATED_UTC, PAID_UTC)
            VALUES (@Id, @Reference, @Type, @LearnerRef, @LearnerName, @CourseId, @Amount,
                @PayerType, @PayerName, @PayerEmail, @Status, @Provider, @ProviderRef, @CreatedUtc, @PaidUtc)
            """, Parameters(payment), transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO dbo.web_payment_items (PAYMENT_ID, ITEM_KEY, DESCRIPTION, AMOUNT) VALUES (@PaymentId, @Key, @Description, @Amount)
            """, payment.Items.Select(i => new { PaymentId = payment.Id, i.Key, i.Description, i.Amount }), transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<Payment?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        LoadAsync("PAYMENT_ID = @value", id, cancellationToken);

    public Task<Payment?> FindByProviderRefAsync(string providerRef, CancellationToken cancellationToken) =>
        LoadAsync("PROVIDER_REF = @value", providerRef, cancellationToken);

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE dbo.web_payments SET AMOUNT = @Amount, PAYER_TYPE = @PayerType, PAYER_NAME = @PayerName, PAYER_EMAIL = @PayerEmail,
                STATUS = @Status, PROVIDER = @Provider, PROVIDER_REF = @ProviderRef, PAID_UTC = @PaidUtc
            WHERE PAYMENT_ID = @Id
            """, Parameters(payment), transaction, cancellationToken: cancellationToken));

        // Mark what was paid for in the learner's records, so it isn't offered again
        if (payment.Status == PaymentStatus.Paid)
        {
            var sql = payment.Type == PaymentType.Deposit
                ? "UPDATE dbo.demo_applications SET DEPOSIT_PAID = 1 WHERE APP_REF = @LearnerRef"
                : "UPDATE dbo.demo_student_charges SET PAID = 1 WHERE STUDENT_ID = @LearnerRef AND CAST(CHARGE_ID AS nvarchar(50)) IN @Keys";
            await connection.ExecuteAsync(new CommandDefinition(sql,
                new { payment.LearnerRef, Keys = payment.Items.Select(i => i.Key).ToArray() }, transaction, cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<Payment?> LoadAsync(string where, object value, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PaymentRow>(new CommandDefinition(
            $"SELECT * FROM dbo.web_payments WHERE {where}", new { value }, cancellationToken: cancellationToken));
        if (row is null) return null;

        var items = await connection.QueryAsync<ItemRow>(new CommandDefinition(
            "SELECT ITEM_KEY, DESCRIPTION, AMOUNT FROM dbo.web_payment_items WHERE PAYMENT_ID = @id ORDER BY ITEM_KEY",
            new { id = row.PAYMENT_ID }, cancellationToken: cancellationToken));

        return new Payment
        {
            Id = row.PAYMENT_ID,
            Reference = row.REFERENCE,
            Type = Enum.Parse<PaymentType>(row.PAYMENT_TYPE),
            LearnerRef = row.LEARNER_REF,
            LearnerName = row.LEARNER_NAME,
            CourseId = row.COURSE_ID,
            Items = items.Select(i => new PaymentItem(i.ITEM_KEY, i.DESCRIPTION, i.AMOUNT)).ToList(),
            PayerType = row.PAYER_TYPE is null ? null : Enum.Parse<PayerType>(row.PAYER_TYPE),
            PayerName = row.PAYER_NAME,
            PayerEmail = row.PAYER_EMAIL,
            Status = Enum.Parse<PaymentStatus>(row.STATUS),
            Provider = row.PROVIDER,
            ProviderRef = row.PROVIDER_REF,
            CreatedUtc = row.CREATED_UTC,
            PaidUtc = row.PAID_UTC
        };
    }

    private static object Parameters(Payment p) => new
    {
        p.Id, p.Reference, Type = p.Type.ToString(), p.LearnerRef, p.LearnerName, p.CourseId, p.Amount,
        PayerType = p.PayerType?.ToString(), p.PayerName, p.PayerEmail, Status = p.Status.ToString(),
        p.Provider, p.ProviderRef, p.CreatedUtc, p.PaidUtc
    };

    private sealed class PaymentRow
    {
        public Guid PAYMENT_ID { get; init; }
        public string REFERENCE { get; init; } = "";
        public string PAYMENT_TYPE { get; init; } = "";
        public string LEARNER_REF { get; init; } = "";
        public string LEARNER_NAME { get; init; } = "";
        public int? COURSE_ID { get; init; }
        public decimal AMOUNT { get; init; }
        public string? PAYER_TYPE { get; init; }
        public string? PAYER_NAME { get; init; }
        public string? PAYER_EMAIL { get; init; }
        public string STATUS { get; init; } = "";
        public string? PROVIDER { get; init; }
        public string? PROVIDER_REF { get; init; }
        public DateTime CREATED_UTC { get; init; }
        public DateTime? PAID_UTC { get; init; }
    }

    private sealed class ItemRow
    {
        public string ITEM_KEY { get; init; } = "";
        public string DESCRIPTION { get; init; } = "";
        public decimal AMOUNT { get; init; }
    }
}
