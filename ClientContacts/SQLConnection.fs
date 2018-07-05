namespace ClientContacts


module SQLQueries = 
    [<Literal>]
    let ConnectionString = """Server=EMILIYAN;Database=test;Integrated Security=true"""
    
    [<Literal>]
    let ListContacts = 
        //WAITFOR DELAY '00:00:10';
        """
        Select c.id, ContactName, IsDisabled, IsAdmin, o.name as organisationName, l.name as locationName
        from Contact c
        inner join Organisation o on o.id = c.organisationId
		left join Location l on l.id = c.locationId
        where ContactName like '%'+@searchPattern+'%' or o.name like '%'+@searchPattern+'%'
        order by o.name, l.name desc, ContactName OFFSET @offset ROWS fetch NEXT @limit ROWS ONLY"""
    
    [<Literal>]
    let ContactInfoQuery = 
        //WAITFOR DELAY '00:00:10';
        """
        Select c.id, ContactName, IsDisabled, IsAdmin, email, telephone, o.name as organisationName, comments
        from Contact c 
        inner join Organisation o on c.organisationId = o.id
        where c.id = @id"""

module SQLTypes = 
    type Contact = {id: int; ContactName: string; IsDisabled: bool; IsAdmin: bool; organisationName: string; locationName: string}
    type ListContactsParams = {searchPattern: string; Limit: int; Offset: int}
    
    type ContactInfo = {id: int; ContactName: string; IsDisabled: bool; IsAdmin: bool; email: string option; 
                        telephone: string option; organisationName: string; comments: string option}

    type ContactInfoQueryParams = {id: int}

    type Limit = 
        |Limit of int
        member this.getData = 
             match this with
             | Limit s -> s
     type Offset =     
        |Offset of int
        member this.getData = 
             match this with
             | Offset s -> s
     


module MySQLConnection = 
    open System.Data.SqlClient
    open Dapper
    open SQLQueries
    open SQLTypes
    open Contact
    open System
    open Elmish
    
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
            |> Seq.map( fun c -> {IsUsedAsLoadingButton=false; id = c.id ;
                                  ContactName = c.ContactName; 
                                  IsDisabled = c.IsDisabled; 
                                  IsAdmin=c.IsAdmin; 
                                  organisationName=c.organisationName;
                                  locationName = c.locationName} )
        
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
            let conn = openConnection() 
            let q =conn.Query<ContactInfo> (ContactInfoQuery ,{id = id})
                   |> Seq.tryHead
            conn.Close()
            return q, dateTriggered
            }

    ///return Async task to load list of contacts from the database
    let getListOfContacts searchString (dateTriggered:DateTime) limit offset =
        async {
            let conn = openConnection() 
            let q = getContacts conn searchString limit offset
            conn.Close()
            return q,dateTriggered
            }
                //return b, elapsed
           // } 
        
        

        
