﻿namespace ClientContacts
module OrganisationPopup=
    open Elmish
    open Elmish.WPF
    open System
    open SQLTypes
    open ElmishUtils
    open System.Threading
    open MySQLConnection
    open Validator
    open DebugUtils

    type Mode = 
        |EditOrganisation
        |AddNewOrganisation

    type Msg = 
        |EditOrganisation of Organisation
        |UpdateOrganisationInput of string
        |Cancel
        |Save
        
    type Model = { 
                   isVisible: bool
                   mode: Mode
                   OrganisationInput: Organisation
                   validation: string option
                   }

    let init() = {
                  isVisible=false
                  mode= Mode.EditOrganisation
                  OrganisationInput = {id= -1; organisationName=""}
                  validation= None
                  }
    
    let getTitleFromMode(x:Mode)=
        match x with 
        |Mode.EditOrganisation -> "Edit Organisation"
        |AddNewOrganisation -> "Add New Organisation"


    let update (msg:Msg) (model:Model) = 
        match msg with
        
        |UpdateOrganisationInput v-> 
            {model with 
                   OrganisationInput={model.OrganisationInput 
                                      with organisationName=v}}, Cmd.none
        |EditOrganisation org -> 
            {model with 
                   isVisible=true
                   OrganisationInput = org
                   mode=Mode.EditOrganisation
                }, Cmd.none
        |Cancel -> 
            {model with isVisible = false}, Cmd.none
        |Save -> 
            model, Cmd.none

    

    let organisationPopupViewBindings: ViewBinding<Model, Msg> list = 
        [ 
            "OrganisationPopupVisibility" |> Binding.oneWay (fun m -> m.isVisible)
            "Title" |> Binding.oneWayMap (fun m -> m.mode) 
                                         (fun v -> getTitleFromMode v)
            "Cancel" |> Binding.cmd (fun m param -> Cancel)
            "Save" |> Binding.cmd (fun m param -> Save)
            "OrganisationText" |> Binding.twoWay (fun m -> m.OrganisationInput.organisationName)
                                                 (fun v m -> UpdateOrganisationInput(string v) )
            "OrganisationValidations" |> Binding.oneWayMap 
                                                (fun m-> m.validation) 
                                                (fun v -> 
                                                    match v with
                                                    | Some o -> o
                                                    | None -> "")
          ]





