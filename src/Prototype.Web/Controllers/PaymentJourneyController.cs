using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Prototype.Core.Courses;
using Prototype.Core.Payments;
using Prototype.Web.Cms;
using Prototype.Web.Site;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.Controllers;

namespace Prototype.Web.Controllers;

/// <summary>
/// The payment journey, one question per page in the GOV.UK style:
/// find the learner → choose what to pay → who is paying → check and pay → the provider's page → receipt.
/// </summary>
public sealed class PaymentJourneyController(
    ILogger<PaymentJourneyController> logger,
    ICompositeViewEngine viewEngine,
    IPublishedContentQuery contentQuery,
    PaymentService payments,
    CourseCatalogue catalogue,
    TimeProvider time) : UmbracoPageController(logger, viewEngine), IVirtualPageController
{
    public IPublishedContent? FindContent(ActionExecutingContext actionExecutingContext) =>
        contentQuery.FindPage(DocTypes.PaymentsPage.Alias);

    // Step 1: find the learner
    [HttpGet]
    public IActionResult Find(string type) =>
        PaymentTypes.FromSlug(type) is { } t ? Step("Find", t, 1) : NotFound();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Find(string type, PaymentForm form, CancellationToken cancellationToken)
    {
        if (PaymentTypes.FromSlug(type) is not { } t) return NotFound();

        var (account, errors) = await LookUpAsync(t, form, cancellationToken);
        if (account is null) return Step("Find", t, 1, form, errors);

        // Step 2 is shown straight from here; the choice is posted with the reference and date of birth again,
        // so nothing about the learner is trusted from the browser
        return await ChooseViewAsync(t, account, form, [], cancellationToken);
    }

    // Step 2: choose what to pay for
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Choose(string type, PaymentForm form, CancellationToken cancellationToken)
    {
        if (PaymentTypes.FromSlug(type) is not { } t) return NotFound();

        var (account, errors) = await LookUpAsync(t, form, cancellationToken);
        if (account is null) return Step("Find", t, 1, form, errors);

        var started = await payments.StartAsync(t, account, form.Items, cancellationToken);
        if (started.Payment is null) return await ChooseViewAsync(t, account, form, new() { ["items"] = started.Error! }, cancellationToken);

        return Redirect($"/pay/{started.Payment.Id}/details/");
    }

    // Step 3: who is paying
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var payment = await payments.GetAsync(id, cancellationToken);
        if (payment is null) return NotFound();
        if (payment.Status == PaymentStatus.Paid) return Redirect($"/pay/{id}/receipt/");

        var form = new PaymentForm { Payer = payment.PayerType, PayerName = payment.PayerName, PayerEmail = payment.PayerEmail };
        return Step("Details", payment.Type, 3, form, payment: payment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Details(Guid id, PaymentForm form, CancellationToken cancellationToken)
    {
        var payment = await payments.GetAsync(id, cancellationToken);
        if (payment is null) return NotFound();

        var errors = PaymentService.PayerErrors(form.Payer, form.PayerName, form.PayerEmail);
        if (errors.Count > 0) return Step("Details", payment.Type, 3, form, new(errors), payment: payment);

        var updated = await payments.SetPayerAsync(id, form.Payer, form.PayerName, form.PayerEmail, cancellationToken);
        if (updated.Payment is null) return Step("Details", payment.Type, 3, form, new() { ["payer"] = updated.Error! }, payment: payment);

        return Redirect($"/pay/{id}/review/");
    }

    // Step 4: check and pay
    [HttpGet]
    public async Task<IActionResult> Review(Guid id, string? declined, CancellationToken cancellationToken)
    {
        var payment = await payments.GetAsync(id, cancellationToken);
        if (payment is null) return NotFound();
        if (payment.Status == PaymentStatus.Paid) return Redirect($"/pay/{id}/receipt/");
        if (payment.PayerName is null) return Redirect($"/pay/{id}/details/");

        var message = declined is not null
            ? "Your payment wasn't taken. The card was declined, so nothing has been charged. Check the card details or try another card."
            : null;
        return Step("Review", payment.Type, 4, payment: payment, courseTitle: await CourseTitleAsync(payment, cancellationToken), message: message);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(Guid id, CancellationToken cancellationToken)
    {
        var site = new Uri($"{Request.Scheme}://{Request.Host}");
        var (redirectTo, error) = await payments.CheckoutAsync(id, new Uri(site, $"/pay/{id}/return/"), new Uri(site, $"/pay/{id}/return/"), cancellationToken);
        if (redirectTo is null)
        {
            var payment = await payments.GetAsync(id, cancellationToken);
            return payment is null ? NotFound() : Step("Review", payment.Type, 4, payment: payment, message: error);
        }

        return Redirect(redirectTo.IsAbsoluteUri ? redirectTo.ToString() : redirectTo.OriginalString);
    }

    // Back from the provider: check the outcome with the provider, never from the address
    [HttpGet]
    public async Task<IActionResult> Return(Guid id, CancellationToken cancellationToken)
    {
        var payment = await payments.ConfirmAsync(id, cancellationToken);
        return payment?.Status switch
        {
            null => NotFound(),
            PaymentStatus.Paid => Redirect($"/pay/{id}/receipt/"),
            PaymentStatus.Failed => await RetryAsync(payment, cancellationToken),
            _ => Redirect($"/pay/{id}/review/")
        };
    }

    [HttpGet]
    public async Task<IActionResult> Receipt(Guid id, CancellationToken cancellationToken)
    {
        var payment = await payments.GetAsync(id, cancellationToken);
        if (payment is null) return NotFound();
        if (payment.Status != PaymentStatus.Paid) return Redirect($"/pay/{id}/review/");

        return Step("Receipt", payment.Type, 5, payment: payment, courseTitle: await CourseTitleAsync(payment, cancellationToken));
    }

    /// <summary>A declined card: a fresh payment for the same items, so the payer can try another card.</summary>
    private async Task<IActionResult> RetryAsync(Payment failed, CancellationToken cancellationToken)
    {
        var retry = await payments.RetryAsync(failed.Id, cancellationToken);
        return retry is null
            ? Redirect($"/pay/{failed.Type.Slug()}/")
            : Redirect($"/pay/{retry.Id}/review/?declined=1");
    }

    private async Task<(LearnerAccount? Account, Dictionary<string, string> Errors)> LookUpAsync(PaymentType type, PaymentForm form, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(form.Reference)) errors["reference"] = $"Enter the {type.ReferenceLabel().ToLowerInvariant()}";

        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var (dob, dobError) = DateOfBirth.Parse(form.DobDay, form.DobMonth, form.DobYear, today);
        if (dobError is not null) errors["dob"] = dobError;
        if (errors.Count > 0) return (null, errors);

        var (account, error) = await payments.FindLearnerAsync(type, form.Reference, dob!.Value, cancellationToken);
        if (account is null) errors["reference"] = error!;
        return (account, errors);
    }

    private async Task<IActionResult> ChooseViewAsync(PaymentType type, LearnerAccount account, PaymentForm form, Dictionary<string, string> errors, CancellationToken cancellationToken)
    {
        // Everything is ticked to start with; most people pay for all of it
        if (form.Items.Count == 0 && errors.Count == 0) form.Items = account.Charges.Select(c => c.Key).ToList();
        var course = account.CourseId is { } id ? await catalogue.FindAsync(id, cancellationToken) : null;
        return Step("Choose", type, 2, form, errors, account: account, courseTitle: course?.Title);
    }

    private async Task<string?> CourseTitleAsync(Payment payment, CancellationToken cancellationToken) =>
        payment.CourseId is { } id ? (await catalogue.FindAsync(id, cancellationToken))?.Title : null;

    private ViewResult Step(string view, PaymentType type, int step, PaymentForm? form = null, Dictionary<string, string>? errors = null,
        LearnerAccount? account = null, Payment? payment = null, string? courseTitle = null, string? message = null) =>
        View($"Payments/{view}", new PaymentStepViewModel(CurrentPage!, type)
        {
            Step = step,
            Form = form ?? new PaymentForm(),
            Errors = errors ?? [],
            Account = account,
            Payment = payment,
            CourseTitle = courseTitle,
            Message = message
        });
}
