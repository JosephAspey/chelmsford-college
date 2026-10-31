using Moq;
using Prototype.Core.Payments;

namespace Prototype.Core.Tests.Payments;

public class PaymentServiceTests
{
    private static readonly DateOnly Dob = new(2009, 5, 21);
    private readonly Mock<ILearnerRecords> _learners = new();
    private readonly InMemoryPaymentStore _store = new();
    private readonly Mock<IPaymentProvider> _provider = new();
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _provider.SetupGet(p => p.Name).Returns("Test provider");
        _service = new PaymentService(_learners.Object, _store, _provider.Object, TimeProvider.System);
    }

    [Fact]
    public async Task FindLearnerAsync_GivenADepositReferenceWithoutItsPrefix_WhenFound_ThenThePrefixIsAdded()
    {
        // Given
        var account = GivenAccount("APP-24817");
        _learners.Setup(l => l.FindAsync(PaymentType.Deposit, "APP-24817", Dob, It.IsAny<CancellationToken>())).ReturnsAsync(account);

        // When
        var (found, error) = await _service.FindLearnerAsync(PaymentType.Deposit, " 24817 ", Dob, CancellationToken.None);

        // Then
        error.Should().BeNull();
        found.Should().Be(account);
    }

    [Fact]
    public async Task FindLearnerAsync_GivenDetailsThatDoNotMatch_WhenSearched_ThenTheMessageDoesNotSayWhichPartWasWrong()
    {
        // When
        var (found, error) = await _service.FindLearnerAsync(PaymentType.Materials, "2610451", Dob, CancellationToken.None);

        // Then
        found.Should().BeNull();
        error.Should().Be(PaymentService.NotFound);
    }

    [Fact]
    public async Task StartAsync_GivenAKeyThatIsNotOneOfTheLearnersCharges_WhenStarted_ThenItIsIgnored()
    {
        // Given
        var account = GivenAccount("2610451", new Charge("1001", PaymentType.Materials, "Kit", null, 150, null));

        // When
        var result = await _service.StartAsync(PaymentType.Materials, account, ["1001", "9999"], CancellationToken.None);

        // Then
        result.Payment!.Items.Should().ContainSingle().Which.Key.Should().Be("1001");
        result.Payment.Amount.Should().Be(150);
        result.Payment.Reference.Should().MatchRegex("^CC-[A-HJ-NP-Z2-9]{6}$");
    }

    [Fact]
    public async Task StartAsync_GivenNothingChosen_WhenStarted_ThenItIsRefused()
    {
        // Given
        var account = GivenAccount("2610451", new Charge("1001", PaymentType.Materials, "Kit", null, 150, null));

        // When
        var result = await _service.StartAsync(PaymentType.Materials, account, [], CancellationToken.None);

        // Then
        result.Payment.Should().BeNull();
        result.Error.Should().Be("Choose at least one thing to pay for");
    }

    [Theory]
    [InlineData(null, "Sam Parent", "sam@example.com", "payer")]
    [InlineData(PayerType.ParentCarer, " ", "sam@example.com", "name")]
    [InlineData(PayerType.ParentCarer, "Sam Parent", "sam@example", "email")]
    [InlineData(PayerType.Learner, "Ella", "", "email")]
    public void PayerErrors_GivenAMissingOrBadField_WhenChecked_ThenThatFieldHasAnError(PayerType? payer, string name, string email, string field)
    {
        // When
        var errors = PaymentService.PayerErrors(payer, name, email);

        // Then
        errors.Keys.Should().Equal(field);
    }

    [Fact]
    public async Task ConfirmAsync_GivenTheProviderSaysPaid_WhenConfirmed_ThenThePaymentIsPaid()
    {
        // Given
        var payment = await GivenPaymentAtProviderAsync();
        _provider.Setup(p => p.GetOutcomeAsync("prov_1", It.IsAny<CancellationToken>())).ReturnsAsync(CheckoutOutcome.Paid);

        // When
        var confirmed = await _service.ConfirmAsync(payment.Id, CancellationToken.None);

        // Then
        confirmed!.Status.Should().Be(PaymentStatus.Paid);
        confirmed.PaidUtc.Should().NotBeNull();
        (await _store.GetAsync(payment.Id, CancellationToken.None))!.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public async Task ConfirmAsync_GivenThePayerCancelledAtTheProvider_WhenConfirmed_ThenThePaymentCanBeTriedAgain()
    {
        // Given
        var payment = await GivenPaymentAtProviderAsync();
        _provider.Setup(p => p.GetOutcomeAsync("prov_1", It.IsAny<CancellationToken>())).ReturnsAsync(CheckoutOutcome.Cancelled);

        // When
        var confirmed = await _service.ConfirmAsync(payment.Id, CancellationToken.None);

        // Then
        confirmed!.Status.Should().Be(PaymentStatus.Started);
    }

    [Fact]
    public async Task CheckoutAsync_GivenAPaymentWithAPayer_WhenCheckedOut_ThenItIsAwaitingPaymentWithTheProvidersReference()
    {
        // Given
        var account = GivenAccount("2610451", new Charge("1002", PaymentType.Trip, "London trip", null, 45, null));
        var started = (await _service.StartAsync(PaymentType.Trip, account, ["1002"], CancellationToken.None)).Payment!;
        await _service.SetPayerAsync(started.Id, PayerType.ParentCarer, "Sam Parent", "sam@example.com", CancellationToken.None);
        _provider.Setup(p => p.StartAsync(It.IsAny<Payment>(), It.IsAny<Uri>(), It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutStart("prov_9", new Uri("https://pay.example.com/9")));

        // When
        var (redirect, error) = await _service.CheckoutAsync(started.Id, new Uri("https://site/return"), new Uri("https://site/cancel"), CancellationToken.None);

        // Then
        error.Should().BeNull();
        redirect.Should().Be(new Uri("https://pay.example.com/9"));
        var stored = await _store.GetAsync(started.Id, CancellationToken.None);
        stored!.Status.Should().Be(PaymentStatus.AwaitingPayment);
        stored.ProviderRef.Should().Be("prov_9");
    }

    [Fact]
    public async Task RetryAsync_GivenADeclinedPayment_WhenRetried_ThenANewPaymentHasTheSameItemsAndPayerAndANewReference()
    {
        // Given
        var declined = await GivenPaymentAtProviderAsync();
        await _store.UpdateAsync(declined with { Status = PaymentStatus.Failed }, CancellationToken.None);

        // When
        var retry = await _service.RetryAsync(declined.Id, CancellationToken.None);

        // Then
        retry!.Id.Should().NotBe(declined.Id);
        retry.Reference.Should().NotBe(declined.Reference);
        retry.Status.Should().Be(PaymentStatus.Started);
        retry.ProviderRef.Should().BeNull();
        retry.Items.Should().BeEquivalentTo(declined.Items);
        retry.PayerEmail.Should().Be(declined.PayerEmail);
        (await _store.GetAsync(declined.Id, CancellationToken.None))!.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public async Task RetryAsync_GivenAPaymentThatWasNotDeclined_WhenRetried_ThenNothingIsCreated()
    {
        // Given
        var awaiting = await GivenPaymentAtProviderAsync();

        // When
        var retry = await _service.RetryAsync(awaiting.Id, CancellationToken.None);

        // Then
        retry.Should().BeNull();
    }

    private async Task<Payment> GivenPaymentAtProviderAsync()
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Reference = "CC-ABC234",
            Type = PaymentType.Deposit,
            LearnerRef = "APP-24817",
            LearnerName = "Maisie Turner",
            Items = [new PaymentItem("APP-24817", "Course deposit", 50)],
            PayerType = PayerType.Learner,
            PayerName = "Maisie Turner",
            PayerEmail = "maisie@example.com",
            Status = PaymentStatus.AwaitingPayment,
            ProviderRef = "prov_1",
            CreatedUtc = DateTime.UtcNow
        };
        await _store.AddAsync(payment, CancellationToken.None);
        return payment;
    }

    private static LearnerAccount GivenAccount(string reference, params Charge[] charges) =>
        new(reference, "Ella", "Richardson", 374, charges);

    private sealed class InMemoryPaymentStore : IPaymentStore
    {
        private readonly Dictionary<Guid, Payment> _payments = [];

        public Task AddAsync(Payment payment, CancellationToken cancellationToken) { _payments[payment.Id] = payment; return Task.CompletedTask; }
        public Task<Payment?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_payments.GetValueOrDefault(id));
        public Task<Payment?> FindByProviderRefAsync(string providerRef, CancellationToken cancellationToken) =>
            Task.FromResult(_payments.Values.FirstOrDefault(p => p.ProviderRef == providerRef));
        public Task UpdateAsync(Payment payment, CancellationToken cancellationToken) { _payments[payment.Id] = payment; return Task.CompletedTask; }
    }
}
