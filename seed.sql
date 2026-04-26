USE LegalCaseDb;
GO

-- Tables
CREATE TABLE ApplicationUsers (
    Id NVARCHAR(450) PRIMARY KEY,
    FullName NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(MAX) NOT NULL,
    Email NVARCHAR(MAX) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    PhoneNumber NVARCHAR(MAX) NULL
);
GO

CREATE TABLE Notifications (
    Id INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(MAX) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    SentAt DATETIME2 NOT NULL,
    IsRead BIT NOT NULL DEFAULT 0
);
GO

CREATE TABLE Lawyers (
    Id INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES ApplicationUsers(Id),
    FullName NVARCHAR(MAX) NOT NULL,
    YearsOfExperience INT NOT NULL DEFAULT 0,
    Specialization NVARCHAR(MAX) NOT NULL,
    Bio NVARCHAR(MAX) NOT NULL,
    Avatar NVARCHAR(MAX) NOT NULL,
    Rating FLOAT NOT NULL DEFAULT 0,
    CasesWon INT NOT NULL DEFAULT 0,
    MaxClients INT NOT NULL DEFAULT 2
);
GO

CREATE TABLE Clients (
    Id INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL FOREIGN KEY REFERENCES ApplicationUsers(Id),
    FullName NVARCHAR(MAX) NOT NULL,
    Phone NVARCHAR(MAX) NULL,
    Address NVARCHAR(MAX) NULL,
    Avatar NVARCHAR(MAX) NOT NULL
);
GO

CREATE TABLE LawyerRequests (
    Id INT IDENTITY PRIMARY KEY,
    LawyerId INT NOT NULL FOREIGN KEY REFERENCES Lawyers(Id),
    ClientId INT NOT NULL FOREIGN KEY REFERENCES Clients(Id),
    Status NVARCHAR(MAX) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    RequestedAt DATETIME2 NOT NULL
);
GO

CREATE TABLE Cases (
    Id INT IDENTITY PRIMARY KEY,
    LawyerId INT NOT NULL FOREIGN KEY REFERENCES Lawyers(Id),
    ClientId INT NOT NULL FOREIGN KEY REFERENCES Clients(Id),
    RequestId INT NULL FOREIGN KEY REFERENCES LawyerRequests(Id),
    Title NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);
GO

CREATE TABLE Appointments (
    Id INT IDENTITY PRIMARY KEY,
    CaseId INT NULL FOREIGN KEY REFERENCES Cases(Id),
    LawyerId INT NOT NULL,
    ClientId INT NULL,
    Date NVARCHAR(MAX) NOT NULL,
    Time NVARCHAR(MAX) NOT NULL,
    Duration INT NOT NULL DEFAULT 60,
    Status NVARCHAR(MAX) NOT NULL,
    ReminderSent BIT NOT NULL DEFAULT 0,
    Notes NVARCHAR(MAX) NOT NULL
);
GO

CREATE TABLE Documents (
    Id INT IDENTITY PRIMARY KEY,
    CaseId INT NOT NULL FOREIGN KEY REFERENCES Cases(Id),
    FileName NVARCHAR(MAX) NOT NULL,
    FilePath NVARCHAR(MAX) NOT NULL,
    Size NVARCHAR(MAX) NOT NULL,
    UploadedBy NVARCHAR(MAX) NOT NULL,
    UploadedAt DATETIME2 NOT NULL
);
GO

-- Indexes
CREATE INDEX IX_Lawyers_UserId ON Lawyers(UserId);
CREATE INDEX IX_Clients_UserId ON Clients(UserId);
CREATE INDEX IX_LawyerRequests_LawyerId ON LawyerRequests(LawyerId);
CREATE INDEX IX_LawyerRequests_ClientId ON LawyerRequests(ClientId);
CREATE INDEX IX_Cases_LawyerId ON Cases(LawyerId);
CREATE INDEX IX_Cases_ClientId ON Cases(ClientId);
CREATE INDEX IX_Cases_RequestId ON Cases(RequestId);
CREATE INDEX IX_Appointments_CaseId ON Appointments(CaseId);
CREATE INDEX IX_Documents_CaseId ON Documents(CaseId);
GO

