namespace ClientContacts

module ContactInfoBox =
    open Elmish
    open Elmish.WPF
    open System
    open MySQLConnection
    open System.Threading
    open SQLTypes
    open ElmishUtils
    open DebugUtils
    
    type InfoBoxMode =
        | ReadOnlyMode
        | EditMode


    type Msg = 
        |LoadContact of int
        |LoadLocationsList of OrganisationId
        |UpdateContactInfo of ContactInfo option * DateTime
        |UpdateLocationsList of Location seq option * DateTime
        |UpdateLocationComboBoxIndex
        |LoadLocationsFailure
        |LoadContactFailure
    [<Literal>]
    let DEFAULT_ORGANISATION_ID = 0

    type Model = { editMode:InfoBoxMode; id: int; loading: bool; loaded:bool; organisation: string; organisationId: int;
                   name: string; phone: string option; locationId: int option;
                   email: string option; comments: string option; 
                   loadContactRequest: AsyncRequest option;
                   LoadLocationsList: AsyncRequest option;
                   IsDisabled: bool; IsAdmin: bool; 
                   LocationsList: Location seq option; 
                   LocationComboBoxIndex: int option
                   }

    let init() = { editMode=ReadOnlyMode; id= 0; loading= false; loaded=false; organisation = ""; organisationId = DEFAULT_ORGANISATION_ID;
                   locationId = None; name = ""; phone = None; email = None;  
                   comments = None; loadContactRequest=None; LoadLocationsList=None;
                   IsDisabled = false; IsAdmin = false; 
                   LocationsList= None; LocationComboBoxIndex = None}
    
 
    let getComboBoxIndex (locations:seq<Location> option) locationId = 
        match locations with 
        | Some lseq -> 
            Some(lseq
                |> Seq.findIndex( fun location-> location.id = locationId) )
        | None -> 
            None

    let update (msg:Msg) (model:Model) = 
        match msg with
        
        | LoadContact s -> 
             let d = newDate()

             //cancel old request
             match model.loadContactRequest with 
             | Some request -> 
                //System.Windows.MessageBox.Show("cancel async") |> ignore
                request.cancelSource.Cancel()
             | _ -> ()

             let src = new CancellationTokenSource()
             {model with loading = true ; loadContactRequest=Some {latestRequest = d; cancelSource = src} }, 
             ofAsync (getContactInfo s d) 
                     (src.Token) 
                     (fun (q,t) -> UpdateContactInfo (q ,t)) //success
                     (fun _ -> LoadContactFailure) //failure

        | UpdateContactInfo (q, d)-> 
            match model.loadContactRequest with 
            | Some r when r.latestRequest = d -> 
            
                match q with 
                |Some info -> 
                    
                    {model with loading = false; loaded = true; name = info.ContactName; 
                                phone = info.telephone; email = info.email;
                                organisation = info.organisationName; 
                                organisationId = info.organisationId;
                                locationId = info.locationId;
                                IsAdmin= info.IsAdmin; IsDisabled=info.IsDisabled
                                comments = info.comments; 
                                loadContactRequest= None
                                }, 
                                match info.organisationId with
                                | s when s = model.organisationId -> 
                                    //no need to query the database if the list of locations for the given organisation is already loaded
                                    //Update the Location ComboBox index only
                                    Cmd.ofMsg (UpdateLocationComboBoxIndex)
                                | _ ->
                                    Cmd.ofMsg (LoadLocationsList(OrganisationId(info.organisationId)))
                |_ ->
                    {model with loading = false; loaded = true}, Cmd.none
            | _ -> model, Cmd.none

        | LoadContactFailure ->  model, Cmd.none

        | UpdateLocationComboBoxIndex -> 
            {model with LocationComboBoxIndex = (match model.locationId with
                                                 | Some lid -> getComboBoxIndex model.LocationsList lid
                                                 | None -> None)
                 }, Cmd.none
        | LoadLocationsList orgId ->
            let d = newDate()
            match model.LoadLocationsList with
            | Some request -> 
                request.cancelSource.Cancel()
            | _-> ()
            let src = new CancellationTokenSource()
            {model with LoadLocationsList=Some {latestRequest = d; cancelSource = src} }, 
            ofAsync (getOrganisationLocations orgId d) 
                    (src.Token) 
                    (fun (locationList,t) -> UpdateLocationsList (locationList ,t))  //request success
                    (fun _ -> LoadLocationsFailure) //request failure
        | UpdateLocationsList (locationsList ,timeStamp) -> 
            match model.LoadLocationsList with 
            | Some request when request.latestRequest = timeStamp -> 
                {model with LoadLocationsList= None; 
                            LocationsList = locationsList; 
                            LocationComboBoxIndex = (match model.locationId with
                                                     | Some lid -> getComboBoxIndex locationsList lid
                                                     | None -> None)
                 }, Cmd.none
                
            | _ -> model, Cmd.none
        | LoadLocationsFailure -> 
            model, Cmd.none
        
            
    let ContactInfoBoxViewBindings: ViewBinding<Model, Msg> list = 
        let stringFromOption opt = 
            match opt with
            | Some o -> o
            | None -> ""
        let intFromOption opt =
            match opt with
            |Some o -> o
            |None -> 0

        ["loading" |> Binding.oneWay (fun m -> m.loading)
         "loaded" |> Binding.oneWay (fun m -> m.loaded)
         "organisation" |> Binding.oneWay (fun m -> m.organisation)
         "location" |> Binding.oneWay (fun m -> intFromOption m.locationId)
         "contactName" |> Binding.oneWay (fun m -> m.name)
         "Comments" |> Binding.oneWay (fun m -> stringFromOption m.comments)
         "phone" |> Binding.oneWay (fun m -> stringFromOption m.phone)
         "email" |> Binding.oneWay (fun m -> stringFromOption m.email)
         "IsDisabled" |> Binding.oneWay (fun m -> m.IsDisabled)
         "IsAdmin" |> Binding.oneWay (fun m -> m.IsAdmin)
         "AreTextBoxesReadOnly" |> Binding.oneWay (fun m -> (match m.editMode with | EditMode -> false | ReadOnlyMode -> true) )
         "IsLocationComboBoxEnabled" |> Binding.oneWay (fun m -> (match m.editMode with | EditMode -> true | ReadOnlyMode -> false) )
         "locationsSource" |> Binding.oneWay (fun m -> match m.LocationsList with 
                                                       | Some l -> l
                                                       | None -> null)
         "SelectedLocationIndex" |> Binding.oneWay (fun m -> match m.LocationComboBoxIndex with |Some index -> index |None -> -1) 
         
        ]




