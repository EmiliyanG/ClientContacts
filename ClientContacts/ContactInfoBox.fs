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
        |Organisation
        |Phone
        |Email
        |Comments
    
    type ValidationError = {message: string; fieldName: TextBox}

    type InfoBoxFieldsStatus = {
        validationErrors: ValidationError option
        isTextBoxInEditMode: TextBox option
    }

    type Msg = 
        |LoadContact of int
        |LoadLocationsList of OrganisationId
        |UpdateContactInfo of ContactInfo option * DateTime
        |UpdateContactInfoPhone of string
        |UpdateContactInfoComments of string
        |UpdateContactInfoContactName of string
        |UpdateContactInfoEmail of string
        |UpdateLocationsList of Location seq option * DateTime
        |UpdateLocationComboBoxIndex
        |LoadLocationsFailure
        |LoadContactFailure
        |EnableTextBox of TextBox
        |DisableTextBox of TextBox * DateTime
    [<Literal>]
    let DEFAULT_ORGANISATION_ID = 0

    type Model = { fieldsStatus:InfoBoxFieldsStatus; fieldStatusChanged: DateTime option; loading: bool; loaded:bool; 
                   contactInfo: ContactInfo option
                   loadContactRequest: AsyncRequest option;
                   LoadLocationsList: AsyncRequest option;
                   LocationsList: Location seq option; 
                   LocationComboBoxIndex: int option
                   }

    let init() = { fieldsStatus =
                       {validationErrors= None
                        isTextBoxInEditMode= None
                       };
                   fieldStatusChanged = None;
                   loading= false; loaded=false; 
                   contactInfo = None;
                   loadContactRequest=None; LoadLocationsList=None; LocationsList= None; 
                   LocationComboBoxIndex = None}
    
 
    let getComboBoxIndex (locations:seq<Location> option) locationId = 
        match locations with 
        | Some lseq -> 
            Some(lseq
                |> Seq.findIndex( fun location-> location.id = locationId) )
        | None -> 
            None
    let updateContactInfoField model updateFunc =
        {model with contactInfo = 
                    (match model.contactInfo with 
                    |Some info -> info |> updateFunc
                    |None-> None)}
    
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
                                    Cmd.ofMsg (UpdateLocationComboBoxIndex)
                                | _ ->
                                    Cmd.ofMsg (LoadLocationsList(OrganisationId(info.organisationId)))
                |_ ->
                    {model with loading = false; loaded = true}, Cmd.none
            | _ -> model, Cmd.none

        | LoadContactFailure ->  model, Cmd.none

        | UpdateLocationComboBoxIndex -> 
            {model with LocationComboBoxIndex = 
                        (match model.contactInfo with
                        |Some info -> 
                            match info.locationId with
                            | Some lid -> getComboBoxIndex model.LocationsList lid
                            | None -> None
                        |None -> None)
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
                            LocationComboBoxIndex = 
                                (match model.contactInfo with 
                                |Some info -> 
                                    match info.locationId with
                                    | Some lid -> getComboBoxIndex locationsList lid
                                    | None -> None
                                | None -> None
                                )
                 }, Cmd.none
                
            | _ -> model, Cmd.none
        | LoadLocationsFailure -> 
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
            updateContactInfoField model (fun info -> Some {info with telephone = optionFromString value}), Cmd.none
        |UpdateContactInfoComments(value) -> 
            updateContactInfoField model (fun info -> Some {info with comments = optionFromString value}), Cmd.none
        |UpdateContactInfoContactName(value) -> 
            updateContactInfoField model (fun info -> Some {info with ContactName = value}), Cmd.none
        |UpdateContactInfoEmail(value)->
            
            let isValidEmail str = 
                let emailRegex = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z"
                Regex.IsMatch(str, emailRegex, RegexOptions.IgnoreCase)
            match isValidEmail value with 
            | true -> 
                updateContactInfoField {model with fieldsStatus = {model.fieldsStatus with validationErrors = None}} 
                                       (fun info -> Some {info with email = optionFromString value}), Cmd.none
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
            |"organisation" -> Organisation
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
         "organisation" |> Binding.oneWay (fun m -> getStringFieldFromContactInfo m (fun info -> info.organisationName))
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
         "IsOrganisationReadOnly" |> Binding.oneWayMap (fun m -> m.fieldsStatus.isTextBoxInEditMode) 
                                                       (fun v -> isReadOnly v Organisation)
         "IsLocationComboBoxEnabled" |> Binding.oneWay (fun m -> false )//binds the isEnabled property; not implemented yet
         "IsPhoneReadOnly"|> Binding.oneWayMap (fun m -> m.fieldsStatus.isTextBoxInEditMode ) 
                                               (fun v -> isReadOnly v Phone)
         "IsEmailReadOnly"|> Binding.oneWayMap (fun m -> m.fieldsStatus.isTextBoxInEditMode) 
                                               (fun v -> isReadOnly v Email)
         "AreCommentsReadOnly"|> Binding.oneWayMap (fun m -> m.fieldsStatus.isTextBoxInEditMode) 
                                                   (fun v -> isReadOnly v Comments)
         
         "locationsSource" |> Binding.oneWayMap (fun m -> m.LocationsList) (fun v -> v |> function |Some l -> l |None-> null)      
         "SelectedLocationIndex" |> Binding.oneWayMap (fun m ->  m.LocationComboBoxIndex) (fun v -> intFromOptionOrDefault v -1)
         
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