-- Seed: ApplicationUsers (demo accounts first to get lowest IDs)
INSERT INTO ApplicationUsers (Id, FullName, Role, Email, PasswordHash, PhoneNumber) VALUES
('u-lawyer-demo', 'Sarah Mitchell', 'lawyer', 'lawyer@example.com', '$2a$11$N0KRnqlVm3WIZVaxzrAkA.E3xXdZnN3YlL9RuFqbm0xzUNQV4TGtm', NULL),
('u-client-demo', 'Michael Torres', 'client', 'client@example.com', '$2a$11$N0KRnqlVm3WIZVaxzrAkA.E3xXdZnN3YlL9RuFqbm0xzUNQV4TGtm', NULL),
('u-l1', 'Sarah Mitchell', 'lawyer', 'sarah.mitchell@legaldesk.com', '$2a$11$N0KRnqlVm3WIZVaxzrAkA.E3xXdZnN3YlL9RuFqbm0xzUNQV4TGtm', NULL),
('u-l2', 'David Hernandez', 'lawyer', 'david.hernandez@legaldesk.com', '$2a$11$N0KRnqlVm3WIZVaxzrAkA.E3xXdZnN3YlL9RuFqbm0xzUNQV4TGtm', NULL),
('u-l3', 'Olivia Chen', 'lawyer', 'olivia.chen@legaldesk.com', '$2a$11$N0KRnqlVm3WIZVaxzrAkA.E3xXdZnN3YlL9RuFqbm0xzUNQV4TGtm', NULL),
('u-l4', 'James Kowalski', 'lawyer', 'james.kowalski@legaldesk.com', '$2a$11$N0KRnqlVm3WIZVaxzrAkA.E3xXdZnN3YlL9RuFqbm0xzUNQV4TGtm', NULL),
('u-c1', 'Michael Torres', 'client', 'michael.torres@email.com', '$2a$11$N0KRnqlVm3WIZVaxzrAkA.E3xXdZnN3YlL9RuFqbm0xzUNQV4TGtm', NULL),
('u-c2', 'Emily Watson', 'client', 'emily.watson@email.com', '$2a$11$N0KRnqlVm3WIZVaxzrAkA.E3xXdZnN3YlL9RuFqbm0xzUNQV4TGtm', NULL);
GO

-- Lawyers (demo lawyer = Id 1)
INSERT INTO Lawyers (UserId, FullName, YearsOfExperience, Specialization, Bio, Avatar, Rating, CasesWon, MaxClients) VALUES
('u-lawyer-demo', 'Sarah Mitchell', 12, 'Corporate Law', 'Sarah is a seasoned corporate attorney with over a decade of experience advising Fortune 500 companies on mergers, acquisitions, and regulatory compliance.', 'SM', 4.9, 142, 2),
('u-l2', 'David Hernandez', 8, 'Criminal Defense', 'David is a passionate criminal defense lawyer who believes in justice for all. He specializes in federal cases and white-collar crime defense.', 'DH', 4.7, 98, 2),
('u-l3', 'Olivia Chen', 15, 'Family Law', 'Olivia is one of the most respected family law attorneys in the state. She handles divorce, custody, and adoption cases with empathy and precision.', 'OC', 4.8, 210, 2),
('u-l4', 'James Kowalski', 10, 'Civil Litigation', 'James brings a strategic, analytical approach to civil litigation. He excels in contract disputes, personal injury, and property law.', 'JK', 4.6, 115, 2),
('u-l1', 'Sarah Mitchell', 12, 'Corporate Law', 'Sarah is a seasoned corporate attorney with over a decade of experience.', 'SM', 4.9, 142, 2);
GO

