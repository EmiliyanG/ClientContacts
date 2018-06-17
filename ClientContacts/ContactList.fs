namespace ClientContacts

module ContactList =
    open Elmish
    open Elmish.WPF
    open System
    open MySQLConnection
    open System.Threading
    open ElmishUtils
    

    type Msg = 
        |SearchContacts of string
        |UpdateContacts of Contact.Model list * DateTime
        |SearchFailure
        |UpdateContactInfo of int 
        //|UpdateContact of Guid * Contact.Msg

    type Model = {search: string; latestRequest: DateTime option; cancelSource: CancellationTokenSource option; contactList: Contact.Model list}

    let init() = { search = ""; latestRequest = None; cancelSource = None;  contactList = [Contact.init()] }

 
        //generate task -> getListOfContacts searchString dateTriggered
        //UpdateContacts (r ,dateTriggered)


    let update (msg:Msg) (model:Model) = 
        match msg with
        | SearchContacts s -> 
             let d = newDate()

             //cancel old request
             match model.cancelSource with 
             | Some src -> 
                //System.Windows.MessageBox.Show("cancel async") |> ignore
                src.Cancel()
             | _ -> ()

             let src = new CancellationTokenSource()
             {model with search = s ; latestRequest = Some d; cancelSource = Some src}, 
             ofAsync (getListOfContacts s d) (src.Token) (fun (q,t) -> UpdateContacts (q ,t)) (fun _ -> SearchFailure)

        | UpdateContacts (q, d)-> 
            match model.latestRequest with 
            | Some r when r = d -> {model with contactList = q}, Cmd.none
            | _ -> model, Cmd.none

        | SearchFailure ->  model, Cmd.none
        | UpdateContactInfo (id) -> 
            model, Cmd.none
        
            
    let contactsListViewBindings = 
        
        ["ContactItems" |> Binding.oneWay (fun m -> m.contactList)
         "SearchBar" |> Binding.twoWay (fun m -> m.search) (fun s m -> SearchContacts(s))
         "UpdateContactInfo" |> Binding.cmd (fun p m -> 
                                let i = p :?> int //downcast the p object to Guid
                                UpdateContactInfo(i))
        ]




