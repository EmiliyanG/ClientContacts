namespace ClientContacts


module SQLQueries = 
    [<Literal>]
    let ConnectionString = """Server=EMILIYAN;Database=test;Integrated Security=true"""
    
    [<Literal>]
    let ListContacts = 
        //WAITFOR DELAY '00:00:10';
        """
        Select c.id, ContactName, IsDisabled, IsAdmin, o.name as organisationName, o.id as organisationId, l.name as locationName
        from Contact c
        inner join Organisation o on o.id = c.organisationId
		left join Location l on l.id = c.locationId
        where ContactName like '%'+@searchPattern+'%' or o.name like '%'+@searchPattern+'%'
        order by o.name, l.name desc, ContactName OFFSET @offset ROWS fetch NEXT @limit ROWS ONLY"""
    
    [<Literal>]
    let ContactInfoQuery = 
        //WAITFOR DELAY '00:00:10';
        """
        Select c.id, ContactName, IsDisabled, IsAdmin, email, telephone,
        o.id as organisationId, l.id as locationId, comments
        from Contact c 
        inner join Organisation o on c.organisationId = o.id
        left join Location l on l.id = c.locationId
        where c.id = @id"""
    [<Literal>]
    let UpdateContactInfoQuery = 
        """
        Update Contact
        set ContactName = @ContactName,
	        IsDisabled = @IsDisabled,
	        IsAdmin = @IsAdmin,
	        email = @email,
	        telephone = @telephone,
	        organisationId = @organisationId,
	        locationid = @locationId,
	        comments = @comments 
        where id = @id"""
    
    [<Literal>]
    let InsertContactInfoQuery = 
        """
        Insert into Contact 
        (ContactName,IsDisabled,IsAdmin,email,telephone,organisationId,id,comments,locationid)
        Values
        (@ContactName,@IsDisabled,@IsAdmin,@email,@telephone,@organisationId,
        (Select isnull(max(id) + 1,1) from Contact), @comments, @locationId)"""
    
    [<Literal>]
    let InsertLocationQuery = 
        """
        Insert into Location(Id, Name, organisationId)
        Values
        ((Select isnull(max(id)+1, 1) from Location),@locationName, @organisationId)
        """
    [<Literal>]
    let OrganisationLocationsQuery =
        """
        Select id, Name as locationName, organisationId from Location
        where organisationId = @organisationId"""

    [<Literal>]
    let OrganisationsQuery =
        """
        Select id, name as organisationName from Organisation"""


module MySQLConnection = 
    open System.Data.SqlClient
    open Dapper
    open SQLQueries
    open SQLTypes
    open Contact
    open System
    open Elmish
    open DebugUtils
    
    //copied from stackoverflow https://stackoverflow.com/questions/42797288/dapper-column-to-f-option-property
    //author: Charles Mager
    //answered Mar 15 '17 at 9:57
    type OptionHandler<'T>() =
        inherit SqlMapper.TypeHandler<option<'T>>()

        override __.SetValue(param, value) = 
            let valueOrNull = 
                match value with
                | Some x -> box x
                | None -> null

            param.Value <- valueOrNull    

        override __.Parse value =
            if isNull value || value = box DBNull.Value 
            then None
            else Some (value :?> 'T)
    
    
    let openConnection() = 
        SqlMapper.AddTypeHandler (OptionHandler<string>())
        SqlMapper.AddTypeHandler (OptionHandler<int>())
        new SqlConnection(ConnectionString)
        
        
    let getContacts (conn:SqlConnection) search (l:Limit) (o:Offset) = 
        
        
        let contacts = 
            conn.Query<Contact> (ListContacts, ({searchPattern = search; Limit= l.getData; Offset=o.getData}) )
            |> Seq.map castSQLContactToContactModel 
        
        match Seq.length contacts with 
        | a when a < l.getData -> 
            contacts
            |> List.ofSeq
        | _ -> //if the sequence length is equal to the query limit more results are expected
            contacts
            |> Seq.append <| (seq [{Contact.init() with IsUsedAsLoadingButton=true}]) //append loading btn at the end
            |> List.ofSeq
        
    ///return Async task to load contact info for given contact id
    let getContactInfo id (dateTriggered:DateTime)= 
        async {
            use conn = openConnection() 
            let q =conn.Query<ContactInfo> (ContactInfoQuery ,{id = id})
                   |> Seq.tryHead
            conn.Close()
            return q, dateTriggered
            }
    
    let getOrganisationLocations (organisationId:OrganisationId) (dateTriggered:DateTime)=
        async{
            use conn = openConnection() 
            let q =conn.Query<Location> (OrganisationLocationsQuery ,{organisationId = organisationId.getData})
            conn.Close()
            
            return (match Seq.isEmpty q with 
                    | true -> None
                    | false -> Some q), dateTriggered
        }

    let getOrganisations (dateTriggered:DateTime)=
        async{
            use conn = openConnection() 
            let q =conn.Query<Organisation> (OrganisationsQuery)
            conn.Close()
            
            return q, dateTriggered
        }

    let updateContactInfo (contactInfo:ContactInfo)=
        async{
            use conn = openConnection() 
            let affectedRows =conn.Execute (UpdateContactInfoQuery,contactInfo)
            conn.Close()
            return affectedRows
        }
    
    let insertContactInfo (contactInfo:ContactInfo)=
        async{
            use conn = openConnection() 
            let affectedRows =conn.Execute (InsertContactInfoQuery,contactInfo)
            conn.Close()
            return affectedRows
        }
    
    let updateOrInsertContactInfo (contactInfo:ContactInfo) = 
        match contactInfo.id with 
        | -1 -> insertContactInfo contactInfo
        |_ -> updateContactInfo contactInfo
    
    let insertLocation (location:Location)=
        async{
            use conn = openConnection() 
            let affectedRows =conn.Execute (InsertLocationQuery,location)
            conn.Close()
            return affectedRows
        }

    ///return Async task to load list of contacts from the database
    let getListOfContacts searchString (dateTriggered:DateTime) limit offset =
        async {
            use conn = openConnection() 
            let q = getContacts conn searchString limit offset
            conn.Close()
            return q,dateTriggered
            }
            
        
        

        