-- Clients (demo client = Id 1)
INSERT INTO Clients (UserId, FullName, Phone, Address, Avatar) VALUES
('u-client-demo', 'Michael Torres', '+1 555-0101', '742 Maple Avenue, Suite 200, New York, NY 10001', 'MT'),
('u-c2', 'Emily Watson', '+1 555-0202', '1580 Oak Lane, Apt 4B, Los Angeles, CA 90015', 'EW'),
('u-c1', 'Michael Torres', '+1 555-0101', '742 Maple Avenue, Suite 200, New York, NY 10001', 'MT');
GO

-- Requests (using demo lawyer Id=1, demo client Id=1)
INSERT INTO LawyerRequests (LawyerId, ClientId, Status, Message, RequestedAt) VALUES
(1, 1, 'approved', 'I need legal counsel for a corporate merger involving two subsidiaries.', '2026-04-01T10:00:00'),
(3, 2, 'approved', 'Seeking representation for a custody arrangement modification.', '2026-04-03T14:30:00'),
(2, 1, 'pending', 'I would like to discuss a federal investigation related to my business.', '2026-04-10T09:15:00'),
(4, 2, 'pending', 'I have a contract dispute with a former business partner.', '2026-04-12T16:45:00');
GO

-- Cases
INSERT INTO Cases (LawyerId, ClientId, RequestId, Title, Description, Status, CreatedAt) VALUES
(1, 1, 1, 'TechCorp Merger Advisory', 'Providing legal counsel and documentation for the merger of TechCorp''s North American and European divisions.', 'active', '2026-04-02T08:00:00'),
(3, 2, 2, 'Watson Custody Modification', 'Representing the client in modifying the existing custody arrangement.', 'active', '2026-04-04T10:00:00');
GO

-- Appointments
DECLARE @today NVARCHAR(10) = CONVERT(NVARCHAR(10), GETDATE(), 23);
DECLARE @tomorrow NVARCHAR(10) = CONVERT(NVARCHAR(10), DATEADD(DAY,1,GETDATE()), 23);
DECLARE @dayafter NVARCHAR(10) = CONVERT(NVARCHAR(10), DATEADD(DAY,2,GETDATE()), 23);

INSERT INTO Appointments (CaseId, LawyerId, ClientId, Date, Time, Duration, Status, ReminderSent, Notes) VALUES
(1, 1, 1, @today, '23:00', 60, 'confirmed', 0, 'Reviewed merger timeline and key milestones. Client approved the preliminary due-diligence checklist.'),
(NULL, 1, NULL, @tomorrow, '14:00', 60, 'available', 0, ''),
(2, 3, 2, @tomorrow, '11:00', 45, 'confirmed', 0, ''),
(NULL, 1, NULL, @dayafter, '09:00', 60, 'available', 0, ''),
(NULL, 3, NULL, @dayafter, '15:00', 45, 'available', 0, ''),
(NULL, 2, NULL, @tomorrow, '10:00', 60, 'available', 0, ''),
(1, 1, 1, '2026-04-10', '10:00', 60, 'completed', 0, 'Initial consultation completed. Discussed scope of the merger and legal requirements.');
GO

-- Documents
INSERT INTO Documents (CaseId, FileName, FilePath, Size, UploadedBy, UploadedAt) VALUES
(1, 'Merger_Agreement_Draft_v1.pdf', '', '2.4 MB', '1', '2026-04-05T12:00:00'),
(1, 'Due_Diligence_Checklist.xlsx', '', '540 KB', '1', '2026-04-06T09:30:00'),
(2, 'Custody_Modification_Petition.pdf', '', '1.1 MB', '3', '2026-04-07T14:00:00'),
(1, 'Financial_Statements_Q1.pdf', '', '3.8 MB', '1', '2026-04-08T11:00:00');
GO
