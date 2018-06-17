namespace ClientContacts


module MainWindow =
    open Elmish
    open Elmish.WPF
    open System.Windows.Forms
    open ClientContactViews

    type Msg = 
        |ContactList of ContactList.Msg 
        |ContactInfoBoxMsg of ContactInfoBox.Msg

    type Model = {Contacts: ContactList.Model ; ContactInfoBox: ContactInfoBox.Model}

    let init() = { Contacts = ContactList.init(); ContactInfoBox=ContactInfoBox.init() }, Cmd.none



    let update (msg:Msg) (model:Model) = 
        match msg with
        | ContactList x -> 
            match x with 
            | ContactList.Msg.UpdateContactInfo i -> 
                model, Cmd.ofMsg (ContactInfoBoxMsg(ContactInfoBox.Msg.LoadContact(i)))
            | _ -> 
                let mapContactList (model, cmd) = model, cmd |> Cmd.map ContactList
                let m, ms = ContactList.update x (model.Contacts) |> mapContactList
                { model with Contacts = m }, ms //Cmd.map ContactList ms
        | ContactInfoBoxMsg x -> 
            let mapContactList (model, cmd) = model, cmd |> Cmd.map ContactInfoBoxMsg
            let m, msg = ContactInfoBox.update x (model.ContactInfoBox) |> mapContactList 
            {model with ContactInfoBox = m}, msg
            
            

            
    let view msg model = 
        //"Clock" |> Binding.model (fun m -> m.Clock) clockViewBinding ClockMsg
        ["ContactList" |> Binding.model (fun m -> m.Contacts) (ContactList.contactsListViewBindings) ContactList
         "ContactInfoBox" |> Binding.model (fun m -> m.ContactInfoBox) (ContactInfoBox.ContactInfoBoxViewBindings) ContactInfoBoxMsg
        
        ]




