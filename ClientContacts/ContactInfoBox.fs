namespace ClientContacts

module ContactInfoBox =
    open Elmish
    open Elmish.WPF
    open System
    open MySQLConnection
    open System.Threading
    open SQLTypes
    open ElmishUtils
    

    type Msg = 
        |LoadContact of int
        |UpdateContactInfo of ContactInfo option * DateTime
        |LoadFailure
        //|UpdateContact of Guid * Contact.Msg

    type Model = { id: int; loading: bool; loaded:bool; organisation: string; name: string; phone: string option; 
                   email: string option; comments: string option; cancelSource: CancellationTokenSource option; 
                   latestRequest: DateTime option; IsDisabled: bool; IsAdmin: bool}

    let init() = { id= 0; loading= false; loaded=false; organisation = ""; name = ""; phone = None; email = None;  
                   comments = None; cancelSource=None; latestRequest = None; IsDisabled = false; IsAdmin = false }
    
 
        
        //generate task -> getListOfContacts searchString dateTriggered
        //UpdateContacts (r ,dateTriggered)


    
    let update (msg:Msg) (model:Model) = 
        match msg with
        | LoadContact s -> 
             let d = newDate()

             //cancel old request
             match model.cancelSource with 
             | Some src -> 
                //System.Windows.MessageBox.Show("cancel async") |> ignore
                src.Cancel()
             | _ -> ()

             let src = new CancellationTokenSource()
             {model with loading = true ; latestRequest = Some d; cancelSource = Some src}, 
             ofAsync (getContactInfo s d) (src.Token) (fun (q,t) -> UpdateContactInfo (q ,t)) (fun _ -> LoadFailure)

        | UpdateContactInfo (q, d)-> 
            match model.latestRequest with 
            | Some r when r = d -> 
            
                match q with 
                |Some info -> 
                    
                    {model with loading = false; loaded = true; name = info.ContactName; 
                                phone = info.telephone; email = info.email;
                                organisation = info.organisationName; IsAdmin= info.IsAdmin; IsDisabled=info.IsDisabled}, Cmd.none
                |_ ->
                    {model with loading = false; loaded = true}, Cmd.none
            | _ -> model, Cmd.none

        | LoadFailure ->  model, Cmd.none
        
            
    let ContactInfoBoxViewBindings: ViewBinding<Model, Msg> list = 
        let stringFromOption opt = 
            match opt with
            | Some o -> o
            | None -> ""
        ["loading" |> Binding.oneWay (fun m -> m.loading)
         "loaded" |> Binding.oneWay (fun m -> m.loaded)
         "organisation" |> Binding.oneWay (fun m -> m.organisation)
         "contactName" |> Binding.oneWay (fun m -> m.name)
         "phone" |> Binding.oneWay (fun m -> stringFromOption m.phone)
         "email" |> Binding.oneWay (fun m -> stringFromOption m.email)
         "IsDisabled" |> Binding.oneWay (fun m -> m.IsDisabled)
         "IsAdmin" |> Binding.oneWay (fun m -> m.IsAdmin)
        ]




