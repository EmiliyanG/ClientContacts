namespace ClientContacts

module OrganisationExpander =
    open Elmish
    open Elmish.WPF
    open System
    open ClientContactViews
    open System.Configuration
    


    type Msg = 
        |UpdateOrganisationContacts of Contact.Model list
        

    type Model = {OrganisationName:string; OrganisationContacts: Contact.Model list}

    let init() = {OrganisationName=""; OrganisationContacts = [] }

 

    let update (msg:Msg) (model:Model) = 
        match msg with
        | UpdateOrganisationContacts(c) -> 
            {model with OrganisationContacts = c}, Cmd.none
        
            
    let contactsListViewBindings: ViewBinding<Model, Msg> list= 
        ["OrganisationContacts" |> Binding.oneWay (fun m -> m.OrganisationContacts)
        ]




