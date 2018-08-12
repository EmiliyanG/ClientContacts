namespace ClientContacts

module ContactInfoBoxBindingsManager =
    open ContactInfoBoxValidator

    type ContactInfoBoxMode = 
        |EditMode
        |ReadOnlyMode

    let stringFromOption opt =
        match opt with
        | Some o -> o
        | None -> ""
    
    ///get field from Some ContactInfo or return defaultValue if None
    let getFieldFromContactInfoOption opt defaultValue func = 
        match opt with
        |Some opt -> func opt
        |None -> defaultValue

    ///get string field from Some ContactInfo or return "" if None
    let getStringFieldFromContactInfo m=
        getFieldFromContactInfoOption m ""
        
    ///get string option field from ContactInfo and pass the result to the stringFromOption function
    let getStringOptionFieldFromContactInfo m=
        getFieldFromContactInfoOption m None
        >> stringFromOption
        
    let intFromOptionOrDefault opt (returnIfNone:int)=
        match opt with
        |Some o -> o
        |None -> returnIfNone

    let isReadOnly = 
        function
        |ReadOnlyMode -> true
        |EditMode -> false
            
    let isEnabled = 
        function
        |ReadOnlyMode -> false
        |EditMode -> true
    let isActionButtonVisible = 
        function
        |ReadOnlyMode -> true
        |EditMode -> false
    
    let getImageBtnVisibility mode param =
        match mode with 
        |EditMode -> true 
        |ReadOnlyMode -> param

    let getImageBtnOpacity mode param =
        match mode with 
        |ReadOnlyMode ->
            match param with
            |true -> 1.0
            |false -> 0.0
        |EditMode -> 
            match param with
            |true -> 1.0
            |false -> 0.3

    let isValidationMessageVisible field errors=
        errors
        |> getValidationsErrorsForTextBox <| field
        |> function 
            |Some e -> true
            |None -> false
