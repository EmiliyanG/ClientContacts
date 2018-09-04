namespace ClientContacts
module LocationPopup=
    open Elmish
    open Elmish.WPF
    open System
    open SQLTypes
    open ElmishUtils
    open System.Threading
    open MySQLConnection
    open Validator
    open DebugUtils

    type Msg = 
        |ShowPopup of OrganisationName
        |LoadOrganisationsList of OrganisationName
        |UpdateOrganisationsList of Organisation seq * DateTime * OrganisationName
        |LoadOrganisationsFailure of exn
        |ChangeLocation of string
        |Cancel
        |TrySaving
        |SavedSuccessfully
        |FailureWhileSaving of exn
        
    type Model = { organisationsList:Organisation seq
                   loadOrganisationsList: AsyncRequest option
                   validation: string option
                   isOrganisationComboBoxEnabled: bool
                   selectedOrganisationIndex: int
                   IsVisible: bool
                   LocationInput: string
                   }

    let init() = {organisationsList = []
                  loadOrganisationsList= None
                  validation=None
                  selectedOrganisationIndex= -1
                  isOrganisationComboBoxEnabled=false
                  IsVisible=false
                  LocationInput = ""
                  }
    let getOrganisationComboBoxIndexByName (organisations:seq<Organisation> ) (org:OrganisationName) = 
        organisations |> Seq.findIndex( fun organisation-> organisation.organisationName = org.getData)

    let getOrganisationByName (organisations:seq<Organisation> ) (org:OrganisationName) = 
        organisations |> Seq.find( fun organisation-> organisation.organisationName = org.getData)

    let update (msg:Msg) (model:Model) = 
        match msg with
        |ShowPopup org -> 
            {init() with IsVisible= true}, Cmd.ofMsg (LoadOrganisationsList org)
        | LoadOrganisationsList(org) -> 
            
            let d = newDate()
            model.loadOrganisationsList |> cancelRequest 
            let src = new CancellationTokenSource()
            {model with loadOrganisationsList=Some {latestRequest = d; cancelSource = src} }, 
            ofAsync (getOrganisations d) 
                    (src.Token) 
                    (fun (organisationList,t) -> UpdateOrganisationsList (organisationList ,t, org))  //request success
                    (fun exn -> LoadOrganisationsFailure exn) //request failure
        |UpdateOrganisationsList (orgList, d, orgName) -> 
            match model.loadOrganisationsList with 
            | Some r when r.latestRequest = d -> 
                {model with organisationsList = orgList
                            selectedOrganisationIndex = getOrganisationComboBoxIndexByName orgList orgName}, Cmd.none
            | _ -> model, Cmd.none
        |LoadOrganisationsFailure e-> 
            failwith  <| sprintf "Failed loading list of organisations with the following exception: %A" exn
        |Cancel -> 
            {model with IsVisible= false}, Cmd.none
        |TrySaving -> 
            
            match validateLocation model.LocationInput with 
            |Success e-> 

                let org = Seq.item (model.selectedOrganisationIndex) (model.organisationsList)
                let l = {id= -1; locationName=(model.LocationInput); organisationId= org.id}
                debug "started saving"
                {model with validation = None}, 
                Cmd.ofAsync (insertLocation)
                            l
                            (fun a -> SavedSuccessfully)
                            (fun e -> FailureWhileSaving e)
            | Failure msg -> 
                debug msg
                {model with validation = Some msg}, 
                Cmd.none
        |SavedSuccessfully -> 
            {model with IsVisible= false}, 
            Cmd.none
        |FailureWhileSaving e-> 
            failwith <| sprintf "%s\n%s" e.Message e.StackTrace
            model, Cmd.none
        |ChangeLocation v -> 
            debug <| sprintf "changing location %s" v
            {model with LocationInput = v}, Cmd.none

    

    let locationPopupViewBindings: ViewBinding<Model, Msg> list = 
        [ 
            "Cancel" |> Binding.cmd (fun param m -> Cancel)
            "LocationPopupVisibility" |> Binding.oneWay (fun m -> m.IsVisible)
            "LocationText" |> Binding.twoWay (fun m -> m.LocationInput) 
                                             (fun v m -> ChangeLocation <| string v)
            "IsOrganisationComboBoxEnabled" |> Binding.oneWay (fun m -> m.isOrganisationComboBoxEnabled)
            "organisationsSource" |> Binding.oneWay (fun m -> m.organisationsList)
            "SelectedOrganisationIndex" |> Binding.oneWay (fun m-> m.selectedOrganisationIndex)
            "Save" |> Binding.cmd(fun param m-> TrySaving)
            "locationValidations" |> Binding.oneWayMap 
                                                (fun m-> m.validation) 
                                                (fun v -> 
                                                    match v with
                                                    | Some o -> o
                                                    | None -> "")
          ]



