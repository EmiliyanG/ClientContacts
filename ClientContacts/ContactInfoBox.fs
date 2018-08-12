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
    open Contact
    open System.Windows.Forms
    open System.Windows
    open ContactInfoBoxValidator
    open ContactInfoBoxBindingsManager
    

        
    type LocationComboBox = 
        |LocationComboBox of Location seq option * index: int option
        member this.getLocationsList = 
            match this with 
            |LocationComboBox (l, i) -> l
        member this.getSelectedLocationIndex = 
            match this with 
            |LocationComboBox (l, i) -> i


    type OrganisationComboBox = 
        |OrganisationComboBox of Organisation seq * index: int
        member this.getOrganisationsList = 
            match this with 
            |OrganisationComboBox (l, i) -> l
        member this.getSelectedOrganisationIndex = 
            match this with 
            |OrganisationComboBox (l, i) -> i



    type Msg = 
        |AddNewContact of OrganisationName
        //contact info
        |LoadContact of int
        |UpdateContactInfo of ContactInfo option * DateTime
        |LoadContactFailure
        //organisation combo box
        |LoadOrganisationsList of OrganisationName option
        |UpdateOrganisationsList of Organisation seq * DateTime * OrganisationName option
        |LoadOrganisationsFailure
        |UpdateOrganisationComboBoxIndex of int
        //location combo box
        |LoadLocationsList of OrganisationId
        |UpdateLocationsList of Location seq option * DateTime
        |LoadLocationsFailure
        |UpdateLocationComboBoxIndex of int option
        //text box updates
        |UpdateContactInfoPhone of string
        |UpdateContactInfoComments of string
        |UpdateContactInfoContactName of string
        |UpdateContactInfoEmail of string
        |UpdateIsAdmin
        |UpdateIsDisabled
        |EnterEditMode
        |CancelChanges 
        |SaveContactInfoChanges
        |SaveContactInfoChangesFailure
        |SaveContactInfoChangesSuccess
    [<Literal>]
    let DEFAULT_ORGANISATION_ID = 0

    type Model = { mode: ContactInfoBoxMode
                   validationErrors: ValidationError list option
                   loading: bool; loaded:bool
                   contactInfo: ContactInfo option
                   contactInfoSnapshot: ContactInfo option
                   loadContactRequest: AsyncRequest option
                   LoadLocationsList: AsyncRequest option
                   loadOrganisationsList: AsyncRequest option
                   locationComboBox: LocationComboBox
                   organisationComboBox: OrganisationComboBox
                   locationComboBoxSnapshot: LocationComboBox
                   organisationComboBoxSnapshot: OrganisationComboBox
                   }

    let init() = { mode=ReadOnlyMode
                   validationErrors= None
                   loading= false; loaded=false;
                   contactInfo= None
                   contactInfoSnapshot= None
                   loadContactRequest=None; LoadLocationsList=None; loadOrganisationsList=None 
                   locationComboBox=LocationComboBox (None, None)
                   organisationComboBox=OrganisationComboBox([], -1)
                   locationComboBoxSnapshot=LocationComboBox (None, None)
                   organisationComboBoxSnapshot=OrganisationComboBox([], -1)
                   }
    
 
    let getLocationComboBoxIndex (locations:seq<Location> option) locationId = 
        match locations with 
        | Some lseq -> 
            Some(lseq
                |> Seq.findIndex( fun location-> location.id = locationId) )
        | None -> 
            None

    let getOrganisationComboBoxIndexById (organisations:seq<Organisation> ) organisationId = 
        organisations |> Seq.findIndex( fun organisation-> organisation.id = organisationId)
    
    let getOrganisationComboBoxIndexByName (organisations:seq<Organisation> ) (org:OrganisationName) = 
        organisations |> Seq.findIndex( fun organisation-> organisation.organisationName = org.getData)

    let getComboBoxItemAtIndex (items:seq<'A> option) index = 
        items  
        |> Option.bind(fun i-> i |> Seq.item index |> Some)

    

    let updateContactInfoField model updateFunc =
        {model with contactInfo = (model.contactInfo 
                                   |> Option.bind(fun info-> info |> updateFunc|> Some))}
    
    ///return some(string) if str is not empty
    let optionFromString str = 
        match str with 
        |"" | null -> None
        |s-> Some(s)
    
    ///validate ContactInfo text field user input
    /// <returns> New model with updated validation errors based on the specified field </returns>
    let validateContactInfoTextField model field value = 
        match value |> getValidationFunction field with
        |Success e -> removeValidationError model.validationErrors field
        |Failure err -> addValidationError model.validationErrors 
                            {message=err; fieldName=field}
        |> fun e -> {model with validationErrors = e}
        
        
    let allowedToLoadNewContact mode = 
        match mode with 
        |EditMode -> 
        MessageBox.Show("Would you like to discard any unsaved changes?",
                        "Client Contacts",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question,
                        MessageBoxResult.No)
        |> (fun result -> match result with 
                            |MessageBoxResult.Yes -> true
                            |_ -> false) 
                        
        |ReadOnlyMode -> true



    let update (msg:Msg) (model:Model) = 
        match msg with
        |AddNewContact org -> 
            match allowedToLoadNewContact model.mode with 
            |true -> 
                {model with 
                    contactInfo = Some({id = -1 
                                        ContactName=""
                                        IsDisabled= false
                                        IsAdmin= false
                                        email= None 
                                        telephone= None
                                        organisationId= -1
                                        locationId=None 
                                        comments=None})
                    loading = false; loaded = true;
                    mode = EditMode},
                Cmd.ofMsg(LoadOrganisationsList (Some(org)))

            |false -> model, Cmd.none
        | LoadContact s -> 
             
             match allowedToLoadNewContact model.mode with 
             |true -> 
                 let d = newDate()

                 //cancel old request
                 match model.loadContactRequest with 
                 | Some request -> 
                    //System.Windows.MessageBox.Show("cancel async") |> ignore
                    request.cancelSource.Cancel()
                 | _ -> ()

                 let src = new CancellationTokenSource()
                 {model with loading = true ; 
                             loadContactRequest=Some {latestRequest = d; cancelSource = src}
                             mode = ReadOnlyMode}, 
                 ofAsync (getContactInfo s d) 
                         (src.Token) 
                         (fun (q,t) -> UpdateContactInfo (q ,t)) //success
                         (fun _ -> LoadContactFailure) //failure
             |false -> model, Cmd.none

        | UpdateContactInfo (q, d)-> 
            match model.loadContactRequest with 
            | Some r when r.latestRequest = d -> 
            
                match q with 
                |Some info -> 
                    
                    {model with loading = false; loaded = true; 
                                contactInfo = Some info
                                loadContactRequest= None
                                }, 
                                match info.organisationId, model.contactInfo  with
                                | orgId, Some info when orgId = info.organisationId -> 
                                    //no need to query the database if the list of locations for the given organisation is already loaded
                                    //Update the Location ComboBox index only
                                    Cmd.ofMsg (UpdateLocationComboBoxIndex(model.contactInfo
                                                  |> Option.bind (fun info-> info.locationId 
                                                                             |> Option.bind (fun lid -> getLocationComboBoxIndex model.locationComboBox.getLocationsList lid)))
                                    )
                                | _ ->
                                    Cmd.ofMsg (LoadOrganisationsList(None))
                |_ ->
                    {model with loading = false; loaded = true}, Cmd.none
            | _ -> model, Cmd.none

        | UpdateLocationComboBoxIndex (index)-> 
            {model with locationComboBox = 
                        LocationComboBox( model.locationComboBox.getLocationsList,index)}, Cmd.none
        | UpdateOrganisationComboBoxIndex (index)-> 

            let org = Seq.item index model.organisationComboBox.getOrganisationsList
            {model with organisationComboBox = OrganisationComboBox( model.organisationComboBox.getOrganisationsList,index);
                        contactInfo= 
                                    (model.contactInfo 
                                    |> Option.bind(
                                        fun info-> 
                                            Some {info with organisationId=org.id; 
                                                            locationId=None})
                                    )
                        
                        }, 
                        
                        Cmd.ofMsg (LoadLocationsList(OrganisationId(org.id)))
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
                    (fun _ -> LoadOrganisationsFailure) //request failure
        
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
                            locationComboBox= 
                                LocationComboBox(locationsList,
                                    
                                    model.contactInfo 
                                    |> Option.bind(fun info->  
                                        info.locationId |> 
                                        Option.bind(fun lid -> getLocationComboBoxIndex locationsList lid)
                                    )
                                )
                 }, Cmd.none
                
            | _ -> model, Cmd.none
        |UpdateOrganisationsList(organisationlist, timeStamp, org)-> 
            match model.loadOrganisationsList with 
            | Some request when request.latestRequest = timeStamp -> 
                

                let orgIndexFromList = Option.map (fun v -> getOrganisationComboBoxIndexByName organisationlist v) org
                
                {model with LoadLocationsList= None; 
                            organisationComboBox= 
                                OrganisationComboBox(organisationlist,
                                    (match model.contactInfo, orgIndexFromList with 
                                    |Some info, None-> getOrganisationComboBoxIndexById organisationlist info.organisationId
                                    |Some info, Some index -> index
                                    |_ -> -1))
                            contactInfo = model.contactInfo 
                                          |> Option.bind (fun (info:ContactInfo) -> 
                                                              Some({info with 
                                                                         organisationId = match orgIndexFromList with 
                                                                                          |Some index -> Seq.item index organisationlist
                                                                                                         |> fun v -> v.id
                                                                                          |None -> -1 }) ) 
                 }, match model.contactInfo with 
                    | Some info -> Cmd.ofMsg (LoadLocationsList(OrganisationId(info.organisationId)))
                    | None -> Cmd.none
            | _ -> model, Cmd.none

        | LoadLocationsFailure |LoadOrganisationsFailure |LoadContactFailure -> 
            model, Cmd.none
        | EnterEditMode->
            {model with mode=EditMode
                        contactInfoSnapshot=model.contactInfo
                        locationComboBoxSnapshot=model.locationComboBox
                        organisationComboBoxSnapshot=model.organisationComboBox}, Cmd.none
        | CancelChanges-> 
            {model with mode=ReadOnlyMode
                        contactInfo=model.contactInfoSnapshot
                        validationErrors=None
                        locationComboBox=model.locationComboBoxSnapshot
                        organisationComboBox=model.organisationComboBoxSnapshot}, Cmd.none
        | SaveContactInfoChanges-> 
            
            let name = Option.map (fun (info:ContactInfo)-> info.ContactName) model.contactInfo
                       |> stringFromOption

            let m = validateContactInfoTextField model ContactName name
            
            match m.validationErrors with 
            |Some v -> m, Cmd.none
            |None-> 
                {m with mode=ReadOnlyMode}, 
                match m.contactInfo with 
                |Some info -> 
                    Cmd.ofAsync (updateContactInfo)
                                info
                                (fun param -> 
                                     match param with 
                                     |1 -> SaveContactInfoChangesSuccess
                                     |_ ->failwith "Could not update or insert a contact in the database"
                                          SaveContactInfoChangesFailure)
                                (fun exn -> SaveContactInfoChangesFailure)
                |None -> Cmd.none
        |SaveContactInfoChangesFailure |SaveContactInfoChangesSuccess -> model, Cmd.none
        |UpdateContactInfoPhone(value) ->
            validateContactInfoTextField model Phone value
            |>  updateContactInfoField <| (fun info -> {info with telephone = optionFromString value}), Cmd.none
        |UpdateContactInfoComments(value) -> 
            validateContactInfoTextField model Comments value
            |> updateContactInfoField <| (fun info -> {info with comments = optionFromString value}), Cmd.none
        |UpdateContactInfoContactName(value) -> 
            validateContactInfoTextField model ContactName value
            |> updateContactInfoField <| (fun info -> {info with ContactName = value}), Cmd.none
        |UpdateIsAdmin -> 
            updateContactInfoField model (fun info -> {info with IsAdmin = not info.IsAdmin}), Cmd.none
        |UpdateIsDisabled -> 
            updateContactInfoField model (fun info -> {info with IsDisabled = not info.IsDisabled}), Cmd.none
        |UpdateContactInfoEmail(value)->
            validateContactInfoTextField model Email value
            |> updateContactInfoField <| (fun info -> {info with email = optionFromString value})
            , Cmd.none

    let ContactInfoBoxViewBindings: ViewBinding<Model, Msg> list = 
        
        let getTextBox = 
            function
            |"contactName" -> ContactName
            |"phone" -> Phone
            |"email" -> Email
            |"comments" -> Comments
            |a -> failwith <| sprintf "Could not match binding to TextField. Binding: %s" a
        
        let isReadOnlyBinding = 
            Binding.oneWayMap (fun m -> m.mode ) 
                              (fun v -> isReadOnly v)
        
        let isEnabledBinding = 
            Binding.oneWayMap (fun m -> m.mode )
                              (fun mode -> isEnabled mode )
        
        let textFieldValidationVisibleBinding textField = 
            Binding.oneWayMap (fun m -> m.validationErrors) 
                              (fun errors -> isValidationMessageVisible textField errors)
        
        let textFieldValidationTextBinding textField = 
            Binding.oneWayMap (fun m -> m.validationErrors) 
                              (fun errors -> getValidationErrorMessageForTextBox textField errors)
        
        let getStringOptionFieldFromContactInfoBinding msg func =
            Binding.twoWay (fun m -> getStringOptionFieldFromContactInfo m.contactInfo func) //getter
                           (fun v m -> msg(v))
        let bindingCmd msg = 
            Binding.cmd (fun param m -> msg)
        ["loading" |> Binding.oneWay (fun m -> m.loading)
         "loaded" |> Binding.oneWay (fun m -> m.loaded)
         "location" |> Binding.oneWayMap (fun m -> m.contactInfo)
                                         (fun info-> getFieldFromContactInfoOption info 0 (fun i -> intFromOptionOrDefault i.locationId 0))
         "contactName" |> Binding.twoWay (fun m -> getStringFieldFromContactInfo m.contactInfo (fun info -> info.ContactName))//getter
                                         (fun v m-> UpdateContactInfoContactName(v))//setter
         "Comments" |> getStringOptionFieldFromContactInfoBinding UpdateContactInfoComments (fun info -> info.comments)
         "phone" |> getStringOptionFieldFromContactInfoBinding UpdateContactInfoPhone (fun info -> info.telephone)
         "email" |> getStringOptionFieldFromContactInfoBinding UpdateContactInfoEmail (fun info -> info.email)
         "IsDisabled" |> Binding.oneWayMap (fun m -> m.contactInfo, m.mode) 
                                           (fun (info,mode)-> getFieldFromContactInfoOption info false (fun i -> i.IsDisabled)
                                                              |> getImageBtnVisibility mode )
         "IsDisabledOpacity" |> Binding.oneWayMap(fun m -> m.contactInfo, m.mode)
                                                 (fun (info,mode)-> getFieldFromContactInfoOption info false (fun i -> i.IsDisabled)
                                                                    |> getImageBtnOpacity mode)
         "IsAdmin" |> Binding.oneWayMap (fun m -> m.contactInfo, m.mode) 
                                        (fun (info,mode) -> getFieldFromContactInfoOption info false (fun i -> i.IsAdmin)
                                                            |> getImageBtnVisibility mode)
         "IsDisabledChanged" |> bindingCmd UpdateIsDisabled
         "IsAdminChanged" |> bindingCmd UpdateIsAdmin
         "IsAdminOpacity"|> Binding.oneWayMap(fun m -> m.contactInfo, m.mode)
                                             (fun (info,mode)-> getFieldFromContactInfoOption info false (fun i -> i.IsAdmin)
                                                                |> getImageBtnOpacity mode)
         

         //are fields enabled or read-only
         "IsContactNameReadOnly" |> isReadOnlyBinding
         "IsOrganisationComboBoxEnabled" |> isEnabledBinding
         "IsLocationComboBoxEnabled" |> isEnabledBinding
         "IsPhoneReadOnly"|> isReadOnlyBinding
         "IsEmailReadOnly"|> isReadOnlyBinding
         "AreCommentsReadOnly"|> isReadOnlyBinding
         "IsAdminBtnEnabled"|> isEnabledBinding
         "IsDisabledBtnEnabled"|> isEnabledBinding
         "locationsSource" |> Binding.oneWayMap (fun m -> m.locationComboBox.getLocationsList) (fun v -> v |> function |Some l -> l |None-> null)      
         "SelectedLocationIndex" |> Binding.twoWay (fun m ->  m.locationComboBox.getSelectedLocationIndex |> intFromOptionOrDefault <| -1) //getter
                                                   (fun index m-> UpdateLocationComboBoxIndex(Some(int index))) //setter
         
         "organisationsSource" |> Binding.oneWay (fun m -> m.organisationComboBox.getOrganisationsList)
         
         "SelectedOrganisationIndex" |> Binding.twoWay (fun m ->  m.organisationComboBox.getSelectedOrganisationIndex) //getter 
                                                       (fun index m -> UpdateOrganisationComboBoxIndex(int index)) //setter
         //change modes
         "EditContact" |> bindingCmd EnterEditMode
         "CancelChanges"|> bindingCmd CancelChanges
         "SaveChanges" |> bindingCmd SaveContactInfoChanges
         //TextBox validations
         "ContactNameValidationsText" |> textFieldValidationTextBinding ContactName 
         "PhoneValidationsText" |> textFieldValidationTextBinding Phone
         "EmailValidationsText" |> textFieldValidationTextBinding Email
         "CommentsValidationsText" |> textFieldValidationTextBinding Comments
         //Validations visibility
         "ContactNameValidationsVisible" |> textFieldValidationVisibleBinding ContactName
         "PhoneValidationsVisible" |> textFieldValidationVisibleBinding Phone
         "EmailValidationsVisible" |> textFieldValidationVisibleBinding Email
         "CommentsValidationsVisible" |> textFieldValidationVisibleBinding Comments
         //Action Buttons visibility
         "EditContactButtonVisible" |> Binding.oneWayMap (fun m -> m.mode) (fun mode -> isActionButtonVisible mode)

        ]




