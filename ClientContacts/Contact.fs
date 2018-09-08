namespace ClientContacts

module Contact =
    open Elmish
    open Elmish.WPF
    open System
    open SQLTypes
    open SharedTypes

    type Msg = 
        |UpdateModel
        
    type Model = { IsUsedAsLoadingButton:bool; 
                   id:int
                   ContactName: string
                   IsDisabled: bool
                   IsAdmin: bool
                   organisation:Entity
                   location:Entity}

    let init() = {IsUsedAsLoadingButton = true
                  id = 0
                  ContactName = ""
                  IsDisabled = false
                  IsAdmin=false
                  organisation={entityType=Organisation; id=0; name=""}
                  location={entityType=Location; id=0; name=""}}
    
    let castSQLContactToContactModel (c:Contact) = 
        {
        IsUsedAsLoadingButton=false
        id = c.id
        ContactName = c.ContactName
        IsDisabled = c.IsDisabled
        IsAdmin=c.IsAdmin
        organisation={entityType=Organisation; id=c.organisationId; name=c.organisationName}
        location={entityType=Location; id=c.locationId; name=c.locationName}
        } 
    
    let castContactInfoToContactModel (c:ContactInfo) (o:Organisation) (l:Location option) = 
        {
        IsUsedAsLoadingButton=false
        id = c.id
        ContactName = c.ContactName
        IsDisabled = c.IsDisabled
        IsAdmin=c.IsAdmin
        organisation={entityType=Organisation; id=o.id; name=o.organisationName}
        location={entityType=Location
                  id=match l with 
                     |Some l -> l.id
                     |None -> -1
                  name=match l with 
                       |Some l -> l.locationName
                       |None -> null}
        } 

    let update msg model = 
        match msg with
        |UpdateModel -> model
    

    let view (msg:Msg) (model:Model) = 
        [ "IsDisabled" |> Binding.oneWay (fun m -> m.IsDisabled)
          "IsAdmin" |> Binding.oneWay (fun m -> m.IsAdmin)
          "ContactName" |> Binding.oneWay (fun m -> m.ContactName)
          "IsUsedAsLoadingButton" |> Binding.oneWay (fun m -> m.IsUsedAsLoadingButton)
          ]

