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

    type Model = { id: int option; loading: bool; organisation: string; name: string; phone: string option; 
                   email: string option; comments: string option; cancelSource: CancellationTokenSource option; 
                   latestRequest: DateTime option;}

    let init() = { id= None; loading= true; organisation = ""; name = ""; phone = None; email = None;  
                   comments = None; cancelSource=None; latestRequest = None }
    
 
        
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
            | Some r when r = d -> {model with loading = false}, Cmd.none
            | _ -> model, Cmd.none

        | LoadFailure ->  model, Cmd.none
        
            
    let ContactInfoBoxViewBindings: ViewBinding<Model, Msg> list = 
        
        ["loading" |> Binding.oneWay (fun m -> m.loading)
        ]




