namespace ClientContacts

module Validator =
    
    open System.Text.RegularExpressions
    open DebugUtils
    
    [<Literal>]
    let emailRegex = @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z"
    [<Literal>]
    let emptyStringRegex = @"^[\s\t]+$"

    type TextBox = 
    |ContactName
    |Phone
    |Email
    |Comments

    type ContactInfoBoxValidationError = {message: string; fieldName: TextBox}

    type Result<'TSuccess,'TFailure> = 
    | Success of 'TSuccess
    | Failure of 'TFailure
    

    let bind switchFunction = 
        function
        | Success s -> switchFunction s
        | Failure f -> Failure f

    let (>>>) f1 f2 =
        f1 >> bind f2

    let isFieldValueLongerThanLimit limit (str:string) = 
        match str.Length <= limit with 
        |true -> Success str
        |false -> Failure (sprintf "Must be less than %d characters." limit)

    let doesFieldContainBlankSpacesOnly (str:string) =
        match Regex.IsMatch(str, emptyStringRegex, RegexOptions.IgnoreCase) with
        |true -> Failure ( "Must contain at least 1 non-space character." )
        |false -> Success str
    
    let isFieldEmpty (str:string) =
        match str.Length with
        |0 -> Failure ("Cannot be empty.")
        |_ -> Success str

    let doesEmailMatchRegex str = 
        match Regex.IsMatch(str, emailRegex, RegexOptions.IgnoreCase) with 
        |true -> Success str
        |false -> Failure "The email is not valid."
    
    let validateEmail= 
        isFieldValueLongerThanLimit 250
        >>> doesEmailMatchRegex

    let validateContactName =
        isFieldValueLongerThanLimit 250
        >>> doesFieldContainBlankSpacesOnly
        >>> isFieldEmpty

    let validateTelephone =
        isFieldValueLongerThanLimit 250

    let validateComments =
        isFieldValueLongerThanLimit 2000
    
    let validateLocation =
        isFieldEmpty
        >>> doesFieldContainBlankSpacesOnly
        >>> isFieldValueLongerThanLimit 250

    let validateOrganisation =
        isFieldEmpty
        >>> doesFieldContainBlankSpacesOnly
        >>> isFieldValueLongerThanLimit 250

    let getValidationFunction =
        function 
        |Email -> validateEmail
        |ContactName -> validateContactName
        |Phone -> validateTelephone
        |Comments -> validateComments

    let getValidationsErrorsForTextBox errors textBox =
        errors
        |>Option.bind(fun errs -> List.tryFind (fun e -> e.fieldName = textBox) errs)
    
    ///return validation error message for the specified field or "" if None
    let getValidationErrorMessageForTextBox field errors=
        errors
        |> getValidationsErrorsForTextBox <| field
        |> (fun e-> 
                match e with 
                |Some er -> 
                    match field, er with 
                    |f, vr when f = vr.fieldName -> vr.message
                    |_, _ -> ""
                |None -> ""
            )

    let addValidationError (errors:ContactInfoBoxValidationError list option) e =
        e::match errors with 
           |Some e -> e
           |None -> []
        |> Some
    
    let removeValidationError errors textBox = 
        errors
        |> Option.bind(fun errs -> errs 
                                   |> List.filter(fun e -> not (e.fieldName = textBox)) 
                                   |> function 
                                      |[] -> None 
                                      |l -> Some l)
