namespace ClientContacts

module ContactInfoBoxValidator =
    type TextBox = 
        |ContactName
        |Phone
        |Email
        |Comments
    type ValidationError = {message: string; fieldName: TextBox}


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
