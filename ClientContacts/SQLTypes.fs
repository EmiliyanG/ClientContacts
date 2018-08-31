namespace ClientContacts
module SQLTypes = 
    type Contact = {id: int; ContactName: string; IsDisabled: bool; IsAdmin: bool; organisationName: string; organisationId: int; locationName: string}
    type ListContactsParams = {searchPattern: string; Limit: int; Offset: int}
    
    type ContactInfo = {id: int; ContactName: string; IsDisabled: bool; IsAdmin: bool; email: string option; 
                        telephone: string option; organisationId: int; locationId: int option; comments: string option}
    type ContactInfoQueryParams = {id: int}

    type Location = {id: int; locationName: string; organisationId: int }
    type Organisation = {id: int; organisationName: string }
    type OrganisationLocationsQueryParams = {organisationId: int}
    
    type OrganisationId = 
        |OrganisationId of int
        member this.getData = 
             match this with
             | OrganisationId i -> i
    
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
     
     type OrganisationName =
        |OrganisationName of string
        member this.getData = 
             match this with
             | OrganisationName s -> s