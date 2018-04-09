namespace ClientContacts


module SQLQueries = 
    [<Literal>]
    let ConnectionString = """Server=EMILIYAN;Database=test;Integrated Security=true"""
    
    [<Literal>]
    let ListContacts = 
        """
        WAITFOR DELAY '00:00:10';
        Select * from Contact 
        where ContactName like '%'+@searchPattern+'%'
        order by ContactName OFFSET @offset ROWS fetch NEXT @limit ROWS ONLY"""

module SQLTypes = 
    type Contact = {ContactName: string; IsDisabled: bool; IsAdmin: bool}
    type ListContactsParams = {searchPattern: string; Limit: int; Offset: int}
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
    
    

    let openConnection() = 
        new SqlConnection(ConnectionString)

    let dapperQuery<'DapperResult> query (conn:SqlConnection) = 
        conn.Query<'DapperResult> query 

    let dapperQueryWithVariables<'DapperResult> query variables (conn:SqlConnection) = 
        conn.Query<'DapperResult> (query, variables)
        


    let getContacts conn search (l:Limit) (o:Offset) = 
        //Promise() {

        let q = 
            dapperQueryWithVariables<Contact> ListContacts {searchPattern = search; Limit= l.getData; Offset=o.getData} conn
            |> Seq.map (fun c -> {id = Guid.NewGuid() ; ContactName = c.ContactName; IsDisabled = c.IsDisabled; IsAdmin=c.IsAdmin})
            |> List.ofSeq

        q
    ///return Async task to load list of contacts from the database
    let getListOfContacts searchString (dateTriggered:DateTime) =
        async {
            let conn = openConnection() 
            let q = getContacts conn searchString (Limit(10)) (Offset(0))
            conn.Close()
            return q,dateTriggered
            }
                //return b, elapsed
           // } 
        
        

        
