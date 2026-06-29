-- Parking Lot*******
-- *                *
-- *                *
--- *****************



-- Comment can be done single line with --
-- Comment can be done multi line with /* */

/*
DQL - Data Query Language
Keywords:

SELECT - retrieve data, select the columns from the resulting set
FROM - the table(s) to retrieve data from
WHERE - a conditional filter of the data
GROUP BY - group the data based on one or more columns
HAVING - a conditional filter of the grouped data
ORDER BY - sort the data
*/


-- BASIC CHALLENGES
-- List all customers (full name, customer id, and country) who are not in the USA

USE Chinook_AutoIncrement;
SELECT FirstName, LastName, CustomerId, Country
FROM dbo.Customer
WHERE Country NOT LIKE 'USA';

-- List all customers from Brazil
SELECT CustomerId, FirstName, Country,
CONCAT(FirstName, ' ', LastName) as FullName
FROM dbo.Customer
WHERE Country = 'Brazil';

-- List all sales agents

SELECT * FROM dbo.Employee WHERE title LIKE '%Agent%';

-- Retrieve a list of all countries in billing addresses on invoices

SELECT DISTINCT BillingCountry FROM dbo.Invoice;

-- Retrieve how many invoices there were in 2021, and what was the sales total for that year?

SELECT COUNT(*) AS Invoices , SUM(Total) AS Total
FROM dbo.Invoice 
WHERE InvoiceDate LIKE '%2021%';


-- (challenge: find the invoice count sales total for every year using one query)


-- how many line items were there for invoice #37

SELECT COUNT(*)
FROM dbo.InvoiceLine 
WHERE InvoiceId = 37;

-- how many invoices per country? BillingCountry  # of invoices 

SELECT BillingCountry, COUNT(*) as TotalInvoices
FROM dbo.Invoice
GROUP BY BillingCountry
ORDER BY BillingCountry;

-- Retrieve the total sales per country, ordered by the highest total sales first.

SELECT BillingCountry, SUM(Total) AS TotalSales
FROM dbo.Invoice
GROUP BY BillingCountry
ORDER BY BillingCountry;


-- JOINS CHALLENGES
-- Every Album by Artist
SELECT A.Name, B.Title
FROM dbo.Album AS B
JOIN dbo.Artist AS A ON A.ArtistId =  B.ArtistId
ORDER BY A.Name;

-- (inner keyword is optional for inner join)
-- All songs of the rock genre

SELECT A.Name AS SongName, B.Name Genre
FROM dbo.Genre AS B
JOIN dbo.Track AS A ON A.GenreId =  B.GenreId
WHERE B.Name = 'Rock'
ORDER BY A.Name;

-- Show all invoices of customers from brazil (mailing address not billing)
SELECT I.*, C.Address CustomerAddress
FROM dbo.Invoice AS I
JOIN dbo.Customer AS C ON C.CustomerId =  I.CustomerId
WHERE C.Country = 'Brazil';

-- Show all invoices together with the name of the sales agent for each one



-- Which sales agent made the most sales in 2021?

SELECT TOP 1 E.FirstName AS EmployeeName, SUM(I.Total)
FROM dbo.Employee AS E
JOIN dbo.Customer AS C ON C.SupportRepId = E.EmployeeId
JOIN dbo.Invoice AS I ON I.CustomerId =  C.CustomerId
GROUP BY E.FirstName;

-- How many customers are assigned to each sales agent?

SELECT E.FirstName AS EmployeeName, COUNT(C.SupportRepId)
FROM dbo.Employee AS E
JOIN dbo.Customer AS C ON C.SupportRepId = E.EmployeeId
GROUP BY E.FirstName;

-- Which track was purchased the most in 2022?

SELECT T.Name Track, SUM(IL.Quantity) CuantitySold
FROM dbo.Track AS T
JOIN dbo.InvoiceLine AS IL ON IL.TrackId = T.TrackId
JOIN dbo.Invoice I ON IL.InvoiceId = I.InvoiceId
WHERE I.InvoiceDate LIKE '%2022%'
GROUP BY T.Name
ORDER BY CuantitySold DESC;

-- Show the top three best selling artists.


-- Which customers have the same initials as at least one other customer?


-- Which countries have the most invoices?


-- Which city has the customer with the highest sales total?


-- Who is the highest spending customer?


-- Return the email and full name of of all customers who listen to Rock.


-- Which artist has written the most Rock songs?


-- Which artist has generated the most revenue?




-- ADVANCED CHALLENGES
-- solve these with a mixture of joins, subqueries, CTE, and set operators.
-- solve at least one of them in two different ways, and see if the execution
-- plan for them is the same, or different.

-- 1. which artists did not make any albums at all?


-- 2. which artists did not record any tracks of the Latin genre?


-- 3. which video track has the longest length? (use media type table)



-- 4. boss employee (the one who reports to nobody)


-- 5. how many audio tracks were bought by German customers, and what was
--    the total price paid for them?


-- 6. list the names and countries of the customers supported by an employee
--    who was hired younger than 35.


-- DML exercises

-- 1. insert two new records into the employee table.

-- 2. insert two new records into the tracks table.

-- 3. update customer Aaron Mitchell's name to Robert Walter

-- 4. delete one of the employees you inserted.

-- 5. delete customer Robert Walter.
