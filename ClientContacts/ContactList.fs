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
    open DebugUtils
    
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
        |UpdateIndividualContact of ContactInfo * OrganisationName * Location option
        |UpdateContactsWithEditedOrganisationName of oldName: OrganisationName * newName: OrganisationName
        |SearchFailure
        |UpdateContactInfo of int 
        |LoadMoreResults
        |FilterDisabled of bool
        |FilterAdmins of bool
        |AddNewContact of OrganisationName
        |AddNewLocation of OrganisationName
        |EditOrganisation of Organisation
        |EditLocation of LocationName
        //|UpdateContact of Guid * Contact.Msg
    
    type Filters = {includeDisabledContacts: bool; showAdminsOnly: bool }

    type Model = {status: SearchResult; loadBtnStatus: LoadMoreBtnStatus; search: string; offset: Offset; limit: Limit;
                  
                  loadContactsRequest: AsyncRequest option;
                  contactList: Contact.Model list;
                  filters: Filters}

    let init() = { status= Loading; loadBtnStatus=DisplayLoadMoreBtn; search = ""; offset= Offset(0); limit=Limit(QUERY_LIMIT);
                   loadContactsRequest=None;  contactList = [];
                   filters= {includeDisabledContacts=false; showAdminsOnly=false} }

 

        

    let update (msg:Msg) (model:Model) = 
        match msg with
        | SearchContacts (s,offset,limit) -> 
             let d = newDate()

             //cancel old request
             match model.loadContactsRequest with 
             | Some request -> 
                request.cancelSource.Cancel()
             | _ -> ()

             let src = new CancellationTokenSource()
             {model with status= (match offset with | Offset(0) -> Loading | _ -> Loaded);
                         search = s; offset=offset; limit=limit ; 
                         loadContactsRequest=Some { latestRequest = d; cancelSource = src}}, 
             ofAsync (getListOfContacts s d limit offset ) (src.Token) (fun (q,t) -> UpdateContacts (q ,t)) (fun _ -> SearchFailure)

        | UpdateContacts (q, d)-> 
            match model.loadContactsRequest, model.offset with 
            | Some request, Offset(0) when request.latestRequest = d -> //new request made - update all results
                {model with status=(match q with
                                   | [] -> NoResults
                                   | _ -> Loaded);
                                   contactList = q}, Cmd.none
            | Some request, _ when request.latestRequest = d -> //load more results request - append to existing results
                
                {model with loadBtnStatus = DisplayLoadMoreBtn;
                            contactList = (List.append (model.contactList |> (List.rev >> List.tail >> List.rev)) q)
                            loadContactsRequest = None}, Cmd.none

            | _ -> model, Cmd.none

        |UpdateIndividualContact (info,o,l) -> 
            
            let newList =
                model.contactList
                |> List.map (
                    fun c -> 
                        match c.id with 
                        | i when i = info.id -> Contact.castContactInfoToContactModel info o l
                        |_ -> c)
                        
            {model with contactList = newList},Cmd.none
        |UpdateContactsWithEditedOrganisationName (oldName, newName) -> 
            let newList =
                model.contactList
                |> List.map(
                    fun c -> 
                        match c.organisationName with 
                        | n when n = oldName.getData -> {c with organisationName = newName.getData}
                        |_ -> c)
                    
            {model with contactList = newList}, Cmd.none
        | SearchFailure ->  model, Cmd.none
        | UpdateContactInfo(id)-> 
            //this message will be elevated 1 level up 
            failwith <| sprintf "this message should have been caught 1 level up. Msg: UpdateContactInfo(%d)" id
            model, Cmd.none
        |AddNewLocation org -> 
            failwith <| sprintf "The message AddNewLocation(%A) should have been intercepted 1 level up" org
            model, Cmd.none
        | AddNewContact(organisationName)-> 
            //this message will be elevated 1 level up 
            failwith <| sprintf "this message should have been caught 1 level up. Msg: AddNewContact(%s)" organisationName.getData
            model, Cmd.none
        |EditOrganisation(org)-> 
            //this message will be elevated 1 level up 
            failwith <| sprintf "this message should have been caught 1 level up. Msg: EditOrganisation(%A)" org
            model, Cmd.none
        |EditLocation(locationName) -> 
            //this message will be elevated 1 level up 
            failwith <| sprintf "this message should have been caught 1 level up. Msg: EditLocation(%s)" locationName.getData
            model, Cmd.none
        | LoadMoreResults -> 
            {model with loadBtnStatus = DisplayLoadingBar}, Cmd.ofMsg (SearchContacts(model.search,Offset(model.offset.getData + QUERY_LIMIT), Limit(QUERY_LIMIT))) 
        | FilterDisabled(isChecked) -> 
            {model with filters = {model.filters with includeDisabledContacts=isChecked}}, Cmd.none 
        | FilterAdmins(isChecked) -> 
            {model with filters = {model.filters with showAdminsOnly=isChecked}}, Cmd.none
        
    let filterContacts (m:Model) =
        m.contactList
        |> (fun cList ->    
            match m.filters.includeDisabledContacts, 
                  m.filters.showAdminsOnly 
                  with 
            | true,true -> cList |> List.filter (fun c -> c.IsAdmin)
            | false, true -> cList |> List.filter (fun c -> c.IsAdmin  && not c.IsDisabled )
            | true, false -> cList 
            | false, false -> cList |> List.filter (fun c -> not c.IsDisabled)
           )

    let contactsListViewBindings = 

        let compareParamWithExpectedValue expected actual =
            match expected, actual with 
            |e, a when e = a -> true
            |_-> false
        
        let getOrganisationByName (orgName:OrganisationName) (contacts:Contact.Model list)=
            let c = List.find (fun (c:Contact.Model) -> c.organisationName = orgName.getData) contacts  
            {id= c.organisationId; organisationName= c.organisationName}
            

        ["ContactItems" |> Binding.oneWayMap (fun m -> m) (fun v-> filterContacts v)
         "SearchBar" |> Binding.twoWay (fun m -> m.search) (fun s m -> SearchContacts(s,Offset(0),Limit(QUERY_LIMIT)) )
         "UpdateContactInfo" |> Binding.cmd (fun p m -> 
                                let i = p :?> int //downcast the p object to int
                                UpdateContactInfo(i))
         "loaded" |> Binding.oneWayMap (fun m -> m.status) (fun v -> v |> compareParamWithExpectedValue Loaded)
         "LoadingNewRequest" |> Binding.oneWayMap (fun m -> m.status)(fun v -> v |> compareParamWithExpectedValue Loading)   
         "DisplayLoadingBar" |> Binding.oneWayMap (fun m -> m.loadBtnStatus) (fun v -> v |> compareParamWithExpectedValue DisplayLoadingBar)  
         "DisplayLoadMoreBtn" |> Binding.oneWayMap (fun m -> m.loadBtnStatus) (fun v -> v |> compareParamWithExpectedValue DisplayLoadMoreBtn) 
         "noResults" |> Binding.oneWayMap (fun m -> m.status) (fun v -> v |> compareParamWithExpectedValue NoResults)
         "LoadMoreResults" |> Binding.cmd (fun msg m -> LoadMoreResults)
         "FilterDisabled" |> Binding.cmd (fun isChecked m -> 
                                              let ic = isChecked :?> bool //downcast the isChecked object to bool
                                              FilterDisabled(ic) )
         "FilterAdmins" |> Binding.cmd (fun isChecked m -> 
                                              let ic = isChecked :?> bool //downcast the isChecked object to bool
                                              FilterAdmins(ic) )
         "AddNewContact" |> Binding.cmd (fun param m -> AddNewContact(OrganisationName(string param)))
         "AddNewLocation" |> Binding.cmd (fun param m -> AddNewLocation(OrganisationName(string param)))
         "EditOrganisation" |> 
            Binding.cmd (
                fun param m -> 
                    OrganisationName(string param)
                    |> getOrganisationByName <| m.contactList
                    |> EditOrganisation
                    )       
         "EditLocation" |> Binding.cmd (fun param m -> EditLocation(LocationName(string param)))
        ]




