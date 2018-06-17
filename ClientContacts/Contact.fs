namespace ClientContacts

module Contact =
    open Elmish
    open Elmish.WPF
    open System

    type Msg = 
        |UpdateModel

    type Model = {id:int ; ContactName: string; IsDisabled: bool; IsAdmin: bool}

    let init() = {id = 0 ; ContactName = "contact 1"; IsDisabled = true; IsAdmin=true}
    
    let update msg model = 
        match msg with
        |UpdateModel -> model
    

    let view (msg:Msg) (model:Model) = 
        [ "IsDisabled" |> Binding.oneWay (fun m -> m.IsDisabled)
          "IsAdmin" |> Binding.oneWay (fun m -> m.IsAdmin)
          "ContactName" |> Binding.oneWay (fun m -> m.ContactName)]

