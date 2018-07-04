begin tran 
--commit tran 
--rollback tran



Declare @organisationNames table (name varchar(250))
Insert into @organisationNames(name)
Values
('Tulip Media'),('Wood Intelligence'),('Apple Lighting'),('Riverecords'),('Riverecords'),('Whiteoutwares'),('Globeworks'),
('Herbwater'),('Grottomedia'),('Cannonstone'),('Spider Solutions'),('Shadow Acoustics'),('Fire Technologies'),('Frostfiretronics'),('Firetronics'),('Surprisystems'),('Nightelligence'),('Wizardforce'),
('Apachemedia'),('Purplewalk'),('Glacier Co.'),('Phenomenon Navigations'),('Wonder Security'),
('Globeworks'),('Oakoms'),('Flukords'),('Thorecords'),('Grizzlyhut'),('Rootbit'),('Granitedream')


DECLARE @contactNames table (name varchar(250))
insert into @contactNames(name)
values
('Bethany Saunders'),('Ryder Osborne'),('Izabella Johns'),('Spencer Wilkins'),('Audrey Harvey'),
('Alexis Donaldson'),('Kyle Ritter'),('Cedric Rowe'),('Matteo Christian'),('Ace David'),('Seth Mathis'),
('Julius Becker'),('Brice Ibarra'),('Harrison Hobbs'),('Riley Patterson'),('Brent Villa'),('Daisy Lucero'),
('Kaylyn Leblanc'),('Albert Sutton'),('Arielle Bender'),('Tatiana Mcdowell'),('Antony Lang'),('Ashleigh Buck'),
('Gaige Braun'),('Wesley Avery'),('Sariah Collins'),('Mylee Hickman'),('Juliet Kaiser'),('Jaydan Brennan'),
('Talan Mcgrath'),('Tobias Finley'),('Augustus Allen'),('Kaylie Rivas'),('Jaylan Bowers'),('Marlon Blackburn'),
('Genevieve Delacruz'),('Harley Kennedy'),('Chasity Larsen'),('Lola Booth'),('Aliza Watson'),('Addyson Mendoza'),
('Yamilet Newman'),('Gustavo Taylor'),('Frida Olson'),('Angelique Andersen'),('Gilbert Carpenter'),('Devan Howard'),
('Adam Henson'),('Donte Cherry'),('Frank Beard'),('Journey Norris'),('Mollie Sanchez'),('Hazel Humphrey'),
('Guillermo Bartlett'),('Dayton Abbott'),('Eliana Herrera'),('Elsa Morrow'),('Andre Kane'),('Immanuel Gillespie'),
('Kristina Dominguez'),('Malik Shannon'),('Morgan Vaughn'),('Bailey Quinn'),('Kevin Joyce'),('Savanah Gray'),
('Norah Walter'),('Peter Madden'),('Chana Tucker'),('Janiah Rasmussen'),('Caitlyn Lester'),('Denzel Adams'),
('Brian Castro'),('Amy Cochran'),('Kaylee Dennis'),('Ernest Lyons'),('Christian Hoover'),('Davon Conrad'),
('Hayden Jackson'),('Amiyah Warner'),('Kyle Kim'),('Josue Owens'),('Maverick Pham'),('Madeleine Haynes'),
('Maggie Meza'),('Dane Farley'),('Zaiden Shepherd'),('Isiah Serrano'),('Dominic Luna'),('Jaylin Reed'),
('Jaydon Yu'),('Dominik Carney'),('Gisselle Powers'),('Mateo Dyer'),('Juliette Adkins'),('Makhi Buchanan'),
('Saul Harvey'),('Zion Woods'),('Paityn Acosta'),('Sofia Morse'),('Ruben Cline'),('Emmett Stewart'),
('Shayla Haley'),('Elyse Greene'),('Felicity Sweeney'),('Braeden Mccormick'),('Nia Rivers'),('Addison Espinoza'),
('Pedro Murray'),('Rigoberto Harrell'),('Hailie Sheppard'),('Maia Barber'),('Kaley Ramirez'),('Brylee Barrett'),
('Johan Long'),('Lexi Stanley'),('Ann Liu'),('Darian Stone'),('Kirsten Pratt'),('Wendy Villanueva'),
('Uriah Higgins'),('Osvaldo Lindsey'),('Lydia Harrison'),('Evelin Jenkins'),('Rachel Savage'),
('Kamari Wilkerson'),('Kelsie Wiggins'),('Danielle Flowers'),('Broderick Harmon'),('Brodie Banks'),
('Rodrigo Munoz'),('John Rice'),('Armani Griffin')


Declare @seed int = (Select max(id) from Organisation)
if @seed is null 
BEGIN 
Set @seed = 1
END
Insert into Organisation(id, name)  
Select @seed + ROW_NUMBER() over (order by name) , name from @organisationNames


set @seed = (Select max(id) from Contact)
if @seed is null
BEGIN 
Set @seed = 1
END

Declare @organisationCount int = (Select count(*) from Organisation)
;with orgIds as (
	Select ROW_NUMBER() over (order by name) as rownum, id 
	from Organisation
), contacts as (
Select row_number() over(order by name) as rownum, name
 from @contactNames
)
Insert into Contact(ContactName, IsDisabled, IsAdmin, email, telephone, organisationId, id, comments)
Select 
	name, --ContactName
	ABS(CHECKSUM(NewId())) % 2,--IsDisabled
	ABS(CHECKSUM(NewId())) % 2, --IsAdmin
	REPLACE(name, ' ','') + '@gmail.com', --email
	('07'+
	CAST(ABS(CHECKSUM(NewId())) % 10 as VARCHAR(10)) +
	CAST(ABS(CHECKSUM(NewId())) % 10 as VARCHAR(10)) +
	CAST(ABS(CHECKSUM(NewId())) % 10 as VARCHAR(10)) +
	CAST(ABS(CHECKSUM(NewId())) % 10 as VARCHAR(10)) +
	CAST(ABS(CHECKSUM(NewId())) % 10 as VARCHAR(10)) +
	CAST(ABS(CHECKSUM(NewId())) % 10 as VARCHAR(10)) +
	CAST(ABS(CHECKSUM(NewId())) % 10 as VARCHAR(10)) +
	CAST(ABS(CHECKSUM(NewId())) % 10 as VARCHAR(10))), --telephone
	(Select id from orgIds ids where ids.rownum = ((c.rownum % @organisationCount) + 1)), --organisationId
	(@seed + rownum), --id
	NULL

from contacts c


