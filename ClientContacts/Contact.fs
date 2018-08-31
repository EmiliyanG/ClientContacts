namespace ClientContacts

module Contact =
    open Elmish
    open Elmish.WPF
    open System
    open SQLTypes

    type Msg = 
        |UpdateModel
        
    type Model = { IsUsedAsLoadingButton:bool; id:int ; ContactName: string; IsDisabled: bool; IsAdmin: bool; organisationName:string; organisationId: int; locationName:string}

    let init() = {IsUsedAsLoadingButton = true; id = 0 ; ContactName = ""; IsDisabled = false; IsAdmin=false; organisationName=""; organisationId=0; locationName=""}
    
    let castSQLContactToContactModel (c:Contact) = 
        {IsUsedAsLoadingButton=false; id = c.id ;
        ContactName = c.ContactName; 
        IsDisabled = c.IsDisabled; 
        IsAdmin=c.IsAdmin; 
        organisationName=c.organisationName;
        organisationId = c.organisationId;
        locationName = c.locationName} 
    
    let castContactInfoToContactModel (c:ContactInfo) (o:OrganisationName) (l:Location option) = 
        {IsUsedAsLoadingButton=false; id = c.id ;
        ContactName = c.ContactName; 
        IsDisabled = c.IsDisabled; 
        IsAdmin=c.IsAdmin; 
        organisationName=o.getData;
        organisationId = c.organisationId;
        locationName = 
            match l with 
            |Some l -> l.locationName
            |None -> null
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

