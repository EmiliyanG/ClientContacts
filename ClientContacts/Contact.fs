namespace ClientContacts

module Contact =
    open Elmish
    open Elmish.WPF
    open System

    type Msg = 
        |UpdateModel
        
    type Model = { IsUsedAsLoadingButton:bool; id:int ; ContactName: string; IsDisabled: bool; IsAdmin: bool; organisationName:string}

    let init() = {IsUsedAsLoadingButton = true; id = 0 ; ContactName = ""; IsDisabled = false; IsAdmin=false; organisationName=""}
    
    let update msg model = 
        match msg with
        |UpdateModel -> model
    

    let view (msg:Msg) (model:Model) = 
        [ "IsDisabled" |> Binding.oneWay (fun m -> m.IsDisabled)
          "IsAdmin" |> Binding.oneWay (fun m -> m.IsAdmin)
          "ContactName" |> Binding.oneWay (fun m -> m.ContactName)
          "IsUsedAsLoadingButton" |> Binding.oneWay (fun m -> m.IsUsedAsLoadingButton)
          ]

