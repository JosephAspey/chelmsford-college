-- Demo data for the payment journeys. These are NOT the college's tables: they stand in for the applications
-- (UNIT-e) and student charges the live site would read, using made-up people. web_payments and
-- web_payment_items are the tables the website itself writes to.

CREATE TABLE dbo.demo_applications (
    APP_REF nvarchar(20) NOT NULL PRIMARY KEY,
    FIRST_NAME nvarchar(100) NOT NULL,
    SURNAME nvarchar(100) NOT NULL,
    DOB date NOT NULL,
    COURSE_ID int NOT NULL,
    DEPOSIT decimal(10, 2) NOT NULL,
    DEPOSIT_PAID bit NOT NULL DEFAULT 0
);
GO
INSERT INTO dbo.demo_applications (APP_REF, FIRST_NAME, SURNAME, DOB, COURSE_ID, DEPOSIT) VALUES
(N'APP-24817', N'Maisie', N'Turner', '2010-04-12', 374, 50.00),
(N'APP-24822', N'Leon', N'Okafor', '2010-09-03', 483, 50.00),
(N'APP-24835', N'Priya', N'Shah', '2010-01-27', 525, 50.00),
(N'APP-24851', N'Jack', N'Hughes', '2009-11-30', 540, 50.00),
(N'APP-24863', N'Grace', N'Bennett', '2005-06-18', 527, 100.00);
GO

CREATE TABLE dbo.demo_students (
    STUDENT_ID nvarchar(20) NOT NULL PRIMARY KEY,
    FIRST_NAME nvarchar(100) NOT NULL,
    SURNAME nvarchar(100) NOT NULL,
    DOB date NOT NULL,
    COURSE_ID int NOT NULL
);
GO
INSERT INTO dbo.demo_students (STUDENT_ID, FIRST_NAME, SURNAME, DOB, COURSE_ID) VALUES
(N'2610451', N'Ella', N'Richardson', '2009-05-21', 374),
(N'2610477', N'Kai', N'Mensah', '2009-08-14', 483),
(N'2610502', N'Sophie', N'Walsh', '2009-02-02', 513),
(N'2610519', N'Ryan', N'Patel', '2008-12-09', 595);
GO

CREATE TABLE dbo.demo_student_charges (
    CHARGE_ID int NOT NULL PRIMARY KEY,
    STUDENT_ID nvarchar(20) NOT NULL,
    CHARGE_TYPE nvarchar(20) NOT NULL, -- Materials or Trip
    DESCRIPTION nvarchar(255) NOT NULL,
    DETAILS nvarchar(500) NULL,
    AMOUNT decimal(10, 2) NOT NULL,
    DUE_DATE date NULL,
    PAID bit NOT NULL DEFAULT 0
);
GO
INSERT INTO dbo.demo_student_charges (CHARGE_ID, STUDENT_ID, CHARGE_TYPE, DESCRIPTION, DETAILS, AMOUNT, DUE_DATE) VALUES
(1001, N'2610451', N'Materials', N'Fashion and textiles kit', N'Fabric, sketchbooks and a sewing kit for the year', 150.00, '2026-10-30'),
(1002, N'2610451', N'Trip', N'London Fashion Week trip', N'Coach travel and entry, Saturday 20 February 2027. Leaves Moulsham Street at 07:30', 45.00, '2027-01-29'),
(1003, N'2610451', N'Trip', N'V&A Museum study visit', N'Train travel and a guided tour, Wednesday 17 March 2027', 25.00, '2027-02-26'),
(1004, N'2610477', N'Materials', N'Chef''s whites and knife kit', N'Two sets of whites, safety shoes and a knife roll', 200.00, '2026-10-30'),
(1005, N'2610477', N'Trip', N'Billingsgate Market visit', N'Early-morning visit to London''s fish market, Thursday 4 March 2027', 30.00, '2027-02-12'),
(1006, N'2610502', N'Materials', N'Hairdressing kit', N'Scissors, combs, brushes and a training head', 120.00, '2026-10-30'),
(1007, N'2610519', N'Materials', N'Electrical tool kit', N'Hand tools, a multimeter and PPE', 95.00, '2026-10-30'),
(1008, N'2610519', N'Trip', N'Electrical skills competition', N'Regional SkillELECTRIC heat, Tuesday 9 February 2027', 15.00, '2027-01-22');
GO

CREATE TABLE dbo.web_payments (
    PAYMENT_ID uniqueidentifier NOT NULL PRIMARY KEY,
    REFERENCE nvarchar(20) NOT NULL UNIQUE,
    PAYMENT_TYPE nvarchar(20) NOT NULL,       -- Deposit, Materials or Trip
    LEARNER_REF nvarchar(20) NOT NULL,        -- application reference or student ID
    LEARNER_NAME nvarchar(200) NOT NULL,
    COURSE_ID int NULL,
    AMOUNT decimal(10, 2) NOT NULL,
    PAYER_TYPE nvarchar(20) NULL,             -- Learner or ParentCarer
    PAYER_NAME nvarchar(200) NULL,
    PAYER_EMAIL nvarchar(320) NULL,
    STATUS nvarchar(20) NOT NULL,             -- Started, AwaitingPayment, Paid, Failed
    PROVIDER nvarchar(50) NULL,
    PROVIDER_REF nvarchar(100) NULL,
    CREATED_UTC datetime2 NOT NULL,
    PAID_UTC datetime2 NULL
);
GO

CREATE TABLE dbo.web_payment_items (
    PAYMENT_ID uniqueidentifier NOT NULL REFERENCES dbo.web_payments (PAYMENT_ID),
    ITEM_KEY nvarchar(50) NOT NULL,           -- APP_REF for a deposit, CHARGE_ID for a charge
    DESCRIPTION nvarchar(255) NOT NULL,
    AMOUNT decimal(10, 2) NOT NULL,
    PRIMARY KEY (PAYMENT_ID, ITEM_KEY)
);
GO
