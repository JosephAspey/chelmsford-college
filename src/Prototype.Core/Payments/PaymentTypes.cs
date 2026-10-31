namespace Prototype.Core.Payments;

/// <summary>How each payment type is described to the person paying.</summary>
public static class PaymentTypes
{
    public static string Slug(this PaymentType type) => type switch
    {
        PaymentType.Deposit => "deposit",
        PaymentType.Materials => "materials",
        _ => "trips"
    };

    public static PaymentType? FromSlug(string? slug) => slug?.ToLowerInvariant() switch
    {
        "deposit" => PaymentType.Deposit,
        "materials" => PaymentType.Materials,
        "trips" => PaymentType.Trip,
        _ => null
    };

    public static string Title(this PaymentType type) => type switch
    {
        PaymentType.Deposit => "Pay a course deposit",
        PaymentType.Materials => "Pay materials fees",
        _ => "Pay for a trip or activity"
    };

    public static string ShortName(this PaymentType type) => type switch
    {
        PaymentType.Deposit => "Course deposit",
        PaymentType.Materials => "Materials fees",
        _ => "Trips and activities"
    };

    public static string Summary(this PaymentType type) => type switch
    {
        PaymentType.Deposit => "Secure your place before you enrol. You'll need your application reference.",
        PaymentType.Materials => "Kit, uniforms and equipment for your course. You'll need your student ID.",
        _ => "Trips, visits and one-off activities. You'll need your student ID."
    };

    /// <summary>The reference the learner (or their parent or carer) uses to find the account.</summary>
    public static string ReferenceLabel(this PaymentType type) =>
        type == PaymentType.Deposit ? "Application reference" : "Student ID";

    public static string ReferenceHint(this PaymentType type) =>
        type == PaymentType.Deposit
            ? "Starts with APP-, for example APP-24817. It's in the email we sent when you applied."
            : "The 7-digit number on your student ID card, for example 2610451.";

    public static string PayerLabel(this PayerType payer) =>
        payer == PayerType.Learner ? "I'm the student" : "I'm a parent or carer";
}
