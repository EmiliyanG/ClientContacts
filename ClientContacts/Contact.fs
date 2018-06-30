namespace ClientContacts

module Contact =
    open Elmish
    open Elmish.WPF
    open System

    type Msg = 
        |UpdateModel

    type Model = {id:int ; ContactName: string; IsDisabled: bool; IsAdmin: bool; organisationId:int}

    let init() = {id = 0 ; ContactName = ""; IsDisabled = true; IsAdmin=true; organisationId=0}
    
    let update msg model = 
        match msg with
        |UpdateModel -> model
    

    let view (msg:Msg) (model:Model) = 
        [ "IsDisabled" |> Binding.oneWay (fun m -> m.IsDisabled)
          "IsAdmin" |> Binding.oneWay (fun m -> m.IsAdmin)
          "ContactName" |> Binding.oneWay (fun m -> m.ContactName)]

