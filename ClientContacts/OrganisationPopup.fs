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
        |RefreshContactList
        |AddNewOrganisation
        |EditOrganisation of Organisation
        |UpdateOrganisationInput of string
        |Cancel
        |TrySaving
        |SavedSuccessfully of Organisation
        |UpdateContactsWithEditedOrganisationName of oldName : OrganisationName *  newName: OrganisationName 
        |FailureWhileSaving of exn
        
    type Model = { 
                   isVisible: bool
                   mode: Mode
                   organisationNameSnapshot: OrganisationName
                   OrganisationInput: Organisation
                   validation: string option
                   }

    let init() = {
                  isVisible=false
                  mode= Mode.EditOrganisation
                  organisationNameSnapshot=OrganisationName("")
                  OrganisationInput = {id= -1; organisationName=""}
                  validation= None
                  }
    
    let getTitleFromMode(x:Mode)=
        match x with 
        |Mode.EditOrganisation -> "Edit Organisation"
        |Mode.AddNewOrganisation -> "Add New Organisation"


    let update (msg:Msg) (model:Model) = 
        match msg with
        |RefreshContactList -> 
            failwith <| sprintf "this message should have been caught 1 level up: RefreshContactList"
            model, Cmd.none
        |UpdateOrganisationInput v-> 
            {model with 
                   OrganisationInput={model.OrganisationInput 
                                      with organisationName=v}}, Cmd.none
        |AddNewOrganisation -> 
            {init() with 
                isVisible=true
                mode = Mode.AddNewOrganisation}, Cmd.none
        |EditOrganisation org -> 
            {model with 
                   isVisible=true
                   OrganisationInput = org
                   organisationNameSnapshot = OrganisationName <| org.organisationName
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
                            (fun a -> SavedSuccessfully model.OrganisationInput)
                            (fun e -> FailureWhileSaving e)
            | Failure msg -> 
                {model with validation = Some msg}, 
                Cmd.none
        |SavedSuccessfully org-> 
            {model with isVisible= false}, 
            match org.id with 
            | -1 -> Cmd.ofMsg (RefreshContactList)
            | x -> Cmd.ofMsg (UpdateContactsWithEditedOrganisationName(model.organisationNameSnapshot,OrganisationName(org.organisationName)))
        |FailureWhileSaving e-> 
            failwith <| sprintf "%s\n%s" e.Message e.StackTrace
            model, Cmd.none
        |UpdateContactsWithEditedOrganisationName(old, newName)-> 
            //this message should be intercepted 1 level up
            failwith <| sprintf "This message should have been caught 1 level up: UpdateContactsWithEditedOrganisationName(%A, %A)" old newName
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





