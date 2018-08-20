namespace ClientContacts
module LocationPopup=
    open Elmish
    open Elmish.WPF
    open System
    open SQLTypes

    type Msg = 
        |UpdateModel
        
    type Model = { organisationsList:Organisation seq; locationField:string option}

    let init() = {organisationsList = []; locationField = None}
    
    let update msg model = 
        match msg with
        |UpdateModel -> model
    

    let view (msg:Msg) (model:Model) = 
        [ "location" |> Binding.oneWay (fun m -> "")
          ]



