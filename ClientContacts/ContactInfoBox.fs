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
    
    type TextBoxModes =
        | ReadOnlyMode
        | EditMode
    type InfoBoxFieldsStatus = {
        contactName: TextBoxModes
        organisation: TextBoxModes
        location: TextBoxModes
        phone: TextBoxModes
        email: TextBoxModes
        comments: TextBoxModes
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
        |EnableTextBox of string
        |DisableTextBox of string * DateTime
    [<Literal>]
    let DEFAULT_ORGANISATION_ID = 0

    type Model = { fieldsStatus:InfoBoxFieldsStatus; fieldStatusChanged: DateTime option; loading: bool; loaded:bool; 
                   contactInfo: ContactInfo option
                   loadContactRequest: AsyncRequest option;
                   LoadLocationsList: AsyncRequest option;
                   LocationsList: Location seq option; 
                   LocationComboBoxIndex: int option
                   }

    let init() = { fieldsStatus={contactName= ReadOnlyMode 
                                 organisation= ReadOnlyMode
                                 location= ReadOnlyMode
                                 phone= ReadOnlyMode
                                 email= ReadOnlyMode
                                 comments= ReadOnlyMode};
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
            let newFieldsStatus = 
                match textBoxName with
                |"contactName" -> {model.fieldsStatus with contactName = EditMode}
                |"organisation" -> {model.fieldsStatus with organisation = EditMode}
                |"phone" -> {model.fieldsStatus with phone = EditMode}
                |"email" -> {model.fieldsStatus with email = EditMode}
                |"comments" -> {model.fieldsStatus with comments = EditMode}
                | _ -> 
                    debug (sprintf "could not match textBox name EnableTextBox(%s)" textBoxName)
                    model.fieldsStatus

            {model with fieldsStatus = newFieldsStatus; fieldStatusChanged = Some(newDate())}, Cmd.none
        |DisableTextBox(textBoxName, newDate)->
            //allowed to disable textbox only if it was enabled more than 100 ms ago
            let allowedToDisableTextBox = 
                match model.fieldStatusChanged with 
                |Some oldDate -> 
                    (newDate - oldDate).Duration() > TimeSpan.FromMilliseconds(float 100)
                |_ -> false

            match allowedToDisableTextBox with 
            |true -> 
                let newFieldsStatus = 
                    match textBoxName with
                    |"contactName" -> {model.fieldsStatus with contactName = ReadOnlyMode}
                    |"organisation" -> {model.fieldsStatus with organisation = ReadOnlyMode}
                    |"phone" -> {model.fieldsStatus with phone = ReadOnlyMode}
                    |"email" -> {model.fieldsStatus with email = ReadOnlyMode}
                    |"comments" -> {model.fieldsStatus with comments = ReadOnlyMode}
                    | _ -> 
                        debug (sprintf "could not match textBox name: message > DisableTextBox(%s,%A)" textBoxName newDate)
                        model.fieldsStatus

                {model with fieldsStatus = newFieldsStatus}, Cmd.none
            |false -> model, Cmd.none
        |UpdateContactInfoPhone(value) ->
            updateContactInfoField model (fun info -> Some {info with telephone = optionFromString value}), Cmd.none
        |UpdateContactInfoComments(value) -> 
            updateContactInfoField model (fun info -> Some {info with comments = optionFromString value}), Cmd.none
        |UpdateContactInfoContactName(value) -> 
            updateContactInfoField model (fun info -> Some {info with ContactName = value}), Cmd.none
        |UpdateContactInfoEmail(value)->
            updateContactInfoField model (fun info -> Some {info with email = optionFromString value}), Cmd.none

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
        ///get int field from Some ContactInfo or return 0 if None
        let getIntFieldFromContactInfo m=
            getFieldFromContactInfoOption m.contactInfo 0
        
        let intFromOptionOrDefault opt (returnIfNone:int)=
            match opt with
            |Some o -> o
            |None -> returnIfNone

        let isReadOnly textBoxMode = 
            match textBoxMode with 
            | EditMode -> false 
            | ReadOnlyMode -> true
        let isValidEmail str = 
            let emailRegex = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z"
            Regex.IsMatch(str, emailRegex, RegexOptions.IgnoreCase)

        ["loading" |> Binding.oneWay (fun m -> m.loading)
         "loaded" |> Binding.oneWay (fun m -> m.loaded)
         "organisation" |> Binding.oneWay (fun m -> getStringFieldFromContactInfo m (fun info -> info.organisationName))
         "location" |> Binding.oneWay (fun m -> getIntFieldFromContactInfo m (fun info -> intFromOptionOrDefault info.locationId 0))
         "contactName" |> Binding.twoWay (fun m -> getStringFieldFromContactInfo m (fun info -> info.ContactName))//getter
                                         (fun v m-> UpdateContactInfoContactName(v))//setter
         "Comments" |> Binding.twoWay (fun m -> getStringOptionFieldFromContactInfo m (fun info -> info.comments)) //getter
                                      (fun v m -> UpdateContactInfoComments(v))//setter
         "phone" |> Binding.twoWay (fun m -> getStringOptionFieldFromContactInfo m  (fun info -> info.telephone)) //getter
                                   (fun v m -> UpdateContactInfoPhone(v)) //setter
         //"email" |> Binding.oneWay (fun m -> getStringOptionFieldFromContactInfo m (fun info -> info.email))
         "email" |> Binding.twoWayValidation (fun m -> getStringOptionFieldFromContactInfo m (fun info -> info.email))//getter
                                             (fun v m -> match isValidEmail v with 
                                                         |true ->  UpdateContactInfoEmail v |> Ok 
                                                         |false -> Error "Email is invalid")//setter with validation
         "IsDisabled" |> Binding.oneWay (fun m -> getFieldFromContactInfoOption m.contactInfo false (fun info -> info.IsDisabled))
         "IsAdmin" |> Binding.oneWay (fun m -> getFieldFromContactInfoOption m.contactInfo false (fun info -> info.IsAdmin))
         
         //are fields enabled or read-only
         "IsContactNameReadOnly" |> Binding.oneWay (fun m -> isReadOnly m.fieldsStatus.contactName)
         "IsOrganisationReadOnly" |> Binding.oneWay (fun m -> isReadOnly m.fieldsStatus.organisation)
         "IsLocationComboBoxEnabled" |> Binding.oneWay (fun m -> (match m.fieldsStatus.location with | EditMode -> true | ReadOnlyMode -> false) )//binds the isEnabled property
         "IsPhoneReadOnly"|> Binding.oneWay (fun m -> isReadOnly m.fieldsStatus.phone )
         "IsEmailReadOnly"|> Binding.oneWay (fun m -> isReadOnly m.fieldsStatus.email)
         "AreCommentsReadOnly"|> Binding.oneWay (fun m -> isReadOnly m.fieldsStatus.comments)
         
         "locationsSource" |> Binding.oneWay (fun m -> match m.LocationsList with 
                                                       | Some l -> l
                                                       | None -> null)
         "SelectedLocationIndex" |> Binding.oneWay (fun m -> intFromOptionOrDefault m.LocationComboBoxIndex -1) 
         
         //change textBox modes
         "EnableTextBox" |> Binding.cmd (fun param m ->  EnableTextBox (string param))
         "DisableTextBox" |> Binding.cmd (fun param m -> DisableTextBox (string param, newDate()))
        ]




