namespace ClientContacts
module LocationPopup=
    open Elmish
    open Elmish.WPF
    open System
    open SQLTypes
    open ElmishUtils
    open System.Threading
    open MySQLConnection

    type Msg = 
        |ShowPopup of OrganisationName
        |LoadOrganisationsList of OrganisationName
        |UpdateOrganisationsList of Organisation seq * DateTime * OrganisationName
        |LoadOrganisationsFailure of exn
        |ChangeLocation of string
        |Cancel
        
    type Model = { organisationsList:Organisation seq
                   loadOrganisationsList: AsyncRequest option
                   isOrganisationComboBoxEnabled: bool
                   selectedOrganisationIndex: int
                   locationField:string option
                   IsVisible: bool
                   LocationInput: string
                   }

    let init() = {organisationsList = []
                  loadOrganisationsList= None
                  selectedOrganisationIndex= -1
                  isOrganisationComboBoxEnabled=true
                  locationField = None
                  IsVisible=false
                  LocationInput = ""
                  }
    let getOrganisationComboBoxIndexByName (organisations:seq<Organisation> ) (org:OrganisationName) = 
        organisations |> Seq.findIndex( fun organisation-> organisation.organisationName = org.getData)

    let update (msg:Msg) (model:Model) = 
        match msg with
        |ShowPopup org -> 
            {init() with IsVisible= true}, Cmd.ofMsg (LoadOrganisationsList org)
        | LoadOrganisationsList(org) -> 
            
            let d = newDate()
            match model.loadOrganisationsList with
            | Some request -> 
                request.cancelSource.Cancel()
            | _-> ()
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
        |ChangeLocation v -> 
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
          ]



