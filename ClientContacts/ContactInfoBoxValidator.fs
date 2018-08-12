namespace ClientContacts

module ContactInfoBoxValidator =
    
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
        member this.ToString =
            match this with 
            |Email -> "email"
            |Phone -> "telephone"
            |ContactName -> "contact name"
            |Comments -> "comments"

    type ValidationError = {message: string; fieldName: TextBox}

    type Result<'TSuccess,'TFailure> = 
    | Success of 'TSuccess
    | Failure of 'TFailure
    

    let bind switchFunction = 
        function
        | Success s -> switchFunction s
        | Failure f -> Failure f

    let (>>>) f1 f2 =
        f1 >> bind f2

    let isFieldValueLongerThanLimit (field:TextBox) limit (str:string) = 
        match str.Length <= limit with 
        |true -> Success str
        |false -> Failure (sprintf "The value in the %s field cannot exceed more than %d characters." (field.ToString)  limit)

    let doesFieldContainBlankSpacesOnly (field:TextBox) (str:string) =
        match Regex.IsMatch(str, emptyStringRegex, RegexOptions.IgnoreCase) with
        |true -> Failure ( sprintf "The %s field cannot be empty" (field.ToString) )
        |false -> Success str
    
    let isFieldEmpty (field:TextBox) (str:string) =
        debug <| sprintf "str: <%s> length: %d" str str.Length
        match str.Length with
        |0 -> Failure ( sprintf "The %s field cannot be empty" (field.ToString) )
        |_ -> Success str

    let doesEmailMatchRegex str = 
        match Regex.IsMatch(str, emailRegex, RegexOptions.IgnoreCase) with 
        |true -> Success str
        |false -> Failure "The email is not valid."
    
    let validateEmail= 
        isFieldValueLongerThanLimit Email 250
        >>> doesEmailMatchRegex

    let validateContactName =
        isFieldValueLongerThanLimit ContactName 250
        >>> doesFieldContainBlankSpacesOnly ContactName
        >>> isFieldEmpty ContactName

    let validateTelephone =
        isFieldValueLongerThanLimit Phone 250

    let validateComments =
        isFieldValueLongerThanLimit Comments 2000
    
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

    let addValidationError (errors:ValidationError list option) e =
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
