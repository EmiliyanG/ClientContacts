namespace ClientContacts

module ContactList =
    open Elmish
    open Elmish.WPF
    open System
    open MySQLConnection
    open System.Threading
    open ElmishUtils
    open ClientContactViews
    open System.Configuration
    open SQLTypes
    open System.Windows.Forms
    
    [<Literal>] 
    let QUERY_LIMIT = 50

    type SearchResult = 
        |Loading
        |Loaded
        |NoResults
    
    type LoadMoreBtnStatus = 
        |DisplayLoadingBar
        |DisplayLoadMoreBtn

    type Msg = 
        |SearchContacts of string * Offset * Limit
        |UpdateContacts of Contact.Model list * DateTime
        |SearchFailure
        |UpdateContactInfo of int 
        |LoadMoreResults
        |FilterDisabled
        //|UpdateContact of Guid * Contact.Msg
    
    type Filters = {filterByDisabledContact: bool; filterByAdminContact: bool }

    type Model = {status: SearchResult; loadBtnStatus: LoadMoreBtnStatus; search: string; offset: Offset; limit: Limit;
                  
                  latestRequest: DateTime option; 
                  cancelSource: CancellationTokenSource option; 
                  
                  contactList: Contact.Model list;
                  filters: Filters}

    let init() = { status= Loading; loadBtnStatus=DisplayLoadMoreBtn; search = ""; offset= Offset(0); limit=Limit(QUERY_LIMIT);
                   latestRequest = None; cancelSource = None;  contactList = [];
                   filters= {filterByDisabledContact=false; filterByAdminContact=false} }

 

        

    let update (msg:Msg) (model:Model) = 
        match msg with
        | SearchContacts (s,offset,limit) -> 
             let d = newDate()

             //cancel old request
             match model.cancelSource with 
             | Some src -> 
                //System.Windows.MessageBox.Show("cancel async") |> ignore
                src.Cancel()
             | _ -> ()

             let src = new CancellationTokenSource()
             {model with status= (match offset with | Offset(0) -> Loading | _ -> Loaded);
                         search = s; offset=offset; limit=limit ; latestRequest = Some d; cancelSource = Some src}, 
             ofAsync (getListOfContacts s d limit offset ) (src.Token) (fun (q,t) -> UpdateContacts (q ,t)) (fun _ -> SearchFailure)

        | UpdateContacts (q, d)-> 
            match model.latestRequest, model.offset with 
            | Some r, Offset(0) when r = d -> //new request made - update all results
                {model with status=(match q with
                                   | [] -> NoResults
                                   | _ -> Loaded);
                                   contactList = q}, Cmd.none
            | Some r, _ when r = d -> //load more results request - append to existing results
                
                {model with loadBtnStatus = DisplayLoadMoreBtn;
                            contactList = (List.append (model.contactList |> (List.rev >> List.tail >> List.rev)) q) }, Cmd.none

            | _ -> model, Cmd.none

        | SearchFailure ->  model, Cmd.none
        | UpdateContactInfo (id) -> 
            //Doing nothing - this message will be elevated 1 level up 
            model, Cmd.none
        | LoadMoreResults -> 
            {model with loadBtnStatus = DisplayLoadingBar}, Cmd.ofMsg (SearchContacts(model.search,Offset(model.offset.getData + QUERY_LIMIT), Limit(QUERY_LIMIT))) 
        | FilterDisabled -> 
            //MessageBox.Show("filterDisabled command called") |> ignore
            {model with filters = {model.filters with filterByDisabledContact=true}}, Cmd.none 
    
    let filterContacts (m:Model) =
        m.contactList
        |> (fun cList ->    
            match m.filters.filterByDisabledContact with
            | true -> cList |> List.filter (fun c -> c.IsDisabled = false)
            | false -> cList
           )
        |> (fun cList ->    
            match m.filters.filterByAdminContact with
            | true -> cList |> List.filter (fun c -> c.IsAdmin = false)
            | false -> cList
           )


    let contactsListViewBindings = 

        ["ContactItems" |> Binding.oneWay (fun m -> filterContacts m)
         "SearchBar" |> Binding.twoWay (fun m -> m.search) (fun s m -> SearchContacts(s,Offset(0),Limit(QUERY_LIMIT)) )
         "UpdateContactInfo" |> Binding.cmd (fun p m -> 
                                let i = p :?> int //downcast the p object to Guid
                                UpdateContactInfo(i))
         "loaded" |> Binding.oneWay (fun m -> match m.status with | Loaded -> true |_ -> false)
         "LoadingNewRequest" |> Binding.oneWay (fun m -> match m.status with | Loading -> true |_ -> false)   
         "DisplayLoadingBar" |> Binding.oneWay (fun m -> match m.loadBtnStatus with |DisplayLoadingBar -> true |_ -> false)  
         "DisplayLoadMoreBtn" |> Binding.oneWay (fun m -> match m.loadBtnStatus with | DisplayLoadMoreBtn -> true |_ -> false ) 
         "noResults" |> Binding.oneWay (fun m -> match m.status with | NoResults -> true |_ -> false)
         "LoadMoreResults" |> Binding.cmd (fun msg m -> LoadMoreResults)
         "FilterDisabled" |> Binding.cmd (fun msg m -> FilterDisabled)
        ]




