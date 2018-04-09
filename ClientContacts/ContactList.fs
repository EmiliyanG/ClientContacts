namespace ClientContacts

module ContactList =
    open Elmish
    open Elmish.WPF
    open System
    open MySQLConnection
    open System.Threading
    

    type Msg = 
        |SearchContacts of string
        |UpdateContacts of Contact.Model list * DateTime
        |SearchFailure
        //|UpdateContact of Guid * Contact.Msg

    type Model = {search: string; latestRequest: DateTime option; cancelSource: CancellationTokenSource option; contactList: Contact.Model list}

    let init() = { search = ""; latestRequest = None; cancelSource = None;  contactList = [Contact.init()] }

    let newDate() = DateTime.Now

    let ofAsync (task: Async<_>)
            (cToken:CancellationToken)
            (ofSuccess: _ -> 'msg)
            (ofError: _ -> 'msg) : Cmd<'msg> =
        
        let buildAsync (c:CancellationToken) a = 
            Async.Start(a,c)
        
        let bind dispatch =
            async {
                let! r = task |> Async.Catch
                dispatch (match r with
                            | Choice1Of2 x -> ofSuccess x
                            | Choice2Of2 x -> ofError x)
            }
        [bind >> buildAsync cToken]
        
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
        
            
    let counterListViewBindings = 
        
        ["ContactItems" |> Binding.oneWay (fun m -> m.contactList)
         "SearchBar" |> Binding.twoWay (fun m -> m.search) (fun s m -> SearchContacts(s))
        ]




