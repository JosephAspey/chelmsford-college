using Microsoft.AspNetCore.Mvc;
using Prototype.Core.Payments;

namespace Prototype.Web.Controllers;

/// <summary>
/// The stand-in payment provider's hosted checkout. In the live site this page belongs to ParentPay, Stripe or
/// whichever provider the college contracts: card details never touch the college's servers.
/// </summary>
public sealed class DemoProviderController(DemoPaymentProvider provider) : Controller
{
    [HttpGet]
    public IActionResult Index(string sessionId) =>
        provider.Find(sessionId) is { } checkout ? View("~/Views/DemoProvider/Index.cshtml", new DemoCheckoutViewModel(checkout, null)) : NotFound();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Pay(string sessionId, string? cardNumber)
    {
        var checkout = provider.Find(sessionId);
        if (checkout is null) return NotFound();

        var error = provider.Pay(sessionId, cardNumber);
        return error is null
            ? Redirect(checkout.ReturnUrl.ToString())
            : View("~/Views/DemoProvider/Index.cshtml", new DemoCheckoutViewModel(checkout, error));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(string sessionId)
    {
        var checkout = provider.Find(sessionId);
        if (checkout is null) return NotFound();

        provider.Cancel(sessionId);
        return Redirect(checkout.CancelUrl.ToString());
    }
}

public sealed record DemoCheckoutViewModel(DemoCheckout Checkout, string? Error);
