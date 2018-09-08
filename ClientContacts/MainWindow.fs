namespace ClientContacts


module MainWindow =
    open Elmish
    open Elmish.WPF
    open System.Windows.Forms
    open ClientContactViews
    open SQLTypes
    open ContactList

    type Msg = 
        |ContactList of ContactList.Msg 
        |ContactInfoBoxMsg of ContactInfoBox.Msg
        |LocationPopupMsg of LocationPopup.Msg
        |OrganisationPopupMsg of OrganisationPopup.Msg
        |AddNewOrganisation


    type Model = {Contacts: ContactList.Model ; 
                  ContactInfoBox: ContactInfoBox.Model; 
                  LocationPopup: LocationPopup.Model
                  OrganisationPopup: OrganisationPopup.Model
                  IsAddressBookVisible: bool
                  }

    let init() = { Contacts = ContactList.init() 
                   ContactInfoBox=ContactInfoBox.init() 
                   LocationPopup=LocationPopup.init()
                   OrganisationPopup=OrganisationPopup.init()
                   IsAddressBookVisible = true 
                   }, 
                  Cmd.ofMsg (ContactList( ContactList.Msg.SearchContacts("", Offset(0), Limit(QUERY_LIMIT) ))) //load list of contacts when window is loaded
                


    let update (msg:Msg) (model:Model) = 
        match msg with
        | ContactList x -> 
            match x with 
            | ContactList.Msg.UpdateContactInfo i -> 
                {model with IsAddressBookVisible = false}, Cmd.ofMsg (ContactInfoBoxMsg(ContactInfoBox.Msg.LoadContact(i)))
            | ContactList.Msg.AddNewContact org -> 
                {model with IsAddressBookVisible = false}, Cmd.ofMsg (ContactInfoBoxMsg(ContactInfoBox.Msg.AddNewContact(org)))
            | ContactList.Msg.AddNewLocation org -> 
                model, Cmd.ofMsg (LocationPopupMsg(LocationPopup.Msg.AddNewLocation(org)))
            | ContactList.Msg.EditLocation l -> 
                model, Cmd.ofMsg(LocationPopupMsg(LocationPopup.Msg.EditExistingLocation(l)))
            | ContactList.Msg.EditOrganisation org -> 
                model, Cmd.ofMsg (OrganisationPopupMsg(OrganisationPopup.Msg.EditOrganisation(org)))
            | _ -> 
                let mapContactList (model, cmd) = model, cmd |> Cmd.map ContactList
                let m, ms = ContactList.update x (model.Contacts) |> mapContactList
                { model with Contacts = m }, ms //Cmd.map ContactList ms
        |AddNewOrganisation -> 
             model, Cmd.ofMsg(OrganisationPopupMsg(OrganisationPopup.Msg.AddNewOrganisation))
        | ContactInfoBoxMsg x -> 
            match x with 
            |ContactInfoBox.Msg.SaveContactInfoChangesSuccess(i,o,l) -> 
                model, Cmd.ofMsg(ContactList(ContactList.Msg.UpdateIndividualContact(i,o,l)))
            |ContactInfoBox.Msg.ShowAddressBookImage -> 
                {model with IsAddressBookVisible = true}, Cmd.none
            |_ ->
                let mapContactList (model, cmd) = model, cmd |> Cmd.map ContactInfoBoxMsg
                let m, msg = ContactInfoBox.update x (model.ContactInfoBox) |> mapContactList 
                {model with ContactInfoBox = m}, msg
        | LocationPopupMsg x -> 
            match x with 
            | LocationPopup.Msg.UpdateContactsWithEditedLocationName l ->
                model, Cmd.ofMsg(ContactList(ContactList.Msg.UpdateContactsWithEditedLocationName(l)))
            |_ ->
                let mapLocationPopupMsg (model, cmd) = model, cmd |> Cmd.map LocationPopupMsg
                let m, msg = LocationPopup.update x (model.LocationPopup) |> mapLocationPopupMsg
                {model with LocationPopup = m}, msg
        | OrganisationPopupMsg x -> 
            match x with 
            |OrganisationPopup.Msg.UpdateContactsWithEditedOrganisationName(a,b) -> 
                model, Cmd.ofMsg(ContactList(ContactList.Msg.UpdateContactsWithEditedOrganisationName(a,b)))
            |_ -> 
                let mapOrganisationPopupMsg (model, cmd) = model, cmd |> Cmd.map OrganisationPopupMsg
                let m, msg = OrganisationPopup.update x (model.OrganisationPopup) |> mapOrganisationPopupMsg
                {model with OrganisationPopup = m}, msg

            

            
    let view msg model = 
        //"Clock" |> Binding.model (fun m -> m.Clock) clockViewBinding ClockMsg
        ["ContactList" |> Binding.model (fun m -> m.Contacts) (ContactList.contactsListViewBindings) ContactList
         "ContactInfoBox" |> Binding.model (fun m -> m.ContactInfoBox) (ContactInfoBox.ContactInfoBoxViewBindings) ContactInfoBoxMsg
         "AddressBook" |> Binding.oneWay (fun m -> m.IsAddressBookVisible)
         "LocationPopup"  |> Binding.model (fun m -> m.LocationPopup) (LocationPopup.locationPopupViewBindings) LocationPopupMsg
         "OrganisationPopup" |> Binding.model (fun m -> m.OrganisationPopup) (OrganisationPopup.organisationPopupViewBindings) OrganisationPopupMsg
         "AddNewOrganisation" |> Binding.cmd(fun param m -> AddNewOrganisation)
        ]




