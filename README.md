# Chelmsford College website prototype

Zim Digital's working prototype for Chelmsford College's website tender (Find a Tender 102420-2026). It covers the four pages in the college's demo brief: home page with course search, course page, events, and the payment journey. It runs on the stack proposed in the bid: Umbraco 17 LTS on ASP.NET Core (.NET 10) with SQL Server.

## What it shows

| Brief asks for | Where |
|---|---|
| Logo, navigation, prominent course search, campaign area, video and images | `/` (all editable in the CMS) |
| Search results filtered by subject, level and study mode | `/courses/?q=chef` |
| One course template driven by the college's SQL tables | `/courses/{C_ID}/{slug}/`, with a "How this page is built" panel |
| Events list filtered by type, single event with date, time, place and booking | `/events/`, `/events/showcase-open-evening/` (plus "Add to calendar") |
| Deposits, materials fees and trips, paid by students or parents | `/pay/` → find → choose → who's paying → check → provider → receipt |
| A CMS staff edit and publish in | `/umbraco` |

Campus is left out of the filters: the college said it's a legacy field with no data.

## How it fits together

```
src/
  Prototype.AppHost/         Aspire: SQL Server in Docker (persistent volume) + the website
  Prototype.ServiceDefaults/ OpenTelemetry, health checks
  Prototype.Core/            No Umbraco: course data, search, HTML cleaning, payment journey
    CollegeData/             college-data.sql (built from the college's demo course data) and demo-payments.sql
  Prototype.Web/             Umbraco site
    Cms/                     Page types and the first-run content seeder
    Controllers/             Route hijacks (home, courses, events) and virtual pages (course, payments)
    Site/                    View helpers and view models
    Views/                   master.cshtml layout and the page templates
tests/Prototype.Core.Tests/  Unit tests (GIVEN/WHEN/THEN)
tools/                       Course data generator and the demo reset script
```

- **Course data** is read straight from tables with the college's own names (`courses`, `course_sections`, `course_levels`, `course_level_link`, `course_funding`, `course_fund_link`), cached for five minutes. In the live build this is a read-only login on their SQL Server 2022.
- **CMS content** (home page, events, payment wording, subject images) lives in Umbraco. Subject area pages map a `course_sections.S_ID` to an image and intro.
- **Payments** go through `IPaymentProvider`. The prototype uses `DemoPaymentProvider`, a stand-in hosted checkout at `/demo-provider/...`; the college will contract ParentPay, Stripe or similar. The site records payments in `web_payments` and `web_payment_items`.

## Running it

Docker Desktop must be running.

```bash
cd src/Prototype.AppHost
dotnet run --launch-profile http
```

- Aspire dashboard: http://localhost:15290 (login link in the console)
- Website: https://localhost:44390 (or http://localhost:5290)
- CMS: https://localhost:44390/umbraco. The CMS has to be on https. User `joseph@zimdigital.co.uk`, password in the AppHost's user secrets: `dotnet user-secrets list` in `src/Prototype.AppHost` (`Parameters:umbraco-admin-password`).

The first run takes a minute or two: it builds the college tables, installs Umbraco, uploads the college's images and creates the starter content.

### Test details for the payment journey

| Payment | Reference | Date of birth |
|---|---|---|
| Deposit | APP-24817 (Maisie Turner) | 12 4 2010 |
| Deposit | APP-24863 (Grace Bennett, adult) | 18 6 2005 |
| Materials or trips | 2610451 (Ella Richardson) | 21 5 2009 |
| Materials or trips | 2610519 (Ryan Patel) | 9 12 2008 |

Card 4242 4242 4242 4242 succeeds, 4000 0000 0000 0002 is declined. Paid items aren't offered again; reset to start over.

### Starting again

```powershell
./tools/reset-demo-data.ps1
```

Removes this project's SQL container and volume and the uploaded media. Only touches the `chelmsford-prototype-sql` volume.

### Regenerating the course tables

`tools/course-data/*.json` are the sheets of Appendix 2 exported from Excel. `node tools/generate-college-data.js` rebuilds `college-data.sql`.

## Commands

```bash
dotnet build
dotnet test
```

Stop the AppHost before building: a running site locks the DLLs.
