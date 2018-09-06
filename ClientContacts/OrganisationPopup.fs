namespace ClientContacts
module OrganisationPopup=
    open Elmish
    open Elmish.WPF
    open System
    open SQLTypes
    open ElmishUtils
    open System.Threading
    open MySQLConnection
    open Validator
    open DebugUtils

    type Mode = 
        |EditOrganisation
        |AddNewOrganisation

    type Msg = 
        |EditOrganisation of Organisation
        |UpdateOrganisationInput of string
        |Cancel
        |TrySaving
        |SavedSuccessfully
        |FailureWhileSaving of exn
        
    type Model = { 
                   isVisible: bool
                   mode: Mode
                   OrganisationInput: Organisation
                   validation: string option
                   }

    let init() = {
                  isVisible=false
                  mode= Mode.EditOrganisation
                  OrganisationInput = {id= -1; organisationName=""}
                  validation= None
                  }
    
    let getTitleFromMode(x:Mode)=
        match x with 
        |Mode.EditOrganisation -> "Edit Organisation"
        |AddNewOrganisation -> "Add New Organisation"


    let update (msg:Msg) (model:Model) = 
        match msg with
        |UpdateOrganisationInput v-> 
            {model with 
                   OrganisationInput={model.OrganisationInput 
                                      with organisationName=v}}, Cmd.none
        |EditOrganisation org -> 
            {model with 
                   isVisible=true
                   OrganisationInput = org
                   mode=Mode.EditOrganisation
                }, Cmd.none
        |Cancel -> 
            {model with isVisible = false}, Cmd.none
        |TrySaving -> 
            match validateOrganisation model.OrganisationInput.organisationName with 
            |Success e-> 

                
                {model with validation = None}, 
                Cmd.ofAsync (updateOrInsertOrganisation)
                            model.OrganisationInput
                            (fun a -> SavedSuccessfully)
                            (fun e -> FailureWhileSaving e)
            | Failure msg -> 
                {model with validation = Some msg}, 
                Cmd.none
        |SavedSuccessfully -> 
            {model with isVisible= false}, 
            Cmd.none
        |FailureWhileSaving e-> 
            failwith <| sprintf "%s\n%s" e.Message e.StackTrace
            model, Cmd.none
            

    

    let organisationPopupViewBindings: ViewBinding<Model, Msg> list = 
        [ 
            "OrganisationPopupVisibility" |> Binding.oneWay (fun m -> m.isVisible)
            "Title" |> Binding.oneWayMap (fun m -> m.mode) 
                                         (fun v -> getTitleFromMode v)
            "Cancel" |> Binding.cmd (fun m param -> Cancel)
            "Save" |> Binding.cmd (fun m param -> TrySaving)
            "OrganisationText" |> Binding.twoWay (fun m -> m.OrganisationInput.organisationName)
                                                 (fun v m -> UpdateOrganisationInput(string v) )
            "OrganisationValidations" |> Binding.oneWayMap 
                                                (fun m-> m.validation) 
                                                (fun v -> 
                                                    match v with
                                                    | Some o -> o
                                                    | None -> "")
          ]





