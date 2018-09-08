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

    type Mode = 
        |EditLocation
        |AddNewLocation

    type Msg = 
        |EditExistingLocation of Location
        |AddNewLocation of Organisation
        |LoadOrganisationsList of Organisation
        |UpdateOrganisationsList of Organisation seq * DateTime * Organisation
        |LoadOrganisationsFailure of exn
        |ChangeLocation of string
        |Cancel
        |TrySaving
        |SavedSuccessfully of Location
        |FailureWhileSaving of exn
        |UpdateContactsWithEditedLocationName of Location
        
    type Model = { 
                   mode:Mode
                   organisationsList:Organisation seq
                   loadOrganisationsList: AsyncRequest option
                   validation: string option
                   isOrganisationComboBoxEnabled: bool
                   selectedOrganisationIndex: int
                   IsVisible: bool
                   LocationNameSnapshot: string
                   LocationInput:Location
                   }

    let init() = {
                  mode=Mode.AddNewLocation
                  organisationsList = []
                  loadOrganisationsList= None
                  validation=None
                  selectedOrganisationIndex= -1
                  isOrganisationComboBoxEnabled=false
                  IsVisible=false
                  LocationNameSnapshot = ""
                  LocationInput={id= -1; locationName=""; organisationId= -1}
                  }
    let getOrganisationComboBoxIndexByName (organisations:seq<Organisation> ) (org:Organisation) = 
        organisations |> Seq.findIndex( fun organisation-> organisation.id = org.id)

    let getOrganisationByName (organisations:seq<Organisation> ) (org:OrganisationName) = 
        organisations |> Seq.find( fun organisation-> organisation.organisationName = org.getData)

    let update (msg:Msg) (model:Model) = 
        match msg with
        |EditExistingLocation l -> 
            let org = {id = l.organisationId; organisationName=""}
            {model with 
                IsVisible = true
                mode = Mode.EditLocation
                LocationInput = l
                LocationNameSnapshot = l.locationName
            }, Cmd.ofMsg (LoadOrganisationsList (org))
        |AddNewLocation org -> 
            let m = init()
            {m with 
                    IsVisible= true
                    mode=Mode.AddNewLocation
                    LocationInput = {m.LocationInput with organisationId = org.id}
            }, Cmd.ofMsg (LoadOrganisationsList org)
        | LoadOrganisationsList(org) -> 
            
            let d = newDate()
            model.loadOrganisationsList |> cancelRequest 
            let src = new CancellationTokenSource()
            {model with loadOrganisationsList=Some {latestRequest = d; cancelSource = src} }, 
            ofAsync (getOrganisations d) 
                    (src.Token) 
                    (fun (organisationList,t) -> UpdateOrganisationsList (organisationList ,t, org))  //request success
                    (fun exn -> LoadOrganisationsFailure exn) //request failure
        |UpdateOrganisationsList (orgList, d, org) -> 
            match model.loadOrganisationsList with 
            | Some r when r.latestRequest = d -> 
                {model with organisationsList = orgList
                            selectedOrganisationIndex = getOrganisationComboBoxIndexByName orgList org}, Cmd.none
            | _ -> model, Cmd.none
        |LoadOrganisationsFailure e-> 
            failwith  <| sprintf "Failed loading list of organisations with the following exception: %A" exn
        |Cancel -> 
            {model with IsVisible= false}, Cmd.none
        |TrySaving -> 
            
            match validateLocation model.LocationInput.locationName with 
            |Success e-> 

                {model with validation = None}, 
                Cmd.ofAsync (updateOrInsertLocation)
                            model.LocationInput
                            (fun a -> SavedSuccessfully(model.LocationInput))
                            (fun e -> FailureWhileSaving e)
            | Failure msg -> 
                {model with validation = Some msg}, 
                Cmd.none
        |SavedSuccessfully l -> 
            {model with IsVisible= false}, 
            match l.id with 
            | -1 -> Cmd.none
            | x -> Cmd.ofMsg (UpdateContactsWithEditedLocationName(l))
        |FailureWhileSaving e-> 
            failwith <| sprintf "%s\n%s" e.Message e.StackTrace
            model, Cmd.none
        |ChangeLocation v -> 
            {model with LocationInput = {model.LocationInput with locationName=v} }, Cmd.none
        |UpdateContactsWithEditedLocationName l -> 
            failwith <| sprintf "this message should have been caught 1 level up: UpdateContactsWithEditedLocationName(%A)" l
            model, Cmd.none
    

    let locationPopupViewBindings: ViewBinding<Model, Msg> list = 
        [ 
            "Cancel" |> Binding.cmd (fun param m -> Cancel)
            "LocationPopupVisibility" |> Binding.oneWay (fun m -> m.IsVisible)
            "LocationText" |> Binding.twoWay (fun m -> m.LocationInput.locationName) 
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



