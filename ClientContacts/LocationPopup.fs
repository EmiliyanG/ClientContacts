namespace ClientContacts
module LocationPopup=
    open Elmish
    open Elmish.WPF
    open System
    open SQLTypes

    type Msg = 
        |UpdateModel
        |Cancel
        
    type Model = { organisationsList:Organisation seq
                   locationField:string option
                   IsVisible: bool
                   }

    let init() = {organisationsList = []
                  locationField = None
                  IsVisible=true
                  }
    
    let update msg model = 
        match msg with
        |UpdateModel -> model, Cmd.none
        |Cancel -> 
            {model with IsVisible= false}, Cmd.none
    

    let locationPopupViewBindings: ViewBinding<Model, Msg> list = 
        [ 
            "Cancel" |> Binding.cmd (fun param m -> Cancel)
            "location" |> Binding.oneWay (fun m -> "")
            "LocationPopupVisibility" |> Binding.oneWay (fun m -> m.IsVisible)
          ]



