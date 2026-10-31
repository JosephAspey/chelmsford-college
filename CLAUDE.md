# Chelmsford College website prototype

Demo for Zim Digital's Chelmsford College website tender. See `README.md` for what it shows, how to run it and the test data. Bid documents and the college's brief are in `D:\projects\Chelsmford College tender` (folder name misspelt on disk).

## Conventions

- .NET 10, Umbraco 17 LTS, Aspire 13. Package versions only in `Directory.Packages.props`.
- `Prototype.Core` has no Umbraco dependency and holds all logic worth testing. Every change there needs tests in `tests/Prototype.Core.Tests`: GIVEN/WHEN/THEN with `// Given`, `// When`, `// Then` comments, named `Method_GivenX_WhenY_ThenZ`, FluentAssertions 7.x and Moq.
- The college's tables keep their own column names (`C_TITLE`, `S_ID`...). Demo-only tables are prefixed `demo_`; tables the site writes are prefixed `web_`.
- CMS page types are created in code (`Cms/ContentSeeder.cs`) on first run only. Changing a page type means resetting the demo data (`tools/reset-demo-data.ps1`) or editing it in the backoffice.
- Page templates name their layout as `master.cshtml`, so Umbraco can register them; nested views use `~/Views/master.cshtml`.
- Don't name a Razor variable `page`: `@page.` is the Razor Pages directive.
- Mobile first CSS in `wwwroot/css/site.css`, no build step. Brand blue `#007AC5` and green `#7BC242`; green fails contrast for text on white, so use `--green-dark` for text.
- WCAG 2.2 AA: check pages with axe after changes. Every page passed at the end of 31 October 2026.
- UK English, dates as "4 November 2026", 12-hour times as the college writes them ("6pm to 8:30pm"), money in GBP.
- Stop the AppHost after checking a change: it locks the DLLs.
