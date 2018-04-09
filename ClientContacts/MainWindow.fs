namespace ClientContacts


module MainWindow =
    open Elmish
    open Elmish.WPF
    open System.Windows.Forms

    type Msg = 
        |ContactList of ContactList.Msg 

    type Model = {Contacts: ContactList.Model}

    let init() = { Contacts = ContactList.init() }, Cmd.none

    let map (model, cmd) =
            model, cmd |> Cmd.map ContactList

    let update (msg:Msg) (model:Model) = 
        match msg with
        | ContactList x -> 
            let m, ms = ContactList.update x (model.Contacts) |> map
            
            { model with Contacts = m }, ms //Cmd.map ContactList ms
            
            

            
    let view msg model = 
        //"Clock" |> Binding.model (fun m -> m.Clock) clockViewBinding ClockMsg
        ["ContactList" |> Binding.model (fun m -> m.Contacts) (ContactList.counterListViewBindings) ContactList
        ]




