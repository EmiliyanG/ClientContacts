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
    open System.Text.RegularExpressions
    open Contact
    open System.Windows.Forms
    open System.Windows
    

    type TextBox = 
        |ContactName
        |Phone
        |Email
        |Comments
    
    type ValidationError = {message: string; fieldName: TextBox}


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





    type InfoBoxFieldsStatus = {
        validationErrors: ValidationError option
        isTextBoxInEditMode: TextBox option
    }

    type Msg = 
        //contact info
        |LoadContact of int
        |UpdateContactInfo of ContactInfo option * DateTime
        |LoadContactFailure
        //organisation combo box
        |LoadOrganisationsList
        |UpdateOrganisationsList of Organisation seq * DateTime
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
        |EnableTextBox of TextBox
        |DisableTextBox of TextBox * DateTime
    [<Literal>]
    let DEFAULT_ORGANISATION_ID = 0

    type Model = { fieldsStatus:InfoBoxFieldsStatus; fieldStatusChanged: DateTime option; loading: bool; loaded:bool
                   contactInfo: ContactInfo option
                   loadContactRequest: AsyncRequest option
                   LoadLocationsList: AsyncRequest option
                   loadOrganisationsList: AsyncRequest option
                   locationComboBox: LocationComboBox
                   organisationComboBox: OrganisationComboBox
                   }

    let init() = { fieldsStatus =
                       {validationErrors= None
                        isTextBoxInEditMode= None
                       };
                   fieldStatusChanged = None
                   loading= false; loaded=false;
                   contactInfo = None
                   loadContactRequest=None; LoadLocationsList=None; loadOrganisationsList=None 
                   locationComboBox=LocationComboBox (None, None)
                   organisationComboBox=OrganisationComboBox([], -1)
                   }
    
 
    let getLocationComboBoxIndex (locations:seq<Location> option) locationId = 
        match locations with 
        | Some lseq -> 
            Some(lseq
                |> Seq.findIndex( fun location-> location.id = locationId) )
        | None -> 
            None

    let getOrganisationComboBoxIndex (organisations:seq<Organisation> ) organisationId = 
        organisations |> Seq.findIndex( fun organisation-> organisation.id = organisationId)


    let getComboBoxItemAtIndex (items:seq<'A> option) index = 
        items  
        |> Option.bind(fun i-> i |> Seq.item index |> Some)

    

    let updateContactInfoField model updateFunc =
        {model with contactInfo = (model.contactInfo 
                                   |> Option.bind(fun info-> info |> updateFunc|> Some))}
    
    ///return some(string) if str is not empty
    let optionFromString str = 
        match str with 
        |"" -> None
        |s-> Some(s)

    let update (msg:Msg) (model:Model) = 
        match msg with
        
        | LoadContact s -> 
             let allowedToLoadNewContact = 
                 match model.fieldsStatus.validationErrors with 
                 |Some e -> 
                    MessageBox.Show("Would you like to discard any unsaved changes?",
                                    "Client Contacts",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question,
                                    MessageBoxResult.No)
                    |> (fun result -> match result with 
                                      |MessageBoxResult.Yes -> true
                                      |_ -> false) 
                        
                 |None -> true
             
             match allowedToLoadNewContact with 
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
                             fieldsStatus = {model.fieldsStatus with isTextBoxInEditMode= None
                                                                     validationErrors=None}}, 
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
                                    Cmd.ofMsg (LoadOrganisationsList)
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
                                                            organisationName = org.organisationName
                                                            locationId=None})
                                    )
                        
                        }, 
                        
                        Cmd.ofMsg (LoadLocationsList(OrganisationId(org.id)))
        | LoadOrganisationsList -> 
            let d = newDate()
            match model.loadOrganisationsList with
            | Some request -> 
                request.cancelSource.Cancel()
            | _-> ()
            let src = new CancellationTokenSource()
            {model with loadOrganisationsList=Some {latestRequest = d; cancelSource = src} }, 
            ofAsync (getOrganisations d) 
                    (src.Token) 
                    (fun (organisationList,t) -> UpdateOrganisationsList (organisationList ,t))  //request success
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
        |UpdateOrganisationsList(organisationlist, timeStamp)-> 
            match model.loadOrganisationsList with 
            | Some request when request.latestRequest = timeStamp -> 
                {model with LoadLocationsList= None; 
                            organisationComboBox= 
                                OrganisationComboBox(organisationlist,
                                    (match model.contactInfo with 
                                    |Some info-> getOrganisationComboBoxIndex organisationlist info.organisationId
                                    | None -> -1))
                 }, match model.contactInfo with 
                    | Some info -> Cmd.ofMsg (LoadLocationsList(OrganisationId(info.organisationId)))
                    | None -> Cmd.none
            | _ -> model, Cmd.none

        | LoadLocationsFailure |LoadOrganisationsFailure |LoadContactFailure -> 
            model, Cmd.none
        |EnableTextBox(textBoxName) -> 
            //do not enable new textbox if another textbox is enabled and validation errors exist
            let allowedToEnableTextBox = 
                match model.fieldsStatus.validationErrors with
                |Some _ -> false
                |_ -> true
            
            match allowedToEnableTextBox with 
            |true -> 
                
                {model with fieldsStatus = {model.fieldsStatus with isTextBoxInEditMode = Some textBoxName} 
                            fieldStatusChanged = Some(newDate())}, 
                match model.fieldsStatus.isTextBoxInEditMode with 
                |Some tb -> Cmd.ofMsg(DisableTextBox(tb,newDate()))
                |None -> Cmd.none
                
            |false-> model,Cmd.none
        |DisableTextBox(textBoxName, newDate)->
            //allowed to disable textbox only if it was enabled more than 100 ms ago
            let allowedToDisableTextBox = 
                match model.fieldStatusChanged, model.fieldsStatus.validationErrors with 
                |Some oldDate,None -> 
                    (newDate - oldDate).Duration() > TimeSpan.FromMilliseconds(float 200)
                |_ -> false

            match allowedToDisableTextBox with 
            |true ->
                {model with fieldsStatus = {model.fieldsStatus with isTextBoxInEditMode=None}}, Cmd.none
            |false -> model, Cmd.none
        |UpdateContactInfoPhone(value) ->
            updateContactInfoField model (fun info -> {info with telephone = optionFromString value}), Cmd.none
        |UpdateContactInfoComments(value) -> 
            updateContactInfoField model (fun info -> {info with comments = optionFromString value}), Cmd.none
        |UpdateContactInfoContactName(value) -> 
            updateContactInfoField model (fun info -> {info with ContactName = value}), Cmd.none
        |UpdateContactInfoEmail(value)->
            
            let isValidEmail str = 
                let emailRegex = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z"
                Regex.IsMatch(str, emailRegex, RegexOptions.IgnoreCase)
            match isValidEmail value with 
            | true -> 
                updateContactInfoField {model with fieldsStatus = {model.fieldsStatus with validationErrors = None}} 
                                       (fun info -> {info with email = optionFromString value}), Cmd.none
            | false -> {model 
                            with fieldsStatus = {model.fieldsStatus 
                                                    with validationErrors = Some {message="Email is not valid"; fieldName=Email} }}, Cmd.none

    let ContactInfoBoxViewBindings: ViewBinding<Model, Msg> list = 
        let stringFromOption opt =
            match opt with
            | Some o -> o
            | None -> ""
        
        ///get field from Some ContactInfo or return defaultValue if None
        let getFieldFromContactInfoOption opt (defaultValue) f = 
            match opt with
            |Some opt -> f opt
            |None -> defaultValue
        ///get string field from Some ContactInfo or return "" if None
        let getStringFieldFromContactInfo m=
            getFieldFromContactInfoOption m.contactInfo ""
        
        ///get string option field from ContactInfo and pass the result to the stringFromOption function
        let getStringOptionFieldFromContactInfo m=
            getFieldFromContactInfoOption m.contactInfo None
            >> stringFromOption
        
        let intFromOptionOrDefault opt (returnIfNone:int)=
            match opt with
            |Some o -> o
            |None -> returnIfNone

        let isReadOnly isTextBoxReadOnly expected = 
            match isTextBoxReadOnly with 
            |Some tb when tb = expected -> false
            |_ -> true


        let getTextBox = 
            function
            |"contactName" -> ContactName
            |"phone" -> Phone
            |"email" -> Email
            |"comments" -> Comments
            |a -> failwith <| sprintf "Could not match binding to TextField. Binding: %s" a
        
        let getValidationMessage field err=
            match err with 
            |Some er -> 
                match field, er with 
                |f, vr when f = vr.fieldName -> vr.message
                |_, _ -> ""
            |None -> ""
        
        let displayValidationMessage field err=
            match err with 
            |Some er -> 
                match field, er with 
                |f, vr when f = vr.fieldName -> true
                |_, _ -> false
            |None -> false
        

        ["loading" |> Binding.oneWay (fun m -> m.loading)
         "loaded" |> Binding.oneWay (fun m -> m.loaded)
         "location" |> Binding.oneWayMap (fun m -> m.contactInfo)
                                         (fun info-> getFieldFromContactInfoOption info 0 (fun i -> intFromOptionOrDefault i.locationId 0))
         "contactName" |> Binding.twoWay (fun m -> getStringFieldFromContactInfo m (fun info -> info.ContactName))//getter
                                         (fun v m-> UpdateContactInfoContactName(v))//setter
         "Comments" |> Binding.twoWay (fun m -> getStringOptionFieldFromContactInfo m (fun info -> info.comments)) //getter
                                      (fun v m -> UpdateContactInfoComments(v))//setter
         "phone" |> Binding.twoWay (fun m -> getStringOptionFieldFromContactInfo m  (fun info -> info.telephone)) //getter
                                   (fun v m -> UpdateContactInfoPhone(v)) //setter
         
         "email" |> Binding.twoWay (fun m -> getStringOptionFieldFromContactInfo m (fun info -> info.email))//getter
                                             (fun v m -> UpdateContactInfoEmail v )//setter with validation
         "IsDisabled" |> Binding.oneWayMap (fun m -> m.contactInfo) 
                                           (fun info-> getFieldFromContactInfoOption info false (fun i -> i.IsDisabled))
         "IsAdmin" |> Binding.oneWayMap (fun m -> m.contactInfo) 
                                        (fun info ->  getFieldFromContactInfoOption info false (fun i -> i.IsAdmin))
         
         //are fields enabled or read-only
         "IsContactNameReadOnly" |> Binding.oneWayMap (fun m -> m.fieldsStatus.isTextBoxInEditMode) 
                                                      (fun v -> isReadOnly v ContactName)
         
         
         "IsOrganisationComboBoxEnabled" |> Binding.oneWay (fun m -> true )
         "IsLocationComboBoxEnabled" |> Binding.oneWay (fun m -> true )//binds the isEnabled property; not implemented yet
         "IsPhoneReadOnly"|> Binding.oneWayMap (fun m -> m.fieldsStatus.isTextBoxInEditMode ) 
                                               (fun v -> isReadOnly v Phone)
         "IsEmailReadOnly"|> Binding.oneWayMap (fun m -> m.fieldsStatus.isTextBoxInEditMode) 
                                               (fun v -> isReadOnly v Email)
         "AreCommentsReadOnly"|> Binding.oneWayMap (fun m -> m.fieldsStatus.isTextBoxInEditMode) 
                                                   (fun v -> isReadOnly v Comments)
         
         "locationsSource" |> Binding.oneWayMap (fun m -> m.locationComboBox.getLocationsList) (fun v -> v |> function |Some l -> l |None-> null)      
         "SelectedLocationIndex" |> Binding.twoWay (fun m ->  m.locationComboBox.getSelectedLocationIndex |> intFromOptionOrDefault <| -1) //getter
                                                   (fun index m-> UpdateLocationComboBoxIndex(Some(int index))) //setter
         
         "organisationsSource" |> Binding.oneWay (fun m -> m.organisationComboBox.getOrganisationsList)
         
         "SelectedOrganisationIndex" |> Binding.twoWay (fun m ->  m.organisationComboBox.getSelectedOrganisationIndex) //getter 
                                                       (fun index m -> UpdateOrganisationComboBoxIndex(int index)) //setter
         //change textBox modes
         "EnableTextBox" |> Binding.cmd (fun param m ->  string param |> getTextBox |> EnableTextBox)
         "DisableTextBox" |> Binding.cmd (fun param m -> (string param |> getTextBox , newDate()) |> DisableTextBox)
         
         //TextBox validations
         "ContactNameValidationsText" |> Binding.oneWayMap (fun m -> m.fieldsStatus.validationErrors) 
                                                           (fun errors -> getValidationMessage ContactName errors)
         "PhoneValidationsText" |> Binding.oneWayMap (fun m -> m.fieldsStatus.validationErrors) 
                                                     (fun errors -> getValidationMessage Phone errors)
         "EmailValidationsText" |> Binding.oneWayMap (fun m -> m.fieldsStatus.validationErrors) 
                                                     (fun errors -> getValidationMessage Email errors)

         //Validations visibility
         "ContactNameValidationsVisible" |> Binding.oneWayMap (fun m -> m.fieldsStatus.validationErrors) 
                                                              (fun errors -> displayValidationMessage ContactName errors)
         "PhoneValidationsVisible" |> Binding.oneWayMap (fun m -> m.fieldsStatus.validationErrors) 
                                                        (fun errors -> displayValidationMessage Phone errors)
         "EmailValidationsVisible" |> Binding.oneWayMap (fun m -> m.fieldsStatus.validationErrors) 
                                                        (fun errors -> displayValidationMessage Email errors)

        ]




